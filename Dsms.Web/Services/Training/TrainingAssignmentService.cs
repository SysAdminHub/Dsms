using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;
using TrainingEntity = Dsms.Web.Domain.Entities.Training;

namespace Dsms.Web.Services.Training;

/// <summary>Zuweisungen von Schulungsteilnehmern zu konkreten Schulungen.</summary>
public class TrainingAssignmentService(
    ApplicationDbContext db,
    ICurrentUserContext currentUser,
    TrainingParticipantService participantService,
    TrainingAccessCodeService accessCodeService,
    IComplianceAuditLogService complianceAuditLog)
{
    public Task<bool> CanManageAsync(CancellationToken ct = default) =>
        participantService.CanManageAsync(ct);

    public Task<bool> CanViewAsync(int tenantId, CancellationToken ct = default) =>
        participantService.CanViewAsync(tenantId, ct);

    public async Task<IReadOnlyList<TrainingAssignmentRow>> GetAssignmentsForTrainingAsync(
        int trainingId,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanViewAsync(tenantId, ct))
            return [];

        var utcNow = DateTime.UtcNow;
        var assignments = await db.TrainingAssignments
            .Include(a => a.TrainingParticipant)
            .Where(a => a.TrainingId == trainingId && a.TenantId == tenantId && !a.IsArchived)
            .OrderBy(a => a.ParticipantNameSnapshot ?? a.ParticipantEmailSnapshot)
            .ThenBy(a => a.ParticipantEmailSnapshot)
            .ToListAsync(ct);

        var assignmentIds = assignments.Select(a => a.Id).ToList();
        var attempts = assignmentIds.Count == 0
            ? []
            : await db.TrainingQuizAttempts
                .Where(a => assignmentIds.Contains(a.TrainingAssignmentId) && a.SubmittedAtUtc != null)
                .OrderByDescending(a => a.AttemptNumber)
                .ToListAsync(ct);

        var latestAttempts = attempts
            .GroupBy(a => a.TrainingAssignmentId)
            .ToDictionary(g => g.Key, g => g.First());

        return assignments.Select(a => ToRow(a, utcNow, latestAttempts.GetValueOrDefault(a.Id))).ToList();
    }

    public async Task<TrainingParticipantStats> GetStatsForTrainingAsync(
        int trainingId,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanViewAsync(tenantId, ct))
            return new TrainingParticipantStats(0, 0, 0);

        var statuses = await db.TrainingAssignments
            .Where(a => a.TrainingId == trainingId
                && a.TenantId == tenantId
                && !a.IsArchived
                && a.Status != TrainingAssignmentStatus.Cancelled)
            .Select(a => a.Status)
            .ToListAsync(ct);

        return new TrainingParticipantStats(
            statuses.Count,
            statuses.Count(s => s is TrainingAssignmentStatus.Invited or TrainingAssignmentStatus.CodeExpired or TrainingAssignmentStatus.Locked),
            statuses.Count(s => s == TrainingAssignmentStatus.Completed));
    }

    public async Task<Dictionary<int, TrainingParticipantStats>> GetStatsForTrainingsAsync(
        int tenantId,
        IReadOnlyList<int> trainingIds,
        CancellationToken ct = default)
    {
        if (!await CanViewAsync(tenantId, ct) || trainingIds.Count == 0)
            return [];

        var rows = await db.TrainingAssignments
            .Where(a => a.TenantId == tenantId
                && trainingIds.Contains(a.TrainingId)
                && !a.IsArchived
                && a.Status != TrainingAssignmentStatus.Cancelled)
            .Select(a => new { a.TrainingId, a.Status })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.TrainingId)
            .ToDictionary(
                g => g.Key,
                g => new TrainingParticipantStats(
                    g.Count(),
                    g.Count(x => x.Status is TrainingAssignmentStatus.Invited or TrainingAssignmentStatus.CodeExpired or TrainingAssignmentStatus.Locked),
                    g.Count(x => x.Status == TrainingAssignmentStatus.Completed)));
    }

    public async Task<TrainingAssignment?> GetAssignmentByIdAsync(
        int assignmentId,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanViewAsync(tenantId, ct))
            return null;

        return await db.TrainingAssignments
            .Include(a => a.Training)
            .Include(a => a.TrainingParticipant)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TenantId == tenantId, ct);
    }

    public async Task<TrainingAssignmentDetail?> GetAssignmentDetailAsync(
        int assignmentId,
        int tenantId,
        CancellationToken ct = default)
    {
        var assignment = await GetAssignmentByIdAsync(assignmentId, tenantId, ct);
        if (assignment is null)
            return null;

        var templateId = assignment.Training.TrainingTemplateId;
        var totalSections = templateId is null
            ? 0
            : await db.TrainingTemplateSections
                .CountAsync(s => s.TrainingTemplateId == templateId && s.IsActive, ct);

        var viewedSections = await db.TrainingAssignmentSectionProgress
            .CountAsync(p => p.TrainingAssignmentId == assignmentId, ct);

        var quizAttempts = await db.TrainingQuizAttempts
            .Where(a => a.TrainingAssignmentId == assignmentId)
            .OrderByDescending(a => a.AttemptNumber)
            .ToListAsync(ct);

        return new TrainingAssignmentDetail(assignment, viewedSections, totalSections, quizAttempts);
    }

    public async Task<TrainingAssignmentOperationResult> AddParticipantToTrainingAsync(
        int trainingId,
        int participantId,
        int tenantId,
        bool sendInvitation,
        CancellationToken ct = default)
    {
        if (!await CanManageAsync(ct))
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.TrainingAccessDenied);

        var training = await GetTrainingAsync(trainingId, tenantId, ct);
        if (training is null)
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.TrainingNotFound);

        var participant = await db.TrainingParticipants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.TenantId == tenantId && p.IsActive && !p.IsArchived, ct);

        if (participant is null)
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.ParticipantNotFound);

        return await CreateAssignmentAsync(
            training,
            participant,
            participant.Email,
            participant.Name,
            sendInvitation,
            ct);
    }

    public async Task<TrainingAssignmentOperationResult> AddParticipantByEmailAsync(
        int trainingId,
        string email,
        string? name,
        string? department,
        int tenantId,
        bool sendInvitation,
        CancellationToken ct = default)
    {
        if (!await CanManageAsync(ct))
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.TrainingAccessDenied);

        var training = await GetTrainingAsync(trainingId, tenantId, ct);
        if (training is null)
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.TrainingNotFound);

        var (participant, _) = await participantService.FindOrCreateByEmailAsync(
            tenantId, email, name, department, ct);

        return await CreateAssignmentAsync(
            training,
            participant,
            participant.Email,
            participant.Name,
            sendInvitation,
            ct);
    }

    public async Task<TrainingAssignmentOperationResult> CancelAssignmentAsync(
        int assignmentId,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanManageAsync(ct))
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.TrainingAccessDenied);

        var assignment = await db.TrainingAssignments
            .Include(a => a.Training)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TenantId == tenantId && !a.IsArchived, ct);

        if (assignment is null)
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.AssignmentNotFound);

        if (assignment.Status == TrainingAssignmentStatus.Cancelled)
            return TrainingAssignmentOperationResult.Ok(assignment.Id);

        assignment.Status = TrainingAssignmentStatus.Cancelled;
        assignment.AccessCodeHash = null;
        assignment.AccessCodeExpiresAtUtc = null;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedByUserId = await currentUser.GetUserIdAsync();

        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogTrainingAssignmentCancelledAsync(
            assignment.TrainingId,
            assignment.Training.Title,
            tenantId,
            assignment.Id,
            assignment.TrainingParticipantId);

        return TrainingAssignmentOperationResult.Ok(assignment.Id);
    }

    public async Task<TrainingBulkImportPreview> PreviewBulkImportAsync(
        int trainingId,
        int tenantId,
        string text,
        CancellationToken ct = default)
    {
        if (!await CanManageAsync(ct))
            return new TrainingBulkImportPreview([], 0, 0, 0, 0, 0);

        var existingEmails = await db.TrainingAssignments
            .Where(a => a.TrainingId == trainingId
                && a.TenantId == tenantId
                && !a.IsArchived
                && a.Status != TrainingAssignmentStatus.Cancelled)
            .Select(a => a.ParticipantEmailSnapshot)
            .ToListAsync(ct);

        var existingSet = existingEmails
            .Select(TrainingParticipantService.NormalizeEmail)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = new List<TrainingBulkImportLineResult>();
        var lineNumber = 0;

        foreach (var rawLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(';').Select(p => p.Trim()).ToArray();
            string? name = null;
            string? email;
            string? department = null;

            if (parts.Length > 3)
            {
                lines.Add(new TrainingBulkImportLineResult(
                    lineNumber, rawLine, false,
                    "Zu viele Spalten. Bitte maximal Name;E-Mail;Abteilung verwenden.",
                    null, null, null, false, false));
                continue;
            }

            if (parts.Length == 1)
            {
                email = parts[0];
            }
            else
            {
                name = string.IsNullOrWhiteSpace(parts[0]) ? null : parts[0];
                email = parts.Length > 1 ? parts[1] : null;
                department = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : null;
            }

            if (string.IsNullOrWhiteSpace(email) || !TrainingParticipantService.IsValidEmail(email))
            {
                lines.Add(new TrainingBulkImportLineResult(
                    lineNumber, rawLine, false, "Ungültige E-Mail-Adresse.", email, name, department, false, false));
                continue;
            }

            var normalized = TrainingParticipantService.NormalizeEmail(email);
            if (!seenInBatch.Add(normalized))
            {
                lines.Add(new TrainingBulkImportLineResult(
                    lineNumber, rawLine, false, "Doppelte E-Mail in der Eingabe.", email, name, department, false, false));
                continue;
            }

            var existingParticipant = await db.TrainingParticipants
                .AnyAsync(p => p.TenantId == tenantId && p.NormalizedEmail == normalized, ct);

            var alreadyAssigned = existingSet.Contains(normalized);
            lines.Add(new TrainingBulkImportLineResult(
                lineNumber, rawLine, true, null, normalized, name, department, alreadyAssigned, existingParticipant));
        }

        var validLines = lines.Where(l => l.IsValid).ToList();
        return new TrainingBulkImportPreview(
            lines,
            validLines.Count(l => !l.AlreadyAssigned),
            validLines.Count(l => l.AlreadyAssigned),
            lines.Count(l => !l.IsValid),
            validLines.Count(l => l.IsExistingParticipant && !l.AlreadyAssigned),
            validLines.Count(l => !l.IsExistingParticipant && !l.AlreadyAssigned));
    }

    public async Task<TrainingInvitationBatchResult> ExecuteBulkImportAsync(
        int trainingId,
        int tenantId,
        string text,
        CancellationToken ct = default)
    {
        var preview = await PreviewBulkImportAsync(trainingId, tenantId, text, ct);
        if (preview.InvalidCount > 0)
        {
            var syntaxErrors = preview.Lines
                .Where(l => !l.IsValid)
                .Select(l => $"Zeile {l.LineNumber}: {l.ErrorMessage}")
                .ToList();
            return new TrainingInvitationBatchResult(0, preview.InvalidCount, syntaxErrors);
        }

        var errors = new List<string>();
        var createdAssignmentIds = new List<int>();

        foreach (var line in preview.Lines.Where(l => l.IsValid && !l.AlreadyAssigned))
        {
            var result = await AddParticipantByEmailAsync(
                trainingId,
                line.Email!,
                line.Name,
                line.Department,
                tenantId,
                sendInvitation: false,
                ct);

            if (!result.Success)
            {
                errors.Add($"Zeile {line.LineNumber}: {result.Message}");
                continue;
            }

            if (result.AssignmentId is int assignmentId)
                createdAssignmentIds.Add(assignmentId);
        }

        return new TrainingInvitationBatchResult(createdAssignmentIds.Count, errors.Count, errors);
    }

    /// <summary>TODO: Vollständige Portal-Validierung in Prompt 5.</summary>
    public async Task<TrainingAccessCodeValidationResult> ValidateAccessCodeAsync(
        string email,
        string code,
        int? trainingId = null,
        CancellationToken ct = default)
    {
        var normalized = TrainingParticipantService.NormalizeEmail(email);
        var utcNow = DateTime.UtcNow;

        var query = db.TrainingAssignments
            .Where(a => a.ParticipantEmailSnapshot == normalized
                && !a.IsArchived
                && a.Status != TrainingAssignmentStatus.Cancelled);

        if (trainingId is int tid)
            query = query.Where(a => a.TrainingId == tid);

        var assignments = await query.ToListAsync(ct);
        if (assignments.Count == 0)
            return TrainingAccessCodeValidationResult.NotFound;

        foreach (var assignment in assignments)
        {
            RefreshExpiredStatus(assignment, utcNow);
            var result = accessCodeService.ValidateAccessCode(assignment, code, utcNow);
            if (result == TrainingAccessCodeValidationResult.Valid)
            {
                await db.SaveChangesAsync(ct);
                return result;
            }
        }

        var target = assignments.FirstOrDefault(a => !string.IsNullOrEmpty(a.AccessCodeHash));
        if (target is not null)
        {
            accessCodeService.ApplyFailedAccessAttempt(target, utcNow);
            await db.SaveChangesAsync(ct);
        }

        return TrainingAccessCodeValidationResult.InvalidCode;
    }

    public async Task RegisterFailedAccessAttemptAsync(
        int assignmentId,
        int tenantId,
        CancellationToken ct = default)
    {
        var assignment = await db.TrainingAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TenantId == tenantId, ct);

        if (assignment is null)
            return;

        accessCodeService.ApplyFailedAccessAttempt(assignment, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
    }

    public async Task ResetFailedAccessAttemptsAsync(
        int assignmentId,
        int tenantId,
        CancellationToken ct = default)
    {
        var assignment = await db.TrainingAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TenantId == tenantId, ct);

        if (assignment is null)
            return;

        accessCodeService.ResetFailedAccessAttempts(assignment);
        await db.SaveChangesAsync(ct);
    }

    private async Task<TrainingAssignmentOperationResult> CreateAssignmentAsync(
        TrainingEntity training,
        TrainingParticipant participant,
        string emailSnapshot,
        string? nameSnapshot,
        bool sendInvitation,
        CancellationToken ct)
    {
        var normalized = TrainingParticipantService.NormalizeEmail(emailSnapshot);
        var duplicate = await db.TrainingAssignments.AnyAsync(a =>
            a.TrainingId == training.Id
            && a.TenantId == training.TenantId
            && a.ParticipantEmailSnapshot == normalized
            && !a.IsArchived
            && a.Status != TrainingAssignmentStatus.Cancelled, ct);

        if (duplicate)
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.ParticipantAlreadyAssigned);

        var userId = await currentUser.GetUserIdAsync();
        var now = DateTime.UtcNow;
        var assignment = new TrainingAssignment
        {
            TenantId = training.TenantId,
            TrainingId = training.Id,
            TrainingParticipantId = participant.Id,
            ParticipantNameSnapshot = nameSnapshot,
            ParticipantEmailSnapshot = normalized,
            Status = TrainingAssignmentStatus.Assigned,
            CreatedAt = now,
            CreatedByUserId = userId
        };

        db.TrainingAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);

        await complianceAuditLog.LogTrainingParticipantAssignedAsync(
            training.Id,
            training.Title,
            training.TenantId,
            assignment.Id,
            participant.Id);

        if (sendInvitation)
        {
            // Invitation handled by caller via TrainingInvitationService to avoid circular dependency
            return TrainingAssignmentOperationResult.Ok(assignment.Id, "Zuweisung erstellt. Einladung wird gesendet.");
        }

        return TrainingAssignmentOperationResult.Ok(assignment.Id);
    }

    private async Task<TrainingEntity?> GetTrainingAsync(int trainingId, int tenantId, CancellationToken ct) =>
        await db.Trainings.FirstOrDefaultAsync(t => t.Id == trainingId && t.TenantId == tenantId, ct);

    private static TrainingAssignmentRow ToRow(
        TrainingAssignment assignment,
        DateTime utcNow,
        TrainingQuizAttempt? latestQuizAttempt)
    {
        var isExpired = assignment.AccessCodeExpiresAtUtc is not null
            && assignment.AccessCodeExpiresAtUtc <= utcNow
            && assignment.Status is TrainingAssignmentStatus.Invited or TrainingAssignmentStatus.CodeExpired;

        var isLocked = assignment.LockedUntilUtc is not null && assignment.LockedUntilUtc > utcNow;

        return new TrainingAssignmentRow(
            assignment,
            assignment.ParticipantNameSnapshot
                ?? assignment.TrainingParticipant?.Name
                ?? assignment.ParticipantEmailSnapshot,
            isExpired,
            isLocked,
            latestQuizAttempt?.Passed,
            latestQuizAttempt?.ScorePercent);
    }

    private void RefreshExpiredStatus(TrainingAssignment assignment, DateTime utcNow)
    {
        if (assignment.Status == TrainingAssignmentStatus.Invited
            && accessCodeService.IsCodeExpired(assignment, utcNow))
        {
            assignment.Status = TrainingAssignmentStatus.CodeExpired;
        }
    }
}
