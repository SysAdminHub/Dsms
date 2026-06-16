using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Services;

/// <summary>Mandantensichere Zugriffs- und Verwaltungslogik für Audit-Vorlagen.</summary>
public interface IAuditTemplateService
{
    Task<bool> CanViewAsync(AuditTemplate template, CancellationToken ct = default);
    Task<bool> CanEditAsync(AuditTemplate template, CancellationToken ct = default);
    Task<bool> CanArchiveAsync(AuditTemplate template, CancellationToken ct = default);
    Task<bool> CanCreateTenantTemplateAsync(CancellationToken ct = default);
    Task<bool> CanCreateOfficialTemplateAsync(CancellationToken ct = default);
    Task<bool> CanSubmitToCommunityAsync(AuditTemplate template, CancellationToken ct = default);
    Task<bool> CanReviewCommunityAsync(CancellationToken ct = default);
    Task<bool> CanCopyToTenantAsync(AuditTemplate template, CancellationToken ct = default);

    Task<AuditTemplate?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<AuditTemplate?> GetSubmittedForReviewAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<AuditTemplate>> ListVisibleAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AuditTemplate>> ListGlobalTemplatesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AuditTemplate>> ListActiveForAuditStartAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CommunitySubmissionRow>> ListSubmittedForReviewAsync(CancellationToken ct = default);

    Task<AuditTemplateOperationResult> SubmitToCommunityAsync(int id, CancellationToken ct = default);
    Task<AuditTemplateOperationResult> ApproveCommunityAsync(int id, string? reviewComment, CancellationToken ct = default);
    Task<AuditTemplateOperationResult> RejectCommunityAsync(int id, string? reviewComment, CancellationToken ct = default);
    Task<AuditTemplateOperationResult> CopyToTenantAsync(int sourceId, CancellationToken ct = default);

    Task<ArchiveOperationResult> ArchiveAsync(int id, CancellationToken ct = default);
    Task<ArchiveOperationResult> RestoreAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDependencyWarningsAsync(int id, CancellationToken ct = default);

    Task CreateAnswerSnapshotsAsync(AuditRun run, AuditTemplate template, CancellationToken ct = default);

    Task<AuditTemplateOperationResult> AddQuestionAsync(
        int templateId, int sortOrder, string? category, string text, CancellationToken ct = default);

    Task<AuditTemplateOperationResult> UpdateQuestionAsync(
        int templateId, int questionId, int sortOrder, string? category, string text, CancellationToken ct = default);

    Task<AuditTemplateOperationResult> DeleteQuestionAsync(
        int templateId, int questionId, CancellationToken ct = default);
}

public sealed record AuditTemplateOperationResult(bool Succeeded, string? Message = null);

public sealed record CommunitySubmissionRow(
    AuditTemplate Template,
    string TenantName,
    string? SubmittedByDisplayName);
