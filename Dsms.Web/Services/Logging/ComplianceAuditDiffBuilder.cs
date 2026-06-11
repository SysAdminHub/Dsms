using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Services.Logging;

/// <summary>Baut Feld-Diffs für Compliance-Update-Auditlogs.</summary>
public static class ComplianceAuditDiffBuilder
{
    public static List<AuditFieldChangeDto> ForProcessingActivity(
        string? previousName,
        ProcessingActivityStatus? previousStatus,
        string? previousOwner,
        string? previousDepartment,
        ProcessingActivity current)
    {
        var changes = new List<AuditFieldChangeDto>();
        AuditDiffHelper.AddIfChanged(changes, "Name", "Name", previousName, current.Name);
        AuditDiffHelper.AddIfChanged(changes, "Status", "Status", previousStatus, current.Status,
            v => v is ProcessingActivityStatus s ? ProcessingActivityLabels.GetStatusLabel(s) : AuditDiffHelper.FormatAuditValue(v));
        AuditDiffHelper.AddIfChanged(changes, "Owner", "Verantwortlicher", previousOwner, current.Owner);
        AuditDiffHelper.AddIfChanged(changes, "ResponsibleDepartment", "Verantwortlicher Bereich",
            previousDepartment, current.ResponsibleDepartment);
        return changes;
    }

    public static List<AuditFieldChangeDto> ForDpia(
        string? previousTitle,
        DpiaStatus? previousStatus,
        DpiaResidualRisk? previousResidualRisk,
        string? previousResponsible,
        DateOnly? previousReviewedAt,
        DateOnly? previousNextReviewAt,
        DataProtectionImpactAssessment current)
    {
        var changes = new List<AuditFieldChangeDto>();
        AuditDiffHelper.AddIfChanged(changes, "Title", "Titel", previousTitle, current.Title);
        AuditDiffHelper.AddIfChanged(changes, "Status", "Status", previousStatus, current.Status,
            v => v is DpiaStatus s ? DsfaLabels.GetStatusLabel(s) : AuditDiffHelper.FormatAuditValue(v));
        AuditDiffHelper.AddIfChanged(changes, "ResidualRisk", "Restrisiko", previousResidualRisk, current.ResidualRisk,
            v => v is DpiaResidualRisk r ? DsfaLabels.GetResidualRiskLabel(r) : AuditDiffHelper.FormatAuditValue(v));
        AuditDiffHelper.AddIfChanged(changes, "ResponsiblePerson", "Verantwortlicher",
            previousResponsible, current.ResponsiblePerson);
        AuditDiffHelper.AddIfChanged(changes, "ReviewedAt", "Geprüft am", previousReviewedAt, current.ReviewedAt);
        AuditDiffHelper.AddIfChanged(changes, "NextReviewAt", "Nächste Prüfung", previousNextReviewAt, current.NextReviewAt);
        return changes;
    }

    public static List<AuditFieldChangeDto> ForTom(
        string? previousTitle,
        TomCategory? previousCategory,
        TomImplementationStatus? previousStatus,
        string? previousOwner,
        DateOnly? previousNextReviewAt,
        Tom current)
    {
        var changes = new List<AuditFieldChangeDto>();
        AuditDiffHelper.AddIfChanged(changes, "Title", "Titel", previousTitle, current.Title);
        AuditDiffHelper.AddIfChanged(changes, "Category", "Kategorie", previousCategory, current.Category,
            v => v is TomCategory c ? TomLabels.GetCategoryLabel(c) : AuditDiffHelper.FormatAuditValue(v));
        AuditDiffHelper.AddIfChanged(changes, "ImplementationStatus", "Umsetzungsstatus",
            previousStatus, current.ImplementationStatus,
            v => v is TomImplementationStatus s ? TomLabels.GetImplementationStatusLabel(s) : AuditDiffHelper.FormatAuditValue(v));
        AuditDiffHelper.AddIfChanged(changes, "Owner", "Verantwortlicher", previousOwner, current.Owner);
        AuditDiffHelper.AddIfChanged(changes, "NextReviewAt", "Nächste Prüfung", previousNextReviewAt, current.NextReviewAt);
        return changes;
    }

    public static List<AuditFieldChangeDto> ForProcessor(
        string? previousName,
        ServiceProviderStatus? previousStatus,
        string? previousResponsible,
        Dsms.Web.Domain.Entities.ServiceProvider current)
    {
        var changes = new List<AuditFieldChangeDto>();
        AuditDiffHelper.AddIfChanged(changes, "Name", "Name", previousName, current.Name);
        AuditDiffHelper.AddIfChanged(changes, "Status", "Status", previousStatus, current.Status,
            v => v is ServiceProviderStatus s ? ServiceProviderLabels.GetStatusLabel(s) : AuditDiffHelper.FormatAuditValue(v));
        AuditDiffHelper.AddIfChanged(changes, "ResponsiblePerson", "Verantwortlicher",
            previousResponsible, current.ResponsiblePerson);
        return changes;
    }

    public static List<AuditFieldChangeDto> ForPrivacyIncident(
        string? previousTitle,
        PrivacyIncidentStatus? previousStatus,
        PrivacyIncidentSeverity? previousSeverity,
        PrivacyIncidentRiskLevel? previousRiskLevel,
        PrivacyIncident current)
    {
        var changes = new List<AuditFieldChangeDto>();
        AuditDiffHelper.AddIfChanged(changes, "Title", "Titel", previousTitle, current.Title);
        AuditDiffHelper.AddIfChanged(changes, "Status", "Status", previousStatus, current.Status,
            v => v is PrivacyIncidentStatus s ? PrivacyIncidentLabels.GetStatusLabel(s) : AuditDiffHelper.FormatAuditValue(v));
        AuditDiffHelper.AddIfChanged(changes, "Severity", "Schweregrad", previousSeverity, current.Severity,
            v => v is PrivacyIncidentSeverity s ? PrivacyIncidentLabels.GetSeverityLabel(s) : AuditDiffHelper.FormatAuditValue(v));
        AuditDiffHelper.AddIfChanged(changes, "RiskLevel", "Risiko", previousRiskLevel, current.RiskLevel,
            v => v is PrivacyIncidentRiskLevel r ? PrivacyIncidentLabels.GetRiskLevelLabel(r) : AuditDiffHelper.FormatAuditValue(v));
        return changes;
    }

    public static List<AuditFieldChangeDto> ForMeasure(
        string? previousTitle,
        MeasureStatus? previousStatus,
        DateOnly? previousDueDate,
        Measure current)
    {
        var changes = new List<AuditFieldChangeDto>();
        AuditDiffHelper.AddIfChanged(changes, "Title", "Titel", previousTitle, current.Title);
        AuditDiffHelper.AddIfChanged(changes, "Status", "Status", previousStatus, current.Status,
            v => v is MeasureStatus s ? MeasureLabels.GetStatusLabel(s) : AuditDiffHelper.FormatAuditValue(v));
        AuditDiffHelper.AddIfChanged(changes, "DueDate", "Fälligkeitsdatum", previousDueDate, current.DueDate);
        return changes;
    }

    public static List<AuditFieldChangeDto> ForAuditRun(
        string? previousTitle,
        AuditRunStatus? previousStatus,
        AuditRun current)
    {
        var changes = new List<AuditFieldChangeDto>();
        AuditDiffHelper.AddIfChanged(changes, "Title", "Titel", previousTitle, current.Title);
        AuditDiffHelper.AddIfChanged(changes, "Status", "Status", previousStatus, current.Status,
            v => v is AuditRunStatus s ? GetAuditRunStatusLabel(s) : AuditDiffHelper.FormatAuditValue(v));
        return changes;
    }

    public static List<AuditFieldChangeDto> ForAuditTemplate(
        string? previousTitle,
        string? previousVersion,
        bool? previousIsActive,
        AuditTemplateType? previousType,
        AuditTemplate current)
    {
        var changes = new List<AuditFieldChangeDto>();
        AuditDiffHelper.AddIfChanged(changes, "Title", "Name", previousTitle, current.Title);
        AuditDiffHelper.AddIfChanged(changes, "Version", "Version", previousVersion, current.Version);
        AuditDiffHelper.AddIfChanged(changes, "IsActive", "Aktiv", previousIsActive, current.IsActive);
        AuditDiffHelper.AddIfChanged(changes, "TemplateType", "Vorlagentyp", previousType, current.TemplateType,
            v => v is AuditTemplateType t ? AuditTemplateLabels.GetTypeBadge(t) : AuditDiffHelper.FormatAuditValue(v));
        return changes;
    }

    private static string GetAuditRunStatusLabel(AuditRunStatus status) => status switch
    {
        AuditRunStatus.Draft => "Entwurf",
        AuditRunStatus.InProgress => "Laufend",
        AuditRunStatus.Completed => "Abgeschlossen",
        _ => status.ToString()
    };
}
