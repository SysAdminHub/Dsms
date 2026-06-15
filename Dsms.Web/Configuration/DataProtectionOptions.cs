namespace Dsms.Web.Configuration;

/// <summary>
/// ASP.NET Data Protection für Identity-Tokens, SMTP-Secrets und Session-Verschlüsselung.
/// Muss mit Dsms.Provisioning übereinstimmen (ApplicationName + gemeinsamer Key-Ring).
/// </summary>
public sealed class DataProtectionOptions
{
    public const string SectionName = "DataProtection";

    public string ApplicationName { get; set; } = "DatenschutzCloud";

    /// <summary>
    /// Relativer oder absoluter Pfad zum Key-Ring.
    /// Lokal typisch <c>../DataProtection-Keys</c> (Solution-Root, von beiden Apps geteilt).
    /// </summary>
    public string KeysPath { get; set; } = "../DataProtection-Keys";
}
