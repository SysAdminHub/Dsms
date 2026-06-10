namespace Dsms.Web.Services.Feedback;

/// <summary>Ergebnis des Feedback-Versands.</summary>
public sealed class FeedbackResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;

    public static FeedbackResult Ok(string message) =>
        new() { Succeeded = true, Message = message };

    public static FeedbackResult Fail(string message) =>
        new() { Succeeded = false, Message = message };
}
