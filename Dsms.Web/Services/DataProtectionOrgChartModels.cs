namespace Dsms.Web.Services;

public sealed class DataProtectionOrgChartFilter
{
    public bool IncludeInactive { get; set; }
    public string? SearchText { get; set; }
    public string? RoleTitle { get; set; }
    public string? Department { get; set; }
}

public sealed class DataProtectionOrgChartNodeViewModel
{
    public int Id { get; init; }
    public string RoleTitle { get; init; } = string.Empty;
    public string? PersonName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Department { get; init; }
    public string? AreaOfResponsibility { get; init; }
    public string? AreaOfResponsibilityShort { get; init; }
    public bool IsActive { get; init; }
    public string? LinkedUserId { get; init; }
    public string? LinkedUserDisplayName { get; init; }
    public int? ReportsToRoleId { get; init; }
    public string? ReportsToFreeText { get; init; }
    public string? ReportsToDisplay { get; init; }
    public int? DeputyRoleId { get; init; }
    public string? DeputyDisplayText { get; init; }
    public string? DeputyShortText { get; init; }
    public string? Remarks { get; init; }
    public bool IsInCycle { get; init; }
    public bool HasOrphanedReportsTo { get; init; }
    public List<DataProtectionOrgChartNodeViewModel> Children { get; } = [];
}

public sealed class DataProtectionOrgChartViewModel
{
    public IReadOnlyList<DataProtectionOrgChartNodeViewModel> RootNodes { get; init; } = [];
    public IReadOnlyList<DataProtectionOrgChartNodeViewModel> CycleNodes { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public int ActiveCount { get; init; }
    public int InactiveCount { get; init; }
    public int WithoutReportsToCount { get; init; }
    public int LinkedUserCount { get; init; }
    public int WithDeputyCount { get; init; }
    public bool HasRolesWithoutReportingLine { get; init; }
}
