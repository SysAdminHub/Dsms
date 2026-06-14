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

    Task LogGlobalAuditTemplateQuestionCreatedAsync(int templateId, string templateTitle, int questionId);
    Task LogGlobalAuditTemplateQuestionUpdatedAsync(int templateId, string templateTitle, int questionId);
    Task LogGlobalAuditTemplateQuestionDeletedAsync(int templateId, string templateTitle, int questionId);
    Task LogTenantAuditTemplateQuestionAccessDeniedAsync(int templateId, int? questionId);

    Task LogEvidenceDocumentUploadedAsync(int id, string fileName, int tenantId);
    Task LogEvidenceDocumentUpdatedAsync(int id, string fileName, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogEvidenceDocumentArchivedAsync(int id, string fileName, int tenantId);
    Task LogEvidenceDocumentRestoredAsync(int id, string fileName, int tenantId);

    Task LogDocumentCategoryCreatedAsync(int id, string name, int tenantId);
    Task LogDocumentCategoryUpdatedAsync(int id, string name, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogDocumentCategoryDeactivatedAsync(int id, string name, int tenantId);
    Task LogDocumentCategoryReactivatedAsync(int id, string name, int tenantId);

    Task LogDataProtectionRoleCreatedAsync(int id, string roleTitle, int tenantId);
    Task LogDataProtectionRoleUpdatedAsync(int id, string roleTitle, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogDataProtectionRoleDeactivatedAsync(int id, string roleTitle, int tenantId);
    Task LogDataProtectionRoleReactivatedAsync(int id, string roleTitle, int tenantId);

    Task LogPrivacyIncidentCreatedAsync(int id, string title, int tenantId);
    Task LogPrivacyIncidentUpdatedAsync(int id, string title, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogPrivacyIncidentArchivedAsync(int id, string title, int tenantId);
    Task LogPrivacyIncidentRestoredAsync(int id, string title, int tenantId);
    Task LogPrivacyIncidentStatusChangedAsync(int id, string title, int tenantId, object oldStatus, object newStatus);

    Task LogDataSubjectRequestCreatedAsync(int id, string displayName, int tenantId);
    Task LogDataSubjectRequestUpdatedAsync(int id, string displayName, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogDataSubjectRequestArchivedAsync(int id, string displayName, int tenantId);
    Task LogDataSubjectRequestRestoredAsync(int id, string displayName, int tenantId);
    Task LogDataSubjectRequestStatusChangedAsync(int id, string displayName, int tenantId, object oldStatus, object newStatus);
    Task LogDataSubjectRequestAnonymizedAsync(int id, string displayName, int tenantId, string? note);

    Task LogTrainingTemplateCreatedAsync(int id, string title, int? tenantId);
    Task LogTrainingTemplateUpdatedAsync(int id, string title, int? tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogTrainingTemplateArchivedAsync(int id, string title, int? tenantId);
    Task LogTrainingTemplateCopiedAsync(int id, string title, int tenantId, int sourceTemplateId);
    Task LogTrainingTemplateSectionChangedAsync(int templateId, string templateTitle, int? tenantId, int sectionId, string sectionTitle);
    Task LogTrainingTemplateAssetUploadedAsync(int templateId, string templateTitle, int? tenantId, int assetId, string assetKey);
    Task LogTrainingTemplateAssetArchivedAsync(int templateId, string templateTitle, int? tenantId, int assetId, string assetKey);
    Task LogTrainingQuestionChangedAsync(int templateId, string templateTitle, int? tenantId, int questionId);
    Task LogTrainingTemplateSubmittedToCommunityAsync(int id, string title, int tenantId);
    Task LogTrainingTemplateCommunityApprovedAsync(int globalTemplateId, string title, int sourceTemplateId, int? sourceTenantId, int questionCountCopied);
    Task LogTrainingTemplateCommunityRejectedAsync(int id, string title, int tenantId);
    Task LogTrainingTemplateCommunityReviewOpenedAsync(int id, string title);
    Task LogTrainingTemplateCommunityQuizReviewOpenedAsync(int templateId, string title, int? tenantId, int questionCount);

    Task LogTrainingCreatedAsync(int id, string title, int tenantId);
    Task LogTrainingCreatedFromTemplateAsync(int id, string title, int tenantId, int templateId, string templateTitle);
    Task LogTrainingUpdatedAsync(int id, string title, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes);
    Task LogTrainingArchivedAsync(int id, string title, int tenantId);
    Task LogTrainingRestoredAsync(int id, string title, int tenantId);
    Task LogTrainingStatusChangedAsync(int id, string title, int tenantId, object oldStatus, object newStatus);
    Task LogTrainingProofLinkedAsync(int trainingId, string trainingTitle, int tenantId, int documentId, string fileName);
    Task LogTrainingProofRemovedAsync(int trainingId, string trainingTitle, int tenantId, int documentId, string fileName);

    Task LogTrainingParticipantCreatedAsync(int participantId, int tenantId);
    Task LogTrainingParticipantUpdatedAsync(int participantId, int tenantId);
    Task LogTrainingParticipantArchivedAsync(int participantId, int tenantId);
    Task LogTrainingParticipantDeactivatedAsync(int participantId, int tenantId);
    Task LogTrainingParticipantReactivatedAsync(int participantId, int tenantId);
    Task LogTrainingParticipantAssignedAsync(int trainingId, string trainingTitle, int tenantId, int assignmentId, int? participantId);
    Task LogTrainingAssignmentCancelledAsync(int trainingId, string trainingTitle, int tenantId, int assignmentId, int? participantId);
    Task LogTrainingInvitationSentAsync(int trainingId, string trainingTitle, int tenantId, int assignmentId, int? participantId);
    Task LogTrainingInvitationResentAsync(int trainingId, string trainingTitle, int tenantId, int assignmentId, int? participantId);
}
