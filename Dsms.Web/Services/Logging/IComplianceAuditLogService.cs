namespace Dsms.Web.Services.Logging;

/// <summary>
/// Zentrale Audit-Protokollierung für fachliche DSMS-Kernbereiche.
/// Alle Methoden schreiben Audit-Logs mit IsVisibleToAdmin = true (außer explizit anders dokumentiert).
/// </summary>
public interface IComplianceAuditLogService
{
    Task LogProcessingActivityCreatedAsync(int id, string name, int tenantId);
    Task LogProcessingActivityUpdatedAsync(int id, string name, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogProcessingActivityArchivedAsync(int id, string name, int tenantId);
    Task LogProcessingActivityRestoredAsync(int id, string name, int tenantId);
    Task LogProcessingActivityStatusChangedAsync(int id, string name, int tenantId, object oldStatus, object newStatus);

    Task LogDpiaCreatedAsync(int id, string title, int tenantId);
    Task LogDpiaUpdatedAsync(int id, string title, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogDpiaArchivedAsync(int id, string title, int tenantId);
    Task LogDpiaRestoredAsync(int id, string title, int tenantId);
    Task LogDpiaStatusChangedAsync(int id, string title, int tenantId, object oldStatus, object newStatus);

    Task LogTomCreatedAsync(int id, string title, int tenantId);
    Task LogTomUpdatedAsync(int id, string title, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogTomArchivedAsync(int id, string title, int tenantId);
    Task LogTomRestoredAsync(int id, string title, int tenantId);
    Task LogTomStatusChangedAsync(int id, string title, int tenantId, object oldStatus, object newStatus);

    Task LogProcessorCreatedAsync(int id, string name, int tenantId);
    Task LogProcessorUpdatedAsync(int id, string name, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogProcessorArchivedAsync(int id, string name, int tenantId);
    Task LogProcessorRestoredAsync(int id, string name, int tenantId);
    Task LogProcessorStatusChangedAsync(int id, string name, int tenantId, object oldStatus, object newStatus);

    Task LogMeasureCreatedAsync(int id, string title, int tenantId);
    Task LogMeasureUpdatedAsync(int id, string title, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogMeasureArchivedAsync(int id, string title, int tenantId);
    Task LogMeasureRestoredAsync(int id, string title, int tenantId);
    Task LogMeasureStatusChangedAsync(int id, string title, int tenantId, object oldStatus, object newStatus);
    Task LogMeasureCompletedAsync(int id, string title, int tenantId);

    Task LogAuditCreatedAsync(int id, string title, int tenantId);
    Task LogAuditStartedAsync(int id, string title, int tenantId);
    Task LogAuditUpdatedAsync(int id, string title, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogAuditCompletedAsync(int id, string title, int tenantId);
    Task LogAuditArchivedAsync(int id, string title, int tenantId);
    Task LogAuditAnswerUpdatedAsync(int auditRunId, string auditTitle, int answerId, int tenantId);

    Task LogAuditTemplateCreatedAsync(int id, string title, int? tenantId);
    Task LogAuditTemplateUpdatedAsync(int id, string title, int? tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogAuditTemplateArchivedAsync(int id, string title, int? tenantId);
    Task LogAuditTemplateRestoredAsync(int id, string title, int? tenantId);
    Task LogAuditTemplatePublishedToCommunityAsync(int id, string title, int tenantId);
    Task LogAuditTemplateImportedAsync(int id, string title, int tenantId, int sourceTemplateId);

    Task LogEvidenceDocumentUploadedAsync(int id, string fileName, int tenantId);
    Task LogEvidenceDocumentArchivedAsync(int id, string fileName, int tenantId);
    Task LogEvidenceDocumentRestoredAsync(int id, string fileName, int tenantId);
}
