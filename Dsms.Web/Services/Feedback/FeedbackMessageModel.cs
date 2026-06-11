namespace Dsms.Web.Services.Feedback;

/// <summary>Eingabedaten für eine Feedback-Nachricht.</summary>
public sealed class FeedbackMessageModel
{
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? CurrentUrl { get; set; }
}
