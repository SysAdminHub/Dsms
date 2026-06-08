namespace Dsms.Web.Services.Licenses;

public static class LicenseDisplayHelper
{
    public const string NoLicenseText = "Keine Lizenz zugeordnet";
    public const string SystemLicenseText = "System / keine Kundenlizenz";
    public const string MultipleLicensesText = "Mehrere Lizenzen";

    public static string FormatOptionDisplay(string licenseNumber, string customerName, string planName) =>
        $"{licenseNumber} - {customerName} - {planName}";

    public static string FormatCompactDisplay(string? licenseNumber, string? customerName, string? planName)
    {
        if (string.IsNullOrWhiteSpace(licenseNumber) || string.IsNullOrWhiteSpace(customerName))
        {
            return NoLicenseText;
        }

        return string.IsNullOrWhiteSpace(planName)
            ? $"{licenseNumber} - {customerName}"
            : $"{licenseNumber} - {customerName} ({planName})";
    }

    public static string FormatCustomerPlanDisplay(string? customerName, string? planName)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            return NoLicenseText;
        }

        return string.IsNullOrWhiteSpace(planName)
            ? customerName
            : $"{customerName} ({planName})";
    }
}
