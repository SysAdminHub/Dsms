using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;

/// <summary>Deutsche Anzeigelabels und einfache Warnlogik für DSFA (Datenschutz-Folgenabschätzung).</summary>
public static class DsfaLabels
{
    public static string GetStatusLabel(DpiaStatus status) => status switch
    {
        DpiaStatus.Draft => "Entwurf",
        DpiaStatus.InReview => "In Prüfung",
        DpiaStatus.Approved => "Freigegeben",
        DpiaStatus.Rejected => "Abgelehnt",
        DpiaStatus.RevisionRequired => "Überarbeitung erforderlich",
        DpiaStatus.Archived => "Archiviert",
        _ => status.ToString()
    };

    public static string GetStatusVariant(DpiaStatus status) => status switch
    {
        DpiaStatus.Approved => "success",
        DpiaStatus.InReview => "primary",
        DpiaStatus.Rejected => "danger",
        DpiaStatus.RevisionRequired => "warning",
        DpiaStatus.Archived => "default",
        _ => "warning"
    };

    public static string GetResidualRiskLabel(DpiaResidualRisk risk) => risk switch
    {
        DpiaResidualRisk.Low => "Niedrig",
        DpiaResidualRisk.Medium => "Mittel",
        DpiaResidualRisk.High => "Hoch",
        DpiaResidualRisk.Critical => "Kritisch",
        DpiaResidualRisk.NotEvaluated => "Noch nicht bewertet",
        _ => risk.ToString()
    };

    public static string GetResidualRiskVariant(DpiaResidualRisk risk) => risk switch
    {
        DpiaResidualRisk.Low => "success",
        DpiaResidualRisk.Medium => "warning",
        DpiaResidualRisk.High => "danger",
        DpiaResidualRisk.Critical => "danger",
        _ => "default"
    };

    public static string GetOutcomeLabel(DpiaOutcome outcome) => outcome switch
    {
        DpiaOutcome.ProcessingPermitted => "Verarbeitung zulässig",
        DpiaOutcome.PermittedWithAdditionalMeasures => "Verarbeitung nur mit zusätzlichen Maßnahmen zulässig",
        DpiaOutcome.ProcessingNotPermitted => "Verarbeitung aktuell nicht zulässig",
        DpiaOutcome.DpoConsultationRequired => "Beratung durch Datenschutzbeauftragten erforderlich",
        DpiaOutcome.SupervisoryConsultationToBeConsidered => "Konsultation der Aufsichtsbehörde zu prüfen",
        DpiaOutcome.NotEvaluated => "Noch nicht bewertet",
        _ => outcome.ToString()
    };

    public static string GetOutcomeVariant(DpiaOutcome outcome) => outcome switch
    {
        DpiaOutcome.ProcessingPermitted => "success",
        DpiaOutcome.PermittedWithAdditionalMeasures => "warning",
        DpiaOutcome.ProcessingNotPermitted => "danger",
        DpiaOutcome.DpoConsultationRequired => "warning",
        DpiaOutcome.SupervisoryConsultationToBeConsidered => "danger",
        _ => "default"
    };

    public static bool IsReviewOverdue(DateOnly? nextReviewAt) =>
        nextReviewAt.HasValue && nextReviewAt.Value < DateOnly.FromDateTime(DateTime.Today);

    public static bool IsHighOrCriticalResidualRisk(DpiaResidualRisk risk) =>
        risk is DpiaResidualRisk.High or DpiaResidualRisk.Critical;

    public static bool IsCriticalOutcome(DpiaOutcome outcome) =>
        outcome is DpiaOutcome.ProcessingNotPermitted
            or DpiaOutcome.SupervisoryConsultationToBeConsidered;

    /// <summary>Ermittelt die aktuellste DSFA (nach letzter Änderung bzw. Erstellung).</summary>
    public static DataProtectionImpactAssessment? GetLatest(
        IReadOnlyList<DataProtectionImpactAssessment> assessments) =>
        assessments
            .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
            .FirstOrDefault();

    /// <summary>Prüft, ob zentrale Pflichtfelder für eine aussagekräftige DSFA noch fehlen.</summary>
    public static bool HasIncompleteRequiredFields(DataProtectionImpactAssessment dpia) =>
        string.IsNullOrWhiteSpace(dpia.Title)
        || string.IsNullOrWhiteSpace(dpia.ReasonForDpia)
        || string.IsNullOrWhiteSpace(dpia.RiskAssessment)
        || string.IsNullOrWhiteSpace(dpia.ProtectiveMeasures);

    /// <summary>Warnhinweise für die DSFA-Detailansicht.</summary>
    public static IReadOnlyList<string> BuildDetailWarnings(DataProtectionImpactAssessment dpia)
    {
        var warnings = new List<string>();

        if (IsHighOrCriticalResidualRisk(dpia.ResidualRisk))
        {
            warnings.Add($"Restrisiko „{GetResidualRiskLabel(dpia.ResidualRisk)}“.");
        }

        if (IsReviewOverdue(dpia.NextReviewAt))
        {
            warnings.Add("Nächste Prüfung ist überfällig.");
        }

        if (IsCriticalOutcome(dpia.Outcome))
        {
            warnings.Add($"Ergebnis: {GetOutcomeLabel(dpia.Outcome)}.");
        }

        if (HasIncompleteRequiredFields(dpia))
        {
            warnings.Add("Pflichtfelder unvollständig (Titel, Grund für DSFA, Risikobewertung oder Schutzmaßnahmen).");
        }

        return warnings;
    }

    /// <summary>Warnhinweise für die Verarbeitungstätigkeit bezogen auf DSFA.</summary>
    public static IReadOnlyList<string> BuildProcessingActivityWarnings(
        ProcessingActivity activity,
        IReadOnlyList<DataProtectionImpactAssessment> assessments)
    {
        var warnings = new List<string>();
        var latest = GetLatest(assessments);

        if (activity.DpiaRequired && assessments.Count == 0)
        {
            warnings.Add("DSFA erforderlich, aber noch keine DSFA angelegt.");
        }

        if (latest is not null)
        {
            if (IsHighOrCriticalResidualRisk(latest.ResidualRisk))
            {
                warnings.Add($"DSFA mit Restrisiko „{GetResidualRiskLabel(latest.ResidualRisk)}“ ({latest.Title}).");
            }

            if (IsReviewOverdue(latest.NextReviewAt))
            {
                warnings.Add($"DSFA-Prüfung überfällig ({latest.Title}).");
            }

            if (latest.Status == DpiaStatus.RevisionRequired)
            {
                warnings.Add($"DSFA-Status „Überarbeitung erforderlich“ ({latest.Title}).");
            }

            if (latest.Outcome == DpiaOutcome.ProcessingNotPermitted)
            {
                warnings.Add($"DSFA-Ergebnis „Verarbeitung aktuell nicht zulässig“ ({latest.Title}).");
            }
        }

        return warnings;
    }
}
