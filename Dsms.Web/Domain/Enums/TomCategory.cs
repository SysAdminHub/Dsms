namespace Dsms.Web.Domain.Enums;

/// <summary>
/// Kategorie einer technischen oder organisatorischen Maßnahme (TOM)
/// nach typischen Schutzzielgruppen im Datenschutz.
/// </summary>
public enum TomCategory
{
    PhysicalAccessControl,
    AdmissionControl,
    AccessControl,
    DisclosureControl,
    InputControl,
    OrderControl,
    AvailabilityControl,
    SeparationRequirement,
    Encryption,
    BackupAndRecovery,
    Logging,
    AuthorizationConcept,
    TrainingAndAwareness,
    Other
}
