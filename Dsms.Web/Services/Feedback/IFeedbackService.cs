namespace Dsms.Web.Services.Feedback;

/// <summary>Versendet Benutzer-Feedback per E-Mail an den Support.</summary>
public interface IFeedbackService
{
    Task<FeedbackResult> SendFeedbackAsync(FeedbackMessageModel model);
}
