using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

public enum DataSubjectRequestAnonymizeResult
{
    Success,
    NotFound,
    AlreadyAnonymized,
    PermissionDenied,
    InvalidStatus,
    TenantMismatch
}

public sealed class DataSubjectRequestAnonymizeOutcome
{
    public DataSubjectRequestAnonymizeResult Result { get; init; }
    public string? Message { get; init; }
}

/// <summary>Verwaltung und Anonymisierung von Betroffenenanfragen.</summary>
public class DataSubjectRequestService(
    ApplicationDbContext db,
    IUserAccessService userAccess,
    IComplianceAuditLogService complianceAuditLog)
{
    public async Task<DataSubjectRequestAnonymizeOutcome> AnonymizeAsync(
        int requestId,
        int tenantId,
        string? currentUserId,
        string? anonymizationNote,
        CancellationToken ct = default)
    {
        if (!await userAccess.CanAnonymizeDataSubjectRequestsAsync())
        {
            return new DataSubjectRequestAnonymizeOutcome
            {
                Result = DataSubjectRequestAnonymizeResult.PermissionDenied,
                Message = "Keine Berechtigung zur Anonymisierung."
            };
        }

        if (!await userAccess.CanAccessTenantAsync(tenantId))
        {
            return new DataSubjectRequestAnonymizeOutcome
            {
                Result = DataSubjectRequestAnonymizeResult.TenantMismatch,
                Message = "Kein Zugriff auf diesen Mandanten."
            };
        }

        var request = await db.DataSubjectRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.TenantId == tenantId, ct);

        if (request is null)
        {
            return new DataSubjectRequestAnonymizeOutcome
            {
                Result = DataSubjectRequestAnonymizeResult.NotFound,
                Message = "Betroffenenanfrage nicht gefunden."
            };
        }

        if (request.PersonalDataAnonymized)
        {
            return new DataSubjectRequestAnonymizeOutcome
            {
                Result = DataSubjectRequestAnonymizeResult.AlreadyAnonymized,
                Message = "Die Falldaten wurden bereits anonymisiert."
            };
        }

        if (!DataSubjectRequestLabels.AllowsAnonymization(request.Status))
        {
            return new DataSubjectRequestAnonymizeOutcome
            {
                Result = DataSubjectRequestAnonymizeResult.InvalidStatus,
                Message = "Anonymisierung ist nur bei abgeschlossenen oder beantworteten Anfragen möglich."
            };
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            var placeholder = DataSubjectRequestLabels.AnonymizedPlaceholder;

            request.DataSubjectName = placeholder;
            request.DataSubjectEmail = null;
            request.DataSubjectPhone = null;
            request.DataSubjectReference = placeholder;
            request.IdentityVerificationNote = placeholder;
            request.Description = placeholder;
            request.InternalNotes = placeholder;
            request.ResultSummary = placeholder;
            request.DeadlineExtensionReason = string.IsNullOrWhiteSpace(request.DeadlineExtensionReason)
                ? request.DeadlineExtensionReason
                : placeholder;

            request.PersonalDataAnonymized = true;
            request.PersonalDataAnonymizedAt = now;
            request.PersonalDataAnonymizedByUserId = currentUserId;
            request.AnonymizationNote = string.IsNullOrWhiteSpace(anonymizationNote) ? null : anonymizationNote.Trim();
            request.UpdatedAt = now;
            request.UpdatedByUserId = currentUserId;

            await db.SaveChangesAsync(ct);

            await complianceAuditLog.LogDataSubjectRequestAnonymizedAsync(
                request.Id,
                DataSubjectRequestLabels.GetAuditDisplayName(request),
                tenantId,
                anonymizationNote);

            await transaction.CommitAsync(ct);

            return new DataSubjectRequestAnonymizeOutcome
            {
                Result = DataSubjectRequestAnonymizeResult.Success,
                Message = "Falldaten wurden endgültig anonymisiert."
            };
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
