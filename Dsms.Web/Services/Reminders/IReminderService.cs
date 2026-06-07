namespace Dsms.Web.Services.Reminders;

/// <summary>
/// Zentrale Erinnerungslogik (Version 1: manueller Versandassistent, keine History, kein Background-Job).
/// </summary>
public interface IReminderService
{
    Task<ReminderPreviewResult> GetReminderPreviewAsync();
    Task<ReminderSendResult> SendRemindersAsync();
}
