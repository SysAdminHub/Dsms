using Dsms.Web.Data;
using Dsms.Web.Data.Seed;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Training;

/// <summary>
/// Erzeugt Teilnahmebescheinigungen als PDF und legt sie im Dokumentenmodul ab.
/// </summary>
public class TrainingCertificateService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ITrainingCertificatePdfService pdfService,
    DocumentStorageService storage,
    ILogger<TrainingCertificateService> logger)
{
    private const string CertificateDownloadPath = "/schulung/teilnahme/bescheinigung/download";

    public async Task<TrainingParticipantCertificateInfo> EnsureCertificateForAssignmentAsync(
        int assignmentId,
        int tenantId,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var assignment = await LoadAssignmentAsync(db, assignmentId, tenantId, ct);
        if (assignment is null)
            return new TrainingParticipantCertificateInfo(null, null, false, null);

        if (assignment.Status != TrainingAssignmentStatus.Completed || assignment.CompletedAtUtc is null)
            return new TrainingParticipantCertificateInfo(null, null, false, null);

        if (assignment.CertificateDocumentId is int existingId)
        {
            var existingDoc = await db.EvidenceDocuments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == existingId && d.TenantId == tenantId && !d.IsArchived, ct);

            if (existingDoc is not null)
            {
                return new TrainingParticipantCertificateInfo(
                    existingDoc.Id,
                    CertificateDownloadPath,
                    false,
                    existingDoc.FileName);
            }

            logger.LogWarning(
                "CertificateDocumentId {DocumentId} verweist auf fehlendes Dokument (Assignment {AssignmentId}, Tenant {TenantId})",
                existingId,
                assignmentId,
                tenantId);
        }

        try
        {
            var model = await BuildModelAsync(db, assignment, ct);
            var pdf = await pdfService.GenerateCertificateAsync(model, ct);
            if (pdf is null)
            {
                logger.LogWarning(
                    "PDF-Erzeugung fehlgeschlagen (Assignment {AssignmentId}, Tenant {TenantId})",
                    assignmentId,
                    tenantId);
                return new TrainingParticipantCertificateInfo(null, null, true, null);
            }

            await db.Entry(assignment).ReloadAsync(ct);
            if (assignment.CertificateDocumentId is int concurrentDocumentId)
            {
                var concurrentDoc = await db.EvidenceDocuments
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == concurrentDocumentId && d.TenantId == tenantId && !d.IsArchived, ct);

                if (concurrentDoc is not null)
                {
                    return new TrainingParticipantCertificateInfo(
                        concurrentDoc.Id,
                        CertificateDownloadPath,
                        false,
                        concurrentDoc.FileName);
                }
            }

            await using var pdfStream = new MemoryStream(pdf.Content);
            var storagePath = await storage.SaveAsync(tenantId, pdf.FileName, pdfStream, ct);

            var category = await DocumentCategorySeeder.FindDefaultCategoryAsync(db, tenantId, "Datenschutz", ct);
            var participantName = ResolveParticipantName(assignment);
            var now = DateTime.UtcNow;

            var document = new EvidenceDocument
            {
                TenantId = tenantId,
                DocumentType = DocumentType.Evidence,
                DocumentCategoryId = category?.Id,
                FileName = pdf.FileName,
                ContentType = pdf.ContentType,
                StoragePath = storagePath,
                FileSizeBytes = pdf.Content.Length,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.EvidenceDocuments.Add(document);
            await db.SaveChangesAsync(ct);

            db.DocumentLinks.Add(new DocumentLink
            {
                TenantId = tenantId,
                DocumentId = document.Id,
                LinkedEntityType = DocumentLinkedEntityType.Training,
                LinkedEntityId = assignment.TrainingId,
                CreatedAt = now
            });

            assignment.CertificateDocumentId = document.Id;
            assignment.UpdatedAt = now;
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Teilnahmebescheinigung erstellt (Assignment {AssignmentId}, Document {DocumentId}, Tenant {TenantId})",
                assignmentId,
                document.Id,
                tenantId);

            return new TrainingParticipantCertificateInfo(
                document.Id,
                CertificateDownloadPath,
                false,
                document.FileName);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Teilnahmebescheinigung konnte nicht abgelegt werden (Assignment {AssignmentId}, Tenant {TenantId})",
                assignmentId,
                tenantId);
            return new TrainingParticipantCertificateInfo(null, null, true, null);
        }
    }

    public async Task<EvidenceDocument?> GetCertificateDocumentForParticipantAsync(
        int assignmentId,
        int tenantId,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var assignment = await LoadAssignmentAsync(db, assignmentId, tenantId, ct);
        if (assignment?.CertificateDocumentId is not int documentId)
            return null;

        return await db.EvidenceDocuments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsArchived, ct);
    }

    private static async Task<TrainingAssignment?> LoadAssignmentAsync(
        ApplicationDbContext db,
        int assignmentId,
        int tenantId,
        CancellationToken ct) =>
        await db.TrainingAssignments
            .IgnoreQueryFilters()
            .Include(a => a.Training)
            .Include(a => a.Tenant)
            .Include(a => a.TrainingParticipant)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TenantId == tenantId && !a.IsArchived, ct);

    private static async Task<TrainingCompletionCertificateModel> BuildModelAsync(
        ApplicationDbContext db,
        TrainingAssignment assignment,
        CancellationToken ct)
    {
        var latestAttempt = await db.TrainingQuizAttempts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.TrainingAssignmentId == assignment.Id && a.SubmittedAtUtc != null)
            .OrderByDescending(a => a.AttemptNumber)
            .FirstOrDefaultAsync(ct);

        int? questionCount = null;
        int? correctQuestionCount = null;

        if (latestAttempt is not null)
        {
            var answers = await db.TrainingQuizAnswers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(a => a.TrainingQuizAttemptId == latestAttempt.Id)
                .ToListAsync(ct);

            questionCount = answers
                .Select(a => a.TrainingQuestionId)
                .Distinct()
                .Count();

            correctQuestionCount = answers
                .GroupBy(a => a.TrainingQuestionId)
                .Count(g => g.Any(a => a.PointsAwarded > 0));
        }
        else if (assignment.Training.TrainingTemplateId is int templateId)
        {
            questionCount = await db.TrainingQuestions
                .IgnoreQueryFilters()
                .CountAsync(q => q.TrainingTemplateId == templateId && q.IsActive, ct);
        }

        var participantName = ResolveParticipantName(assignment);
        var organizationName = assignment.Tenant.LegalName?.Trim();
        if (string.IsNullOrWhiteSpace(organizationName))
            organizationName = assignment.Tenant.Name;

        return new TrainingCompletionCertificateModel
        {
            OrganizationName = organizationName,
            ParticipantName = participantName,
            ParticipantEmail = string.IsNullOrWhiteSpace(assignment.ParticipantEmailSnapshot)
                ? null
                : assignment.ParticipantEmailSnapshot,
            TrainingTitle = assignment.Training.Title,
            CompletedAtUtc = assignment.CompletedAtUtc!.Value,
            AssignmentId = assignment.Id,
            HasQuiz = latestAttempt is not null || questionCount > 0,
            QuizPassed = latestAttempt?.Passed,
            QuizScorePercent = latestAttempt?.ScorePercent,
            QuizQuestionCount = questionCount > 0 ? questionCount : null,
            QuizCorrectQuestionCount = correctQuestionCount,
            GeneratedAtUtc = DateTime.UtcNow
        };
    }

    private static string ResolveParticipantName(TrainingAssignment assignment)
    {
        if (!string.IsNullOrWhiteSpace(assignment.ParticipantNameSnapshot))
            return assignment.ParticipantNameSnapshot.Trim();

        if (assignment.TrainingParticipant?.Name is { Length: > 0 } name)
            return name.Trim();

        return assignment.ParticipantEmailSnapshot;
    }
}
