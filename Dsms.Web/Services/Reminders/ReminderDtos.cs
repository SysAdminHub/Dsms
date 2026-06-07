namespace Dsms.Web.Services.Reminders;

public sealed class ReminderItemDto
{
    public int TenantId { get; init; }
    public string TenantName { get; init; } = "";
    public ReminderType ReminderType { get; init; }
    public int EntityId { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public DateOnly? DueDate { get; init; }
    public int? DaysUntilDue { get; init; }
    public bool IsOverdue { get; init; }
    public string? DetailUrl { get; init; }
}

public sealed class ReminderRecipientDto
{
    public string UserId { get; init; } = "";
    public string Email { get; init; } = "";
    public string DisplayName { get; init; } = "";
}

public sealed class ReminderTenantPreviewDto
{
    public int TenantId { get; init; }
    public string TenantName { get; init; } = "";
    public IReadOnlyList<ReminderRecipientDto> AdminRecipients { get; init; } = [];
    public IReadOnlyList<ReminderItemDto> Items { get; init; } = [];
    public string? WarningMessage { get; init; }
}

public sealed class ReminderPreviewResult
{
    public IReadOnlyList<ReminderTenantPreviewDto> Tenants { get; init; } = [];
    public bool HasAnyItems => Tenants.Any(t => t.Items.Count > 0);
}

public sealed class ReminderTenantSendResultDto
{
    public int TenantId { get; init; }
    public string TenantName { get; init; } = "";
    public int RecipientCount { get; init; }
    public int ItemCount { get; init; }
    public bool Success { get; init; }
    public bool Skipped { get; init; }
    public string? Message { get; init; }
}

public sealed class ReminderSendResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = "";
    public IReadOnlyList<ReminderTenantSendResultDto> TenantResults { get; init; } = [];
}
