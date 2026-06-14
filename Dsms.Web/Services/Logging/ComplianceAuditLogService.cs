namespace Dsms.Web.Services.Logging;

/// <summary>
/// Schreibt fachliche Audit-Logs für Compliance-Kernbereiche über den zentralen <see cref="ILogService"/>.
/// </summary>
public sealed class ComplianceAuditLogService(ILogService logService) : IComplianceAuditLogService
{
    public Task LogProcessingActivityCreatedAsync(int id, string name, int tenantId) =>
        LogAsync("ProcessingActivityCreated", "Verarbeitungstätigkeit wurde erstellt.",
            "ProcessingActivity", id, name, tenantId);

    public Task LogProcessingActivityUpdatedAsync(int id, string name, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("ProcessingActivityUpdated", "Verarbeitungstätigkeit wurde geändert.",
            "ProcessingActivity", id, name, tenantId, changes);

    public Task LogProcessingActivityArchivedAsync(int id, string name, int tenantId) =>
        LogAsync("ProcessingActivityArchived", "Verarbeitungstätigkeit wurde archiviert.",
            "ProcessingActivity", id, name, tenantId);

    public Task LogProcessingActivityRestoredAsync(int id, string name, int tenantId) =>
        LogAsync("ProcessingActivityRestored", "Verarbeitungstätigkeit wurde wiederhergestellt.",
            "ProcessingActivity", id, name, tenantId);

    public Task LogProcessingActivityStatusChangedAsync(int id, string name, int tenantId, object oldStatus, object newStatus) =>
        LogStatusChangeAsync("ProcessingActivityStatusChanged", "Status der Verarbeitungstätigkeit wurde geändert.",
            "ProcessingActivity", id, name, tenantId, oldStatus, newStatus);

    public Task LogDpiaCreatedAsync(int id, string title, int tenantId) =>
        LogAsync("DpiaCreated", "DSFA wurde erstellt.", "Dpia", id, title, tenantId);

    public Task LogDpiaUpdatedAsync(int id, string title, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("DpiaUpdated", "DSFA wurde geändert.", "Dpia", id, title, tenantId, changes);

    public Task LogDpiaArchivedAsync(int id, string title, int tenantId) =>
        LogAsync("DpiaArchived", "DSFA wurde archiviert.", "Dpia", id, title, tenantId);

    public Task LogDpiaRestoredAsync(int id, string title, int tenantId) =>
        LogAsync("DpiaRestored", "DSFA wurde wiederhergestellt.", "Dpia", id, title, tenantId);

    public Task LogDpiaStatusChangedAsync(int id, string title, int tenantId, object oldStatus, object newStatus) =>
        LogStatusChangeAsync("DpiaStatusChanged", "Status der DSFA wurde geändert.", "Dpia", id, title, tenantId, oldStatus, newStatus);

    public Task LogTomCreatedAsync(int id, string title, int tenantId) =>
        LogAsync("TomCreated", "TOM wurde erstellt.", "Tom", id, title, tenantId);

    public Task LogTomUpdatedAsync(int id, string title, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("TomUpdated", "TOM wurde geändert.", "Tom", id, title, tenantId, changes);

    public Task LogTomArchivedAsync(int id, string title, int tenantId) =>
        LogAsync("TomArchived", "TOM wurde archiviert.", "Tom", id, title, tenantId);

    public Task LogTomRestoredAsync(int id, string title, int tenantId) =>
        LogAsync("TomRestored", "TOM wurde wiederhergestellt.", "Tom", id, title, tenantId);

    public Task LogTomStatusChangedAsync(int id, string title, int tenantId, object oldStatus, object newStatus) =>
        LogStatusChangeAsync("TomStatusChanged", "Status des TOM wurde geändert.", "Tom", id, title, tenantId, oldStatus, newStatus);

    public Task LogProcessorCreatedAsync(int id, string name, int tenantId) =>
        LogAsync("ProcessorCreated", "Dienstleister wurde erstellt.", "Processor", id, name, tenantId);

    public Task LogProcessorUpdatedAsync(int id, string name, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("ProcessorUpdated", "Dienstleister wurde geändert.", "Processor", id, name, tenantId, changes);

    public Task LogProcessorArchivedAsync(int id, string name, int tenantId) =>
        LogAsync("ProcessorArchived", "Dienstleister wurde archiviert.", "Processor", id, name, tenantId);

    public Task LogProcessorRestoredAsync(int id, string name, int tenantId) =>
        LogAsync("ProcessorRestored", "Dienstleister wurde wiederhergestellt.", "Processor", id, name, tenantId);

    public Task LogProcessorStatusChangedAsync(int id, string name, int tenantId, object oldStatus, object newStatus) =>
        LogStatusChangeAsync("ProcessorStatusChanged", "Status des Dienstleisters wurde geändert.", "Processor", id, name, tenantId, oldStatus, newStatus);

    public Task LogMeasureCreatedAsync(int id, string title, int tenantId) =>
        LogAsync("MeasureCreated", "Maßnahme wurde erstellt.", "Measure", id, title, tenantId);

    public Task LogMeasureUpdatedAsync(int id, string title, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("MeasureUpdated", "Maßnahme wurde geändert.", "Measure", id, title, tenantId, changes);

    public Task LogMeasureArchivedAsync(int id, string title, int tenantId) =>
        LogAsync("MeasureArchived", "Maßnahme wurde archiviert.", "Measure", id, title, tenantId);

    public Task LogMeasureRestoredAsync(int id, string title, int tenantId) =>
        LogAsync("MeasureRestored", "Maßnahme wurde wiederhergestellt.", "Measure", id, title, tenantId);

    public Task LogMeasureStatusChangedAsync(int id, string title, int tenantId, object oldStatus, object newStatus) =>
        LogStatusChangeAsync("MeasureStatusChanged", "Status der Maßnahme wurde geändert.", "Measure", id, title, tenantId, oldStatus, newStatus);

    public Task LogMeasureCompletedAsync(int id, string title, int tenantId) =>
        LogAsync("MeasureCompleted", "Maßnahme wurde abgeschlossen.", "Measure", id, title, tenantId);

    public Task LogAuditCreatedAsync(int id, string title, int tenantId) =>
        LogAsync("AuditCreated", "Audit wurde erstellt.", "Audit", id, title, tenantId);

    public Task LogAuditStartedAsync(int id, string title, int tenantId) =>
        LogAsync("AuditStarted", "Audit wurde gestartet.", "Audit", id, title, tenantId);

    public Task LogAuditUpdatedAsync(int id, string title, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("AuditUpdated", "Audit wurde geändert.", "Audit", id, title, tenantId, changes);

    public Task LogAuditCompletedAsync(int id, string title, int tenantId) =>
        LogAsync("AuditCompleted", "Audit wurde abgeschlossen.", "Audit", id, title, tenantId);

    public Task LogAuditArchivedAsync(int id, string title, int tenantId) =>
        LogAsync("AuditArchived", "Audit wurde archiviert.", "Audit", id, title, tenantId);

    public Task LogAuditAnswerUpdatedAsync(int auditRunId, string auditTitle, int answerId, int tenantId) =>
        LogAsync("AuditAnswerUpdated", "Audit-Antwort wurde geändert.", "Audit", auditRunId, auditTitle, tenantId,
            metadata: new { AnswerId = answerId });

    public Task LogAuditTemplateCreatedAsync(int id, string title, int? tenantId) =>
        LogAsync("AuditTemplateCreated", "Auditvorlage wurde erstellt.", "AuditTemplate", id, title, tenantId);

    public Task LogAuditTemplateUpdatedAsync(int id, string title, int? tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("AuditTemplateUpdated", "Auditvorlage wurde geändert.", "AuditTemplate", id, title, tenantId, changes);

    public Task LogAuditTemplateArchivedAsync(int id, string title, int? tenantId) =>
        LogAsync("AuditTemplateArchived", "Auditvorlage wurde archiviert.", "AuditTemplate", id, title, tenantId);

    public Task LogAuditTemplateRestoredAsync(int id, string title, int? tenantId) =>
        LogAsync("AuditTemplateRestored", "Auditvorlage wurde wiederhergestellt.", "AuditTemplate", id, title, tenantId);

    public Task LogAuditTemplatePublishedToCommunityAsync(int id, string title, int tenantId) =>
        LogAsync("AuditTemplatePublishedToCommunity", "Auditvorlage wurde zur Community eingereicht.",
            "AuditTemplate", id, title, tenantId);

    public Task LogAuditTemplateImportedAsync(int id, string title, int tenantId, int sourceTemplateId) =>
        LogAsync("AuditTemplateImported", "Auditvorlage wurde importiert.", "AuditTemplate", id, title, tenantId,
            metadata: new { SourceTemplateId = sourceTemplateId });

    public Task LogGlobalAuditTemplateQuestionCreatedAsync(int templateId, string templateTitle, int questionId) =>
        LogAsync("GlobalAuditTemplateQuestionCreated", "Frage einer globalen Auditvorlage wurde erstellt.",
            "AuditTemplate", templateId, templateTitle, null,
            metadata: new { QuestionId = questionId, Result = "Success" });

    public Task LogGlobalAuditTemplateQuestionUpdatedAsync(int templateId, string templateTitle, int questionId) =>
        LogAsync("GlobalAuditTemplateQuestionUpdated", "Frage einer globalen Auditvorlage wurde geändert.",
            "AuditTemplate", templateId, templateTitle, null,
            metadata: new { QuestionId = questionId, Result = "Success" });

    public Task LogGlobalAuditTemplateQuestionDeletedAsync(int templateId, string templateTitle, int questionId) =>
        LogAsync("GlobalAuditTemplateQuestionDeleted", "Frage einer globalen Auditvorlage wurde gelöscht.",
            "AuditTemplate", templateId, templateTitle, null,
            metadata: new { QuestionId = questionId, Result = "Success" });

    public Task LogTenantAuditTemplateQuestionAccessDeniedAsync(int templateId, int? questionId) =>
        LogAsync("TenantAuditTemplateQuestionAccessDenied",
            "Zugriff auf mandantenspezifische Auditvorlagen-Frage verweigert.",
            "AuditTemplate", templateId, templateId.ToString(), null,
            metadata: new { QuestionId = questionId, Result = "Denied" });

    public Task LogEvidenceDocumentUploadedAsync(int id, string fileName, int tenantId) =>
        LogAsync("EvidenceDocumentUploaded", "Nachweisdokument wurde hochgeladen.", "EvidenceDocument", id, fileName, tenantId);

    public Task LogEvidenceDocumentUpdatedAsync(int id, string fileName, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("EvidenceDocumentUpdated", "Dokumentmetadaten wurden geändert.", "EvidenceDocument", id, fileName, tenantId, changes);

    public Task LogEvidenceDocumentArchivedAsync(int id, string fileName, int tenantId) =>
        LogAsync("EvidenceDocumentArchived", "Nachweisdokument wurde archiviert.", "EvidenceDocument", id, fileName, tenantId);

    public Task LogEvidenceDocumentRestoredAsync(int id, string fileName, int tenantId) =>
        LogAsync("EvidenceDocumentRestored", "Nachweisdokument wurde wiederhergestellt.", "EvidenceDocument", id, fileName, tenantId);

    public Task LogDocumentCategoryCreatedAsync(int id, string name, int tenantId) =>
        LogAsync("DocumentCategoryCreated", "Dokumentkategorie wurde erstellt.", "DocumentCategory", id, name, tenantId);

    public Task LogDocumentCategoryUpdatedAsync(int id, string name, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("DocumentCategoryUpdated", "Dokumentkategorie wurde geändert.", "DocumentCategory", id, name, tenantId, changes);

    public Task LogDocumentCategoryDeactivatedAsync(int id, string name, int tenantId) =>
        LogAsync("DocumentCategoryDeactivated", "Dokumentkategorie wurde deaktiviert.", "DocumentCategory", id, name, tenantId);

    public Task LogDocumentCategoryReactivatedAsync(int id, string name, int tenantId) =>
        LogAsync("DocumentCategoryReactivated", "Dokumentkategorie wurde reaktiviert.", "DocumentCategory", id, name, tenantId);

    public Task LogDataProtectionRoleCreatedAsync(int id, string roleTitle, int tenantId) =>
        LogAsync("DataProtectionRoleCreated", "Datenschutzrolle wurde erstellt.", "DataProtectionRole", id, roleTitle, tenantId);

    public Task LogDataProtectionRoleUpdatedAsync(int id, string roleTitle, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("DataProtectionRoleUpdated", "Datenschutzrolle wurde geändert.", "DataProtectionRole", id, roleTitle, tenantId, changes);

    public Task LogDataProtectionRoleDeactivatedAsync(int id, string roleTitle, int tenantId) =>
        LogAsync("DataProtectionRoleDeactivated", "Datenschutzrolle wurde deaktiviert.", "DataProtectionRole", id, roleTitle, tenantId);

    public Task LogDataProtectionRoleReactivatedAsync(int id, string roleTitle, int tenantId) =>
        LogAsync("DataProtectionRoleReactivated", "Datenschutzrolle wurde reaktiviert.", "DataProtectionRole", id, roleTitle, tenantId);

    public Task LogPrivacyIncidentCreatedAsync(int id, string title, int tenantId) =>
        LogAsync("PrivacyIncidentCreated", "Datenschutzvorfall wurde erstellt.", "PrivacyIncident", id, title, tenantId);

    public Task LogPrivacyIncidentUpdatedAsync(int id, string title, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("PrivacyIncidentUpdated", "Datenschutzvorfall wurde geändert.", "PrivacyIncident", id, title, tenantId, changes);

    public Task LogPrivacyIncidentArchivedAsync(int id, string title, int tenantId) =>
        LogAsync("PrivacyIncidentArchived", "Datenschutzvorfall wurde archiviert.", "PrivacyIncident", id, title, tenantId);

    public Task LogPrivacyIncidentRestoredAsync(int id, string title, int tenantId) =>
        LogAsync("PrivacyIncidentRestored", "Datenschutzvorfall wurde wiederhergestellt.", "PrivacyIncident", id, title, tenantId);

    public Task LogPrivacyIncidentStatusChangedAsync(int id, string title, int tenantId, object oldStatus, object newStatus) =>
        LogStatusChangeAsync("PrivacyIncidentStatusChanged", "Status des Datenschutzvorfalls wurde geändert.",
            "PrivacyIncident", id, title, tenantId, oldStatus, newStatus);

    public Task LogDataSubjectRequestCreatedAsync(int id, string displayName, int tenantId) =>
        LogAsync("DataSubjectRequestCreated", "Betroffenenanfrage wurde erstellt.",
            "DataSubjectRequest", id, displayName, tenantId);

    public Task LogDataSubjectRequestUpdatedAsync(int id, string displayName, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("DataSubjectRequestUpdated", "Betroffenenanfrage wurde geändert.",
            "DataSubjectRequest", id, displayName, tenantId, changes);

    public Task LogDataSubjectRequestArchivedAsync(int id, string displayName, int tenantId) =>
        LogAsync("DataSubjectRequestArchived", "Betroffenenanfrage wurde archiviert.",
            "DataSubjectRequest", id, displayName, tenantId);

    public Task LogDataSubjectRequestRestoredAsync(int id, string displayName, int tenantId) =>
        LogAsync("DataSubjectRequestRestored", "Betroffenenanfrage wurde wiederhergestellt.",
            "DataSubjectRequest", id, displayName, tenantId);

    public Task LogDataSubjectRequestStatusChangedAsync(int id, string displayName, int tenantId, object oldStatus, object newStatus) =>
        LogStatusChangeAsync("DataSubjectRequestStatusChanged", "Status der Betroffenenanfrage wurde geändert.",
            "DataSubjectRequest", id, displayName, tenantId, oldStatus, newStatus);

    public Task LogDataSubjectRequestAnonymizedAsync(int id, string displayName, int tenantId, string? note) =>
        LogAsync(
            "DataSubjectRequestAnonymized",
            "Falldaten der Betroffenenanfrage wurden endgültig anonymisiert.",
            "DataSubjectRequest",
            id,
            displayName,
            tenantId,
            metadata: string.IsNullOrWhiteSpace(note) ? null : new { AnonymizationNoteProvided = true });

    public Task LogTrainingTemplateCreatedAsync(int id, string title, int? tenantId) =>
        LogAsync("TrainingTemplateCreated", "Schulungsvorlage wurde erstellt.", "TrainingTemplate", id, title, tenantId);

    public Task LogTrainingTemplateUpdatedAsync(int id, string title, int? tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("TrainingTemplateUpdated", "Schulungsvorlage wurde geändert.", "TrainingTemplate", id, title, tenantId, changes);

    public Task LogTrainingTemplateArchivedAsync(int id, string title, int? tenantId) =>
        LogAsync("TrainingTemplateArchived", "Schulungsvorlage wurde archiviert.", "TrainingTemplate", id, title, tenantId);

    public Task LogTrainingTemplateCopiedAsync(int id, string title, int tenantId, int sourceTemplateId) =>
        LogAsync("TrainingTemplateCopied", "Schulungsvorlage wurde kopiert.", "TrainingTemplate", id, title, tenantId,
            metadata: new { SourceTemplateId = sourceTemplateId });

    public Task LogTrainingTemplateSectionChangedAsync(int templateId, string templateTitle, int? tenantId, int sectionId, string sectionTitle) =>
        LogAsync("TrainingTemplateSectionChanged", "Schulungskarte wurde geändert.", "TrainingTemplate", templateId, templateTitle, tenantId,
            metadata: new { SectionId = sectionId, SectionTitle = sectionTitle });

    public Task LogTrainingTemplateAssetUploadedAsync(int templateId, string templateTitle, int? tenantId, int assetId, string assetKey) =>
        LogAsync("TrainingTemplateAssetUploaded", "Schulungsasset wurde hochgeladen.", "TrainingTemplate", templateId, templateTitle, tenantId,
            metadata: new { AssetId = assetId, AssetKey = assetKey });

    public Task LogTrainingTemplateAssetArchivedAsync(int templateId, string templateTitle, int? tenantId, int assetId, string assetKey) =>
        LogAsync("TrainingTemplateAssetArchived", "Schulungsasset wurde archiviert.", "TrainingTemplate", templateId, templateTitle, tenantId,
            metadata: new { AssetId = assetId, AssetKey = assetKey });

    public Task LogTrainingQuestionChangedAsync(int templateId, string templateTitle, int? tenantId, int questionId) =>
        LogAsync("TrainingQuestionChanged", "Quizfrage wurde geändert.", "TrainingTemplate", templateId, templateTitle, tenantId,
            metadata: new { QuestionId = questionId });

    public Task LogTrainingTemplateSubmittedToCommunityAsync(int id, string title, int tenantId) =>
        LogAsync("TrainingTemplateSubmittedToCommunity", "Schulungsvorlage wurde als Community-Vorlage eingereicht.",
            "TrainingTemplate", id, title, tenantId);

    public Task LogTrainingTemplateCommunityApprovedAsync(int globalTemplateId, string title, int sourceTemplateId, int? sourceTenantId, int questionCountCopied) =>
        LogAsync("TrainingTemplateCommunityApproved", "Community-Schulungsvorlage wurde freigegeben.",
            "TrainingTemplate", globalTemplateId, title, null,
            metadata: new
            {
                SourceTemplateId = sourceTemplateId,
                SourceTenantId = sourceTenantId,
                QuestionCountCopied = questionCountCopied,
                Result = "Success"
            });

    public Task LogTrainingTemplateCommunityRejectedAsync(int id, string title, int tenantId) =>
        LogAsync("TrainingTemplateCommunityRejected", "Community-Schulungsvorlage wurde abgelehnt.",
            "TrainingTemplate", id, title, tenantId);

    public Task LogTrainingTemplateCommunityReviewOpenedAsync(int id, string title) =>
        LogAsync("TrainingTemplateCommunityReviewOpened", "Community-Schulungsvorlage zur Prüfung geöffnet.",
            "TrainingTemplate", id, title, null);

    public Task LogTrainingTemplateCommunityQuizReviewOpenedAsync(int templateId, string title, int? tenantId, int questionCount) =>
        LogAsync("TrainingTemplateCommunityQuizReviewOpened", "Quiz einer Community-Schulungsvorlage zur Prüfung geöffnet.",
            "TrainingTemplate", templateId, title, tenantId,
            metadata: new { QuestionCount = questionCount, Result = "Success" });

    public Task LogTrainingCreatedAsync(int id, string title, int tenantId) =>
        LogAsync("TrainingCreated", "Schulung wurde erstellt.", "Training", id, title, tenantId);

    public Task LogTrainingCreatedFromTemplateAsync(int id, string title, int tenantId, int templateId, string templateTitle) =>
        LogAsync("TrainingCreatedFromTemplate", "Schulung wurde aus Vorlage erstellt.", "Training", id, title, tenantId,
            metadata: new { TemplateId = templateId, TemplateTitle = templateTitle });

    public Task LogTrainingUpdatedAsync(int id, string title, int tenantId, IReadOnlyList<AuditFieldChangeDto> changes) =>
        LogUpdateAsync("TrainingUpdated", "Schulung wurde geändert.", "Training", id, title, tenantId, changes);

    public Task LogTrainingArchivedAsync(int id, string title, int tenantId) =>
        LogAsync("TrainingArchived", "Schulung wurde archiviert.", "Training", id, title, tenantId);

    public Task LogTrainingRestoredAsync(int id, string title, int tenantId) =>
        LogAsync("TrainingRestored", "Schulung wurde reaktiviert.", "Training", id, title, tenantId);

    public Task LogTrainingStatusChangedAsync(int id, string title, int tenantId, object oldStatus, object newStatus) =>
        LogAsync("TrainingStatusChanged", "Schulungsstatus wurde geändert.", "Training", id, title, tenantId,
            metadata: new { OldStatus = oldStatus.ToString(), NewStatus = newStatus.ToString() });

    public Task LogTrainingProofLinkedAsync(int trainingId, string trainingTitle, int tenantId, int documentId, string fileName) =>
        LogAsync("TrainingProofLinked", "Nachweis wurde verknüpft.", "Training", trainingId, trainingTitle, tenantId,
            metadata: new { DocumentId = documentId, FileName = fileName });

    public Task LogTrainingProofRemovedAsync(int trainingId, string trainingTitle, int tenantId, int documentId, string fileName) =>
        LogAsync("TrainingProofRemoved", "Nachweis-Verknüpfung wurde entfernt.", "Training", trainingId, trainingTitle, tenantId,
            metadata: new { DocumentId = documentId, FileName = fileName });

    public Task LogTrainingParticipantCreatedAsync(int participantId, int tenantId) =>
        LogAsync("TrainingParticipantCreated", "Schulungsteilnehmer wurde erstellt.", "TrainingParticipant", participantId, participantId.ToString(), tenantId);

    public Task LogTrainingParticipantUpdatedAsync(int participantId, int tenantId) =>
        LogAsync("TrainingParticipantUpdated", "Schulungsteilnehmer wurde geändert.", "TrainingParticipant", participantId, participantId.ToString(), tenantId);

    public Task LogTrainingParticipantArchivedAsync(int participantId, int tenantId) =>
        LogAsync("TrainingParticipantArchived", "Schulungsteilnehmer wurde archiviert.", "TrainingParticipant", participantId, participantId.ToString(), tenantId);

    public Task LogTrainingParticipantDeactivatedAsync(int participantId, int tenantId) =>
        LogAsync("TrainingParticipantDeactivated", "Schulungsteilnehmer wurde deaktiviert.", "TrainingParticipant", participantId, participantId.ToString(), tenantId);

    public Task LogTrainingParticipantReactivatedAsync(int participantId, int tenantId) =>
        LogAsync("TrainingParticipantReactivated", "Schulungsteilnehmer wurde reaktiviert.", "TrainingParticipant", participantId, participantId.ToString(), tenantId);

    public Task LogTrainingParticipantAssignedAsync(int trainingId, string trainingTitle, int tenantId, int assignmentId, int? participantId) =>
        LogAsync("TrainingParticipantAssigned", "Teilnehmer wurde einer Schulung zugewiesen.", "Training", trainingId, trainingTitle, tenantId,
            metadata: new { AssignmentId = assignmentId, ParticipantId = participantId });

    public Task LogTrainingAssignmentCancelledAsync(int trainingId, string trainingTitle, int tenantId, int assignmentId, int? participantId) =>
        LogAsync("TrainingAssignmentCancelled", "Schulungszuweisung wurde abgebrochen.", "Training", trainingId, trainingTitle, tenantId,
            metadata: new { AssignmentId = assignmentId, ParticipantId = participantId });

    public Task LogTrainingInvitationSentAsync(int trainingId, string trainingTitle, int tenantId, int assignmentId, int? participantId) =>
        LogAsync("TrainingInvitationSent", "Schulungseinladung wurde gesendet.", "Training", trainingId, trainingTitle, tenantId,
            metadata: new { AssignmentId = assignmentId, ParticipantId = participantId });

    public Task LogTrainingInvitationResentAsync(int trainingId, string trainingTitle, int tenantId, int assignmentId, int? participantId) =>
        LogAsync("TrainingInvitationResent", "Schulungseinladung wurde erneut gesendet.", "Training", trainingId, trainingTitle, tenantId,
            metadata: new { AssignmentId = assignmentId, ParticipantId = participantId });

    private Task LogUpdateAsync(
        string action,
        string description,
        string entityType,
        int id,
        string name,
        int? tenantId,
        IReadOnlyList<AuditFieldChangeDto> changes)
    {
        var payload = AuditDiffHelper.BuildPayload(changes);
        if (payload is null)
        {
            return Task.CompletedTask;
        }

        return logService.LogAuditAsync(
            action: action,
            description: description,
            entityType: entityType,
            entityId: id.ToString(),
            entityName: name,
            tenantId: tenantId,
            oldValues: payload.OldValues,
            newValues: payload.NewValues,
            metadata: payload.Metadata,
            isVisibleToAdmin: true);
    }

    private Task LogStatusChangeAsync(
        string action,
        string description,
        string entityType,
        int id,
        string name,
        int? tenantId,
        object oldStatus,
        object newStatus)
    {
        var changes = new List<AuditFieldChangeDto>();
        AuditDiffHelper.AddIfChanged(changes, "Status", "Status", oldStatus, newStatus);
        return LogUpdateAsync(action, description, entityType, id, name, tenantId, changes);
    }

    private Task LogAsync(
        string action,
        string description,
        string entityType,
        int id,
        string name,
        int? tenantId,
        object? oldValues = null,
        object? newValues = null,
        object? metadata = null) =>
        logService.LogAuditAsync(
            action: action,
            description: description,
            entityType: entityType,
            entityId: id.ToString(),
            entityName: name,
            tenantId: tenantId,
            oldValues: oldValues,
            newValues: newValues,
            metadata: metadata,
            isVisibleToAdmin: true);
}
