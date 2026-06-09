using System.Net.Mail;

namespace Dsms.Web.Services.Licenses;

internal static class PlanToLicenseValidator
{
    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Active", "Inactive", "Suspended" };

    public static void ValidateForCreate(CreateLicenseFromPlanDto dto)
    {
        if (dto.PlanId == Guid.Empty)
        {
            throw new InvalidOperationException("Bitte wählen Sie einen Tarif aus.");
        }

        if (string.IsNullOrWhiteSpace(dto.CustomerName))
        {
            throw new InvalidOperationException("Bitte geben Sie einen Kundennamen ein.");
        }

        var status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status.Trim();
        if (!AllowedStatuses.Contains(status))
        {
            throw new InvalidOperationException("Der Status ist ungültig.");
        }

        if (!string.IsNullOrWhiteSpace(dto.CustomerEmail) && !IsValidEmail(dto.CustomerEmail))
        {
            throw new InvalidOperationException("Die E-Mail-Adresse ist ungültig.");
        }

        var validFrom = dto.ValidFrom ?? DateTime.UtcNow.Date;
        if (dto.ValidUntil.HasValue && dto.ValidUntil.Value < validFrom)
        {
            throw new InvalidOperationException("Das Ablaufdatum darf nicht vor dem Startdatum liegen.");
        }
    }

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(email.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }
}
