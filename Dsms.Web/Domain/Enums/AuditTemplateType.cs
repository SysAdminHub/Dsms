namespace Dsms.Web.Domain.Enums;

/// <summary>
/// Herkunft und Sichtbarkeit einer Audit-Vorlage.
/// </summary>
public enum AuditTemplateType
{
    /// <summary>Eigene Vorlage eines Mandanten – nur im eigenen Mandanten sichtbar.</summary>
    Tenant = 0,

    /// <summary>Offizielle Vorlage des Plattformbetreibers – für alle Mandanten sichtbar.</summary>
    Official = 1,

    /// <summary>Freigegebene Community-Vorlage – von einem Mandanten eingereicht, für alle sichtbar.</summary>
    Community = 2
}
