namespace Dsms.Web.Domain;

/// <summary>Eindeutige Schlüssel für globale Email-Vorlagen (nicht frei änderbar nach Anlage).</summary>
public static class EmailTemplateKeys
{
    public const string PasswordReset = "PasswordReset";
    public const string WelcomeSetPassword = "WelcomeSetPassword";
    public const string Reminder = "Reminder";
    public const string TestEmail = "TestEmail";
    public const string FeedbackMessageToSupport = "FeedbackMessageToSupport";
    public const string SignupLegalConfirmation = "SignupLegalConfirmation";
    public const string TrainingInvitation = "TrainingInvitation";

    public static readonly string[] All =
        [PasswordReset, WelcomeSetPassword, Reminder, TestEmail, FeedbackMessageToSupport, SignupLegalConfirmation, TrainingInvitation];
}
