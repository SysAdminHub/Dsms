namespace Dsms.Web.Services.CommunityTemplates;

public interface ICommunityTemplateNotificationService
{
    /// <summary>
    /// Versendet eine Systembenachrichtigung nach erfolgreicher Community-Einreichung.
    /// Fehler werden geloggt; wirft keine Exception.
    /// </summary>
    Task TryNotifyCommunityTemplateSubmittedAsync(
        CommunityTemplateNotificationModel model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Benachrichtigt den einreichenden Benutzer über das Ergebnis der Community-Prüfung.
    /// Fehler werden geloggt; wirft keine Exception.
    /// </summary>
    Task TryNotifyCommunityTemplateReviewedAsync(
        CommunityTemplateReviewNotificationModel model,
        CancellationToken cancellationToken = default);
}
