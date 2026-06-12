namespace Dsms.Web.Domain.Enums;

/// <summary>Zieltyp einer Dokument-Verknüpfung in <see cref="Entities.DocumentLink"/>.</summary>
public enum DocumentLinkedEntityType
{
    AuditRun = 0,
    Measure = 1,
    ServiceProvider = 2,
    ProcessingActivity = 3,
    Dsfa = 4,
    PrivacyIncident = 5,
    Tom = 6,
    DataSubjectRequest = 7
}
