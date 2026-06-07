namespace Dsms.Web.Services.Email;

/// <summary>Beispieldaten für Vorlagen-Vorschau und Testmails aus dem Superuser-Bereich.</summary>
public static class EmailTemplateSampleData
{
    public const string AppName = "DSMS";

    public static IReadOnlyDictionary<string, string> AsDictionary() => new Dictionary<string, string>
    {
        ["AppName"] = AppName,
        ["UserName"] = "Max Mustermann",
        ["UserEmail"] = "max.mustermann@example.com",
        ["TenantName"] = "Demo GmbH",
        ["ResetLink"] = "https://example.com/reset-password/demo-token",
        ["InviteLink"] = "https://example.com/set-password/demo-token",
        ["ExpiresInMinutes"] = "60",
        ["ReminderTitle"] = "Offene Maßnahme prüfen",
        ["ReminderText"] = "Bitte prüfe die offene Maßnahme im Datenschutzmanagementsystem.",
        ["DueDate"] = "31.12.2026",
        ["ActionLink"] = "https://example.com/measures/1",
        ["SupportEmail"] = "support@example.com"
    };
}
