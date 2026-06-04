namespace Dsms.Web.Domain.Enums;

/// <summary>
/// Lebenszyklus einer Verarbeitungstätigkeit im Verzeichnis von Verarbeitungstätigkeiten (Art. 30 DSGVO).
/// </summary>
public enum ProcessingActivityStatus
{
    /// <summary>Entwurf – noch nicht freigegeben.</summary>
    Draft = 0,

    /// <summary>Aktiv – im Verzeichnis geführt und gültig.</summary>
    Active = 1,

    /// <summary>In Prüfung – z. B. durch Datenschutzbeauftragten.</summary>
    InReview = 2,

    /// <summary>Archiviert – nicht mehr aktiv, aber aufbewahrt.</summary>
    Archived = 3
}
