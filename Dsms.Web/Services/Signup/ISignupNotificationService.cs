namespace Dsms.Web.Services.Signup;

public interface ISignupNotificationService
{
    /// <summary>
    /// Versendet interne Benachrichtigung nach erfolgreichem Public Signup.
    /// Fehler werden geloggt; wirft keine Exception.
    /// </summary>
    Task TrySendPublicSignupNotificationAsync(Guid pendingSignupId, bool passwordSetupEmailSent);
}
