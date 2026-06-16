namespace Dsms.Web.Services.Licenses;

/// <summary>
/// Prüft lizenzbasierte Feature-Freischaltungen für den aktiven Mandanten.
/// </summary>
public interface ILicenseFeatureService
{
    /// <summary>
    /// Prüft, ob das Schulungsmodul für den aktuellen Mandantenkontext aktiv ist.
    /// </summary>
    Task<bool> HasTrainingModuleAsync(CancellationToken ct = default);

    /// <summary>
    /// Prüft, ob das Schulungsmodul für einen bestimmten Mandanten aktiv ist.
    /// </summary>
    Task<bool> HasTrainingModuleAsync(int tenantId, CancellationToken ct = default);
}
