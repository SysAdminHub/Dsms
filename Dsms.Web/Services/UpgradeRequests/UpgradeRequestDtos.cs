namespace Dsms.Web.Services.UpgradeRequests;

public static class UpgradeRequestConstants
{
    public const string IndividualUpgradeOption = "__individual__";
}

public sealed class UpgradeTargetPlanDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string DisplayLabel { get; init; } = string.Empty;
    public decimal? PriceMonthly { get; init; }
    public decimal? PriceYearly { get; init; }
    public string Currency { get; init; } = "EUR";
    public bool IsFree { get; init; }
    public int SortOrder { get; init; }
    public int? MaxTenants { get; init; }
    public int? MaxAdmins { get; init; }
    public int? MaxUsersPerTenant { get; init; }
    public int? MaxDpiaPerTenant { get; init; }
    public int? MaxTomsPerTenant { get; init; }
    public int? MaxStorageMb { get; init; }
}

public sealed class UpgradeRequestInput
{
    public string? SelectedOption { get; set; }
    public string? Message { get; set; }
}

public sealed class UpgradeRequestResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsIndividual { get; init; }

    public static UpgradeRequestResult Ok(string message, bool isIndividual = false) =>
        new() { Succeeded = true, Message = message, IsIndividual = isIndividual };

    public static UpgradeRequestResult Fail(string message) =>
        new() { Succeeded = false, Message = message };
}
