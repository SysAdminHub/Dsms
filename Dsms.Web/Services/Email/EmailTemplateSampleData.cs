using Dsms.Web.Configuration;

namespace Dsms.Web.Services.Email;

/// <summary>Beispieldaten für Vorlagen-Vorschau und Testmails aus dem Superuser-Bereich.</summary>
public static class EmailTemplateSampleData
{
    public static IReadOnlyDictionary<string, string> AsDictionary(AppBrandingOptions branding) =>
        new Dictionary<string, string>
        {
            ["AppName"] = branding.ProductName,
            ["ProductName"] = branding.ProductName,
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
            ["SupportEmail"] = branding.SupportEmail,
            ["Category"] = "Fehler melden",
            ["Subject"] = "Beispiel-Betreff",
            ["Message"] = "Beispiel-Nachricht des Benutzers.",
            ["UserRole"] = "Admin",
            ["TenantId"] = "1",
            ["CurrentUrl"] = "https://example.com/processing-activities",
            ["AppVersion"] = "1.0.1",
            ["CreatedAt"] = "10.06.2026 14:30"
        };
}
