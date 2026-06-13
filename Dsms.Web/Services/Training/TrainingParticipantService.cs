using System.Net.Mail;
using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
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
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Email.Contains(term)
                || (p.Name != null && p.Name.Contains(term))
                || (p.Department != null && p.Department.Contains(term)));
        }

        return await query
            .OrderBy(p => p.Name ?? p.Email)
            .ThenBy(p => p.Email)
            .ToListAsync(ct);
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
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Email == normalized, ct);
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
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Email == normalized, ct);

        if (existing is not null)
            return TrainingAssignmentOperationResult.Fail("Ein Teilnehmer mit dieser E-Mail existiert bereits.");

        var userId = await currentUser.GetUserIdAsync();
        var now = DateTime.UtcNow;
        var participant = new TrainingParticipant
        {
            TenantId = tenantId,
            Email = normalized,
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
            .AnyAsync(p => p.TenantId == tenantId && p.Email == normalized && p.Id != id, ct);

        if (duplicate)
            return TrainingAssignmentOperationResult.Fail("Ein anderer Teilnehmer verwendet bereits diese E-Mail.");

        participant.Email = normalized;
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
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Email == normalized, ct);

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
}
