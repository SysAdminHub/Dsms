namespace Dsms.Web.Services.Privacy;

/// <summary>
/// Anonymisiert IP-Adressen für die Speicherung in Nachweisdatensätzen (z. B. LegalAcceptance).
/// IPv4: letztes Oktett auf 0. IPv6: /64-Präfix, hintere 64 Bits auf 0.
/// </summary>
public interface IIpAnonymizationService
{
    string? AnonymizeIpAddress(string? ipAddress);
}
