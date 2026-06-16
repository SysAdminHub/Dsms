namespace Dsms.Web.Services.CommunityTemplates;

public enum CommunityTemplateNotificationType
{
    Audit,
    Training
}

public sealed class CommunityTemplateNotificationModel
{
    public required CommunityTemplateNotificationType TemplateType { get; init; }
    public required int TemplateId { get; init; }
    public required string TemplateTitle { get; init; }
    public required int TenantId { get; init; }
    public required string TenantName { get; init; }
    public required string SubmittedByUserId { get; init; }
    public string? SubmittedByName { get; init; }
    public string? SubmittedByEmail { get; init; }
    public required DateTime SubmittedAt { get; init; }
}
