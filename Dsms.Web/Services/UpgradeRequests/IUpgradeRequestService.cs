using Dsms.Web.Services.Licenses;

namespace Dsms.Web.Services.UpgradeRequests;

/// <summary>
/// Versendet gezielte Lizenz-Anfragen per E-Mail an die konfigurierte System-E-Mail.
/// Es erfolgt keine automatische Lizenzänderung und keine automatische Mandantenanlage.
/// </summary>
public interface IUpgradeRequestService
{
    /// <summary>Fragt die Freischaltung des Schulungsmoduls an.</summary>
    Task<UpgradeRequestResult> SubmitTrainingModuleRequestAsync(
        LicenseDetailsDto license,
        TrainingModuleRequestInput input);

    /// <summary>Fragt einen weiteren Mandanten im Rahmen des bestehenden Lizenzlimits an.</summary>
    Task<UpgradeRequestResult> SubmitAdditionalTenantRequestAsync(
        LicenseDetailsDto license,
        AdditionalTenantRequestInput input);

    /// <summary>Fragt eine Mandantenerweiterung an, wenn das Mandantenlimit erreicht ist.</summary>
    Task<UpgradeRequestResult> SubmitTenantExpansionRequestAsync(
        LicenseDetailsDto license,
        TenantExpansionRequestInput input);
}
