namespace Dsms.Web.Domain.Enums;

/// <summary>
/// Zustand des optionalen Schulungsmoduls je Lizenz im neuen Preis-/Lizenzmodell.
/// Schulungsdaten bleiben nach Ablauf erhalten – es wird ausschließlich der Zugriff gesteuert.
/// </summary>
public enum TrainingModuleStatus
{
    /// <summary>Schulungsmodul ist nicht freigeschaltet.</summary>
    Disabled = 0,

    /// <summary>Kostenloser Testzeitraum; aktiv bis <c>TrainingModuleTrialEndsAt</c>.</summary>
    Trial = 1,

    /// <summary>Bezahlt freigeschaltet bis zu einem festen Datum (<c>TrainingModuleValidUntil</c>).</summary>
    ActiveUntil = 2,

    /// <summary>Dauerhaft freigeschaltet (kein Ablaufdatum).</summary>
    Unlimited = 3
}
