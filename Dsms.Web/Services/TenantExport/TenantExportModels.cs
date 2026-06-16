namespace Dsms.Web.Services.TenantExport;

public sealed class ExportInfoDto
{
    public DateTime ExportCreatedAt { get; init; }
    public string? ExportCreatedByUserId { get; init; }
    public string? ExportCreatedByEmail { get; init; }
    public int TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string ApplicationName { get; init; } = "Datenschutz-Cloud";
    public string ExportVersion { get; init; } = "1.0";
    public string SecurityNote { get; init; } =
        "Dieser Export enthält keine Passwort-Hashes, Tokens, Secrets oder SMTP-Passwörter.";
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class TenantExportDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? LegalName { get; init; }
    public string? Street { get; init; }
    public string? HouseNumber { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? DpoName { get; init; }
    public string? DpoStreet { get; init; }
    public string? DpoHouseNumber { get; init; }
    public string? DpoPostalCode { get; init; }
    public string? DpoCity { get; init; }
    public string? DpoPhone { get; init; }
    public string? DpoEmail { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeletionRequested { get; init; }
    public DateTime? DeletionRequestedAt { get; init; }
    public DateTime? DeletionScheduledAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class UserExportDto
{
    public string Id { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? UserName { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = [];
    public bool IsActive { get; init; }
    public IReadOnlyList<int> AssignedTenantIds { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}

public sealed class ProcessingActivityExportDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Purpose { get; init; }
    public string? ResponsibleDepartment { get; init; }
    public string? LegalBasis { get; init; }
    public string? DataSubjectCategories { get; init; }
    public string? PersonalDataCategories { get; init; }
    public string? Recipients { get; init; }
    public bool ThirdCountryTransfer { get; init; }
    public string? ThirdCountryTransferDescription { get; init; }
    public string? RetentionPeriod { get; init; }
    public bool DpiaRequired { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Owner { get; init; }
    public bool IsArchived { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<int> LinkedTomIds { get; init; } = [];
    public IReadOnlyList<ProcessingActivityServiceProviderLinkExportDto> LinkedServiceProviders { get; init; } = [];
    public IReadOnlyList<int> LinkedMeasureIds { get; init; } = [];
    public IReadOnlyList<int> LinkedAuditAnswerIds { get; init; } = [];
}

public sealed class ProcessingActivityServiceProviderLinkExportDto
{
    public int ServiceProviderId { get; init; }
    public string RoleInProcessing { get; init; } = string.Empty;
}

public sealed class DsfaExportDto
{
    public int Id { get; init; }
    public int ProcessingActivityId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? ProcessingDescription { get; init; }
    public string? ReasonForDpia { get; init; }
    public string? NecessityAndProportionality { get; init; }
    public string? RiskAssessment { get; init; }
    public string? ProtectiveMeasures { get; init; }
    public string ResidualRisk { get; init; } = string.Empty;
    public string Outcome { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ResponsiblePerson { get; init; }
    public string? ReviewedBy { get; init; }
    public string? ReviewedAt { get; init; }
    public string? NextReviewAt { get; init; }
    public bool IsArchived { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class TomExportDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Category { get; init; } = string.Empty;
    public string ProtectionGoal { get; init; } = string.Empty;
    public string ImplementationStatus { get; init; } = string.Empty;
    public string? Owner { get; init; }
    public string? ValidFrom { get; init; }
    public string? NextReviewAt { get; init; }
    public string? EvidenceReference { get; init; }
    public string? Notes { get; init; }
    public bool IsArchived { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<int> LinkedProcessingActivityIds { get; init; } = [];
    public IReadOnlyList<int> LinkedServiceProviderIds { get; init; } = [];
}

public sealed class ProcessorExportDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ProviderType { get; init; } = string.Empty;
    public string? ServicePurpose { get; init; }
    public string? ContactPerson { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public string? Address { get; init; }
    public string? Country { get; init; }
    public bool IsDataProcessor { get; init; }
    public bool DataProcessingAgreementExists { get; init; }
    public string? DataProcessingAgreementDate { get; init; }
    public string? DataProcessingAgreementReviewedAt { get; init; }
    public string? DataProcessingAgreementReviewResult { get; init; }
    public bool TomsReviewed { get; init; }
    public string? TomsReviewedAt { get; init; }
    public string? TomsReviewResult { get; init; }
    public bool SubProcessorsAllowed { get; init; }
    public string? SubProcessorsDescription { get; init; }
    public bool ThirdCountryInvolvement { get; init; }
    public string? ThirdCountry { get; init; }
    public string ThirdCountryLegalBasis { get; init; } = string.Empty;
    public string? ThirdCountryTransferGuarantees { get; init; }
    public string RiskAssessment { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ResponsiblePerson { get; init; }
    public string? Notes { get; init; }
    public bool IsArchived { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<int> LinkedProcessingActivityIds { get; init; } = [];
    public IReadOnlyList<int> LinkedTomIds { get; init; } = [];
}

public sealed class MeasureExportDto
{
    public int Id { get; init; }
    public int? AuditRunId { get; init; }
    public int? AuditAnswerId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? DueDate { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? AssignedUserId { get; init; }
    public bool IsArchived { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<int> LinkedProcessingActivityIds { get; init; } = [];
}

public sealed class TrainingExportDto
{
    public int Id { get; init; }
    public int? TrainingTemplateId { get; init; }
    public string? TemplateTitle { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string TrainingType { get; init; } = string.Empty;
    public string? TargetAudience { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? ScheduledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? RepeatDueAt { get; init; }
    public string? ResponsibleUserId { get; init; }
    public string? ResponsibleName { get; init; }
    public int ParticipantCount { get; init; }
    public bool ProofMissing { get; init; }
    public string? Notes { get; init; }
    public bool IsArchived { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class AuditQuestionExportDto
{
    public int Id { get; init; }
    public int SortOrder { get; init; }
    public string Text { get; init; } = string.Empty;
    public string? Category { get; init; }
    public bool IsRequired { get; init; }
}

public sealed class AuditTemplateExportDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Version { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsArchived { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<AuditQuestionExportDto> Questions { get; init; } = [];
}

public sealed class AuditAnswerExportDto
{
    public int Id { get; init; }
    public int AuditQuestionId { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string? QuestionCategory { get; init; }
    public int QuestionSortOrder { get; init; }
    public string? AnswerText { get; init; }
    public string ComplianceLevel { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime? AnsweredAt { get; init; }
    public IReadOnlyList<int> LinkedProcessingActivityIds { get; init; } = [];
    public IReadOnlyList<int> LinkedMeasureIds { get; init; } = [];
}

public sealed class AuditRunExportDto
{
    public int Id { get; init; }
    public int AuditTemplateId { get; init; }
    public string? TemplateTitle { get; init; }
    public string? TemplateVersion { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? AssignedUserId { get; init; }
    public bool IsArchived { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<AuditAnswerExportDto> Answers { get; init; } = [];
}

public sealed class PrivacyIncidentExportDto
{
    public int Id { get; init; }
    public string IncidentNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string OwnRole { get; init; } = string.Empty;
    public DateTime? DiscoveredAt { get; init; }
    public DateTime? OccurredAt { get; init; }
    public DateTime? ReportedToUsAt { get; init; }
    public string? ResponsiblePerson { get; init; }
    public string? InternalReference { get; init; }
    public string? Description { get; init; }
    public string? HowDetected { get; init; }
    public string? Cause { get; init; }
    public string? AffectedSystems { get; init; }
    public bool? IncidentStillActive { get; init; }
    public DateTime? IncidentStoppedAt { get; init; }
    public bool ConfidentialityAffected { get; init; }
    public bool IntegrityAffected { get; init; }
    public bool AvailabilityAffected { get; init; }
    public string? BreachTypeDescription { get; init; }
    public string? AffectedDataCategories { get; init; }
    public string? AffectedPersonGroups { get; init; }
    public int? ApproxAffectedPersons { get; init; }
    public int? ApproxAffectedRecords { get; init; }
    public bool SpecialCategoriesAffected { get; init; }
    public string? LikelyConsequences { get; init; }
    public string RiskLevel { get; init; } = string.Empty;
    public string? RiskAssessmentReason { get; init; }
    public string SupervisoryAuthorityNotificationRequired { get; init; } = string.Empty;
    public string? SupervisoryAuthorityNotificationReason { get; init; }
    public string? SupervisoryAuthorityName { get; init; }
    public DateTime? SupervisoryAuthorityNotifiedAt { get; init; }
    public string? SupervisoryAuthorityReference { get; init; }
    public string? NotificationDelayReason { get; init; }
    public string DataSubjectsNotificationRequired { get; init; } = string.Empty;
    public string? DataSubjectsNotificationReason { get; init; }
    public DateTime? DataSubjectsNotifiedAt { get; init; }
    public string? DataSubjectsNotificationMethod { get; init; }
    public string? DataSubjectsNotificationSummary { get; init; }
    public string? ImmediateActions { get; init; }
    public string? RemediationActions { get; init; }
    public string? PreventiveActions { get; init; }
    public string? ClosureSummary { get; init; }
    public DateTime? ClosedAt { get; init; }
    public string? CreatedByUserId { get; init; }
    public string? UpdatedByUserId { get; init; }
    public bool IsArchived { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<int> LinkedProcessingActivityIds { get; init; } = [];
    public IReadOnlyList<int> LinkedServiceProviderIds { get; init; } = [];
    public IReadOnlyList<int> LinkedMeasureIds { get; init; } = [];
    public IReadOnlyList<int> LinkedTomIds { get; init; } = [];
}

public sealed class DocumentLinkExportDto
{
    public string EntityType { get; init; } = string.Empty;
    public int EntityId { get; init; }
}

public sealed class DocumentMetadataExportDto
{
    public int DocumentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string? ModuleReference { get; init; }
    public int? EntityId { get; init; }
    public IReadOnlyList<DocumentLinkExportDto> LinkedEntities { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public string? CreatedByUserId { get; init; }
    public string RelativePathInZip { get; init; } = string.Empty;
    public bool FileIncluded { get; init; }
    public string? FileWarning { get; init; }
}

public sealed class TenantExportResult
{
    public required string FileName { get; init; }
    public string ContentType { get; init; } = "application/zip";
    public required byte[] Content { get; init; }
}

/// <summary>Minimale Vorlagenreferenz für Audit-Export (ohne archivierbare Bool-Felder).</summary>
internal sealed class AuditTemplateRef
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
}

/// <summary>Minimale Fragenreferenz für Audit-Export.</summary>
internal sealed class AuditQuestionRef
{
    public int Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public string? Category { get; init; }
    public int SortOrder { get; init; }
}
