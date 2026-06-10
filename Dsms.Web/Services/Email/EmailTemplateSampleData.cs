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
            ["SupportEmail"] = branding.SupportEmail
        };
}
