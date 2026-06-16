namespace Dsms.Web.Services.CommunityTemplates;

public sealed class CommunityTemplateReviewNotificationModel
{
    public required CommunityTemplateNotificationType TemplateType { get; init; }
    public required int TemplateId { get; init; }
    public required string TemplateTitle { get; init; }
    public int? TenantId { get; init; }
    public required string SubmittedByUserId { get; init; }
    public string? SubmittedByName { get; init; }
    public string? SubmittedByEmail { get; init; }
    public required bool IsApproved { get; init; }
    public string? ReviewComment { get; init; }
    public required DateTime ReviewedAt { get; init; }
}
