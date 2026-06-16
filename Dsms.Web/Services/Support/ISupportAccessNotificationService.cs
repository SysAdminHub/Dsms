namespace Dsms.Web.Services.Support;

public interface ISupportAccessNotificationService
{
    /// <summary>
    /// Versendet eine Systembenachrichtigung nach erfolgreicher Supportzugriffs-Freigabe.
    /// Fehler beim Versand blockieren die Fachfunktion nicht.
    /// </summary>
    Task TrySendSupportAccessRequestedNotificationAsync(
        int grantId,
        CancellationToken cancellationToken = default);
}
