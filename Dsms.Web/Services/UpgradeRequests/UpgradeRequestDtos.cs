namespace Dsms.Web.Services.UpgradeRequests;

/// <summary>Anfrage zur Freischaltung des Schulungsmoduls (keine automatische Freischaltung).</summary>
public sealed class TrainingModuleRequestInput
{
    public string? Message { get; set; }
}

/// <summary>Anfrage für einen weiteren Mandanten im Rahmen des bestehenden Lizenzlimits.</summary>
public sealed class AdditionalTenantRequestInput
{
    public string? TenantName { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
    public string? Message { get; set; }
}

/// <summary>Anfrage für eine Mandantenerweiterung, wenn das Lizenzlimit erreicht ist.</summary>
public sealed class TenantExpansionRequestInput
{
    public string? Message { get; set; }
}

public sealed class UpgradeRequestResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;

    public static UpgradeRequestResult Ok(string message) =>
        new() { Succeeded = true, Message = message };

    public static UpgradeRequestResult Fail(string message) =>
        new() { Succeeded = false, Message = message };
}
