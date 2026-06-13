using System.Net.Mail;
using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Training;

/// <summary>Stammdatenverwaltung für Schulungsteilnehmer (keine App-Benutzer).</summary>
public class TrainingParticipantService(
    ApplicationDbContext db,
    IUserAccessService access,
    ICurrentUserContext currentUser,
    IComplianceAuditLogService complianceAuditLog)
{
    public async Task<bool> CanManageAsync(CancellationToken ct = default) =>
        await access.CanEditComplianceContentAsync();

    public async Task<bool> CanViewAsync(int tenantId, CancellationToken ct = default)
    {
        if (!await access.CanAccessTenantAsync(tenantId))
            return false;

        return await access.CanEditComplianceContentAsync()
            || await access.IsAuditorAsync()
            || await access.IsSuperuserAsync();
    }

    public async Task<IReadOnlyList<TrainingParticipant>> GetActiveParticipantsAsync(
        int tenantId,
        string? search = null,
        CancellationToken ct = default)
    {
        if (!await CanViewAsync(tenantId, ct))
            return [];

        var query = db.TrainingParticipants
            .Where(p => p.TenantId == tenantId && p.IsActive && !p.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
            query = ApplySearch(query, search.Trim());

        return await query
            .OrderBy(p => p.Name ?? p.Email)
            .ThenBy(p => p.Email)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TrainingParticipantListRow>> GetParticipantOverviewAsync(
        int tenantId,
        string? search = null,
        TrainingParticipantListFilter filter = TrainingParticipantListFilter.Active,
        string? department = null,
        CancellationToken ct = default)
    {
        if (!await CanViewAsync(tenantId, ct))
            return [];

        var query = db.TrainingParticipants.Where(p => p.TenantId == tenantId);

        query = filter switch
        {
            TrainingParticipantListFilter.Active => query.Where(p => p.IsActive && !p.IsArchived),
            TrainingParticipantListFilter.Inactive => query.Where(p => !p.IsActive || p.IsArchived),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(search))
            query = ApplySearch(query, search.Trim());

        if (!string.IsNullOrWhiteSpace(department))
            query = query.Where(p => p.Department == department.Trim());

        var participants = await query
            .OrderBy(p => p.Name ?? p.Email)
            .ThenBy(p => p.Email)
            .ToListAsync(ct);

        if (participants.Count == 0)
            return [];

        var participantIds = participants.Select(p => p.Id).ToList();
        var assignments = await db.TrainingAssignments
            .Where(a => a.TenantId == tenantId
                && a.TrainingParticipantId != null
                && participantIds.Contains(a.TrainingParticipantId.Value)
                && !a.IsArchived)
            .ToListAsync(ct);

        var statsByParticipant = BuildAssignmentStats(assignments);
        var rows = participants.Select(p =>
        {
            var stats = statsByParticipant.GetValueOrDefault(p.Id);
            return new TrainingParticipantListRow(
                p,
                stats?.Total ?? 0,
                stats?.Invited ?? 0,
                stats?.Started ?? 0,
                stats?.Completed ?? 0,
                stats?.Open ?? 0,
                stats?.LastParticipationUtc);
        }).ToList();

        return filter switch
        {
            TrainingParticipantListFilter.HasOpen => rows.Where(r => r.OpenCount > 0).ToList(),
            TrainingParticipantListFilter.HasCompleted => rows.Where(r => r.CompletedCount > 0).ToList(),
            TrainingParticipantListFilter.NeverCompleted => rows.Where(r => r.CompletedCount == 0).ToList(),
            _ => rows
        };
    }

    public async Task<IReadOnlyList<string>> GetDepartmentsAsync(int tenantId, CancellationToken ct = default)
    {
        if (!await CanViewAsync(tenantId, ct))
            return [];

        return await db.TrainingParticipants
            .Where(p => p.TenantId == tenantId && p.Department != null && p.Department != "")
            .Select(p => p.Department!)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync(ct);
    }

    public async Task<TrainingParticipantDetail?> GetParticipantDetailAsync(
        int id,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanViewAsync(tenantId, ct))
            return null;

        var participant = await db.TrainingParticipants
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (participant is null)
            return null;

        var assignments = await db.TrainingAssignments
            .Include(a => a.Training)
            .Where(a => a.TenantId == tenantId
                && a.TrainingParticipantId == id
                && !a.IsArchived)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var assignmentIds = assignments.Select(a => a.Id).ToList();
        var latestAttempts = assignmentIds.Count == 0
            ? []
            : await db.TrainingQuizAttempts
                .Where(q => assignmentIds.Contains(q.TrainingAssignmentId) && q.SubmittedAtUtc != null)
                .OrderByDescending(q => q.AttemptNumber)
                .ToListAsync(ct);

        var attemptByAssignment = latestAttempts
            .GroupBy(q => q.TrainingAssignmentId)
            .ToDictionary(g => g.Key, g => g.First());

        var history = assignments.Select(a =>
        {
            attemptByAssignment.TryGetValue(a.Id, out var attempt);
            var emailDiffers = !string.Equals(
                a.ParticipantEmailSnapshot,
                participant.Email,
                StringComparison.OrdinalIgnoreCase);

            return new TrainingParticipantAssignmentHistoryRow(
                a,
                a.Training,
                attempt?.Passed,
                attempt?.ScorePercent,
                emailDiffers);
        }).ToList();

        return new TrainingParticipantDetail(participant, history);
    }

    public async Task<TrainingParticipant?> GetByIdAsync(int id, int tenantId, CancellationToken ct = default)
    {
        if (!await CanViewAsync(tenantId, ct))
            return null;

        return await db.TrainingParticipants
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);
    }

    public async Task<TrainingParticipant?> FindByEmailAsync(
        int tenantId,
        string email,
        CancellationToken ct = default)
    {
        if (!await CanViewAsync(tenantId, ct))
            return null;

        var normalized = NormalizeEmail(email);
        return await db.TrainingParticipants
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.NormalizedEmail == normalized, ct);
    }

    public async Task<TrainingAssignmentOperationResult> CreateParticipantAsync(
        int tenantId,
        string email,
        string? name,
        string? department,
        string? externalReference,
        CancellationToken ct = default)
    {
        if (!await CanManageAsync(ct))
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.TrainingAccessDenied);

        if (!IsValidEmail(email))
            return TrainingAssignmentOperationResult.Fail("Ungültige E-Mail-Adresse.");

        var normalized = NormalizeEmail(email);
        var existing = await db.TrainingParticipants
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.NormalizedEmail == normalized, ct);

        if (existing is not null)
            return TrainingAssignmentOperationResult.Fail("Ein Teilnehmer mit dieser E-Mail existiert bereits.");

        var userId = await currentUser.GetUserIdAsync();
        var now = DateTime.UtcNow;
        var participant = new TrainingParticipant
        {
            TenantId = tenantId,
            Email = normalized,
            NormalizedEmail = normalized,
            Name = NormalizeOptional(name),
            Department = NormalizeOptional(department),
            ExternalReference = NormalizeOptional(externalReference),
            IsActive = true,
            CreatedAt = now,
            CreatedByUserId = userId
        };

        db.TrainingParticipants.Add(participant);
        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogTrainingParticipantCreatedAsync(participant.Id, tenantId);

        return TrainingAssignmentOperationResult.Ok(participant.Id);
    }

    public async Task<TrainingAssignmentOperationResult> UpdateParticipantAsync(
        int id,
        int tenantId,
        string email,
        string? name,
        string? department,
        string? externalReference,
        bool isActive,
        CancellationToken ct = default)
    {
        if (!await CanManageAsync(ct))
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.TrainingAccessDenied);

        if (!IsValidEmail(email))
            return TrainingAssignmentOperationResult.Fail("Ungültige E-Mail-Adresse.");

        var participant = await db.TrainingParticipants
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (participant is null)
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.ParticipantNotFound);

        var normalized = NormalizeEmail(email);
        var duplicate = await db.TrainingParticipants
            .AnyAsync(p => p.TenantId == tenantId && p.NormalizedEmail == normalized && p.Id != id, ct);

        if (duplicate)
            return TrainingAssignmentOperationResult.Fail("Ein anderer Teilnehmer verwendet bereits diese E-Mail.");

        participant.Email = normalized;
        participant.NormalizedEmail = normalized;
        participant.Name = NormalizeOptional(name);
        participant.Department = NormalizeOptional(department);
        participant.ExternalReference = NormalizeOptional(externalReference);
        participant.IsActive = isActive;
        participant.UpdatedAt = DateTime.UtcNow;
        participant.UpdatedByUserId = await currentUser.GetUserIdAsync();

        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogTrainingParticipantUpdatedAsync(participant.Id, tenantId);

        return TrainingAssignmentOperationResult.Ok(participant.Id);
    }

    public async Task<TrainingAssignmentOperationResult> SetParticipantActiveAsync(
        int id,
        int tenantId,
        bool isActive,
        CancellationToken ct = default)
    {
        if (!await CanManageAsync(ct))
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.TrainingAccessDenied);

        var participant = await db.TrainingParticipants
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && !p.IsArchived, ct);

        if (participant is null)
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.ParticipantNotFound);

        if (participant.IsActive == isActive)
            return TrainingAssignmentOperationResult.Ok(participant.Id);

        participant.IsActive = isActive;
        participant.UpdatedAt = DateTime.UtcNow;
        participant.UpdatedByUserId = await currentUser.GetUserIdAsync();

        await db.SaveChangesAsync(ct);

        if (isActive)
            await complianceAuditLog.LogTrainingParticipantReactivatedAsync(participant.Id, tenantId);
        else
            await complianceAuditLog.LogTrainingParticipantDeactivatedAsync(participant.Id, tenantId);

        return TrainingAssignmentOperationResult.Ok(participant.Id);
    }

    public async Task<TrainingAssignmentOperationResult> ArchiveParticipantAsync(
        int id,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanManageAsync(ct))
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.TrainingAccessDenied);

        var participant = await db.TrainingParticipants
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && !p.IsArchived, ct);

        if (participant is null)
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.ParticipantNotFound);

        participant.IsArchived = true;
        participant.ArchivedAt = DateTime.UtcNow;
        participant.ArchivedByUserId = await currentUser.GetUserIdAsync();
        participant.IsActive = false;
        participant.UpdatedAt = DateTime.UtcNow;
        participant.UpdatedByUserId = await currentUser.GetUserIdAsync();

        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogTrainingParticipantArchivedAsync(participant.Id, tenantId);

        return TrainingAssignmentOperationResult.Ok(participant.Id);
    }

    public async Task<(TrainingParticipant Participant, bool Created)> FindOrCreateByEmailAsync(
        int tenantId,
        string email,
        string? name,
        string? department,
        CancellationToken ct = default)
    {
        if (!await CanManageAsync(ct))
            throw new InvalidOperationException(TrainingLabels.TrainingAccessDenied);

        if (!IsValidEmail(email))
            throw new ArgumentException("Ungültige E-Mail-Adresse.", nameof(email));

        var normalized = NormalizeEmail(email);
        var existing = await db.TrainingParticipants
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.NormalizedEmail == normalized, ct);

        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(existing.Name))
                existing.Name = name.Trim();

            if (!string.IsNullOrWhiteSpace(department) && string.IsNullOrWhiteSpace(existing.Department))
                existing.Department = department.Trim();

            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = await currentUser.GetUserIdAsync();
            }

            await db.SaveChangesAsync(ct);
            return (existing, false);
        }

        var result = await CreateParticipantAsync(tenantId, normalized, name, department, null, ct);
        if (!result.Success || result.AssignmentId is null)
            throw new InvalidOperationException(result.Message ?? "Teilnehmer konnte nicht erstellt werden.");

        var created = await db.TrainingParticipants
            .FirstAsync(p => p.Id == result.AssignmentId.Value, ct);

        return (created, true);
    }

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            _ = new MailAddress(email.Trim());
            return email.Contains('@');
        }
        catch
        {
            return false;
        }
    }

    public static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<TrainingParticipant> ApplySearch(IQueryable<TrainingParticipant> query, string term) =>
        query.Where(p =>
            p.Email.Contains(term)
            || p.NormalizedEmail.Contains(term)
            || (p.Name != null && p.Name.Contains(term))
            || (p.Department != null && p.Department.Contains(term))
            || (p.ExternalReference != null && p.ExternalReference.Contains(term)));

    private sealed record AssignmentStats(
        int Total,
        int Invited,
        int Started,
        int Completed,
        int Open,
        DateTime? LastParticipationUtc);

    private static Dictionary<int, AssignmentStats> BuildAssignmentStats(IReadOnlyList<TrainingAssignment> assignments)
    {
        return assignments
            .Where(a => a.TrainingParticipantId is int pid)
            .GroupBy(a => a.TrainingParticipantId!.Value)
            .ToDictionary(g => g.Key, g =>
            {
                var active = g.Where(a => a.Status != TrainingAssignmentStatus.Cancelled).ToList();
                var invited = active.Count(a => a.Status is TrainingAssignmentStatus.Invited
                    or TrainingAssignmentStatus.CodeExpired
                    or TrainingAssignmentStatus.Locked
                    or TrainingAssignmentStatus.Assigned);
                var started = active.Count(a => a.Status == TrainingAssignmentStatus.Started);
                var completed = active.Count(a => a.Status == TrainingAssignmentStatus.Completed);
                var open = active.Count(a => a.Status is not TrainingAssignmentStatus.Completed
                    and not TrainingAssignmentStatus.Cancelled);
                var last = active
                    .Select(a => a.ParticipationConfirmedAtUtc ?? a.CompletedAtUtc)
                    .Where(d => d is not null)
                    .Select(d => d!.Value)
                    .DefaultIfEmpty()
                    .Max();

                return new AssignmentStats(
                    active.Count,
                    invited,
                    started,
                    completed,
                    open,
                    last == default ? null : last);
            });
    }
}
