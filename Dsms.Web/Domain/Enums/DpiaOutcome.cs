namespace Dsms.Web.Domain.Enums;

/// <summary>Ergebnis / Bewertung der Zulässigkeit der Verarbeitung in der DSFA.</summary>
public enum DpiaOutcome
{
    NotEvaluated = 0,
    ProcessingPermitted = 1,
    PermittedWithAdditionalMeasures = 2,
    ProcessingNotPermitted = 3,
    DpoConsultationRequired = 4,
    SupervisoryConsultationToBeConsidered = 5
}
