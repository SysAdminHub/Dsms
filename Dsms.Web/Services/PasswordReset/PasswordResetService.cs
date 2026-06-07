using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using Dsms.Web.Data;
using Microsoft.AspNetCore.Components;
using Dsms.Web.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Dsms.Web.Services.PasswordReset;

public sealed class PasswordResetService(
    UserManager<ApplicationUser> userManager,
    IUserAccessService access,
    IEmailService emailService,
    IDbContextFactory<ApplicationDbContext> dbFactory,
    NavigationManager navigationManager,
    IDistributedCache cache,
    IOptions<DataProtectionTokenProviderOptions> tokenOptions,
    ILogger<PasswordResetService> logger) : IPasswordResetService
{
    private const int SelfServiceCooldownMinutes = 5;
    private static readonly TimeSpan SelfServiceCooldown = TimeSpan.FromMinutes(SelfServiceCooldownMinutes);

    public string NeutralSelfServiceMessage =>
        "Falls ein Konto mit dieser Emailadresse existiert, wurde eine Email zum Zurücksetzen des Passworts versendet.";

    public async Task<PasswordResetSelfServiceResult> RequestSelfServiceResetAsync(string email)
    {
        if (!IsValidEmail(email))
        {
            return new PasswordResetSelfServiceResult
            {
                ValidationError = "Bitte eine gültige Emailadresse eingeben."
            };
        }

        var normalizedEmail = email.Trim();
        var user = await userManager.FindByEmailAsync(normalizedEmail);

        if (user is null || !user.IsActive)
        {
            return new PasswordResetSelfServiceResult { Message = NeutralSelfServiceMessage };
        }

        if (await IsRateLimitedAsync(normalizedEmail))
        {
            return new PasswordResetSelfServiceResult { Message = NeutralSelfServiceMessage };
        }

        var sendResult = await SendResetEmailAsync(user);
        await MarkRateLimitedAsync(normalizedEmail);

        if (!sendResult.Succeeded)
        {
            logger.LogWarning(
                "Passwortreset-Mail für {Email} konnte nicht gesendet werden: {Detail}",
                normalizedEmail,
                sendResult.DetailMessage ?? sendResult.Message);
        }

        return new PasswordResetSelfServiceResult { Message = NeutralSelfServiceMessage };
    }

    public async Task<PasswordResetAdminResult> SendAdminResetAsync(string userId)
    {
        if (!await access.CanManageUsersAsync())
        {
            return new PasswordResetAdminResult
            {
                Succeeded = false,
                Message = "Keine Berechtigung zur Benutzerverwaltung."
            };
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !await access.CanManageUserAsync(user))
        {
            return new PasswordResetAdminResult
            {
                Succeeded = false,
                Message = "Benutzer nicht gefunden oder kein Zugriff."
            };
        }

        if (user.Email is null)
        {
            return new PasswordResetAdminResult
            {
                Succeeded = false,
                Message = "Passwortreset-Mail konnte nicht gesendet werden. Bitte Email-Einstellungen prüfen."
            };
        }

        if (await IsRateLimitedAsync(user.Email))
        {
            return new PasswordResetAdminResult
            {
                Succeeded = false,
                Message = $"Für diesen Benutzer wurde kürzlich bereits eine Passwortreset-Mail versendet. Bitte {SelfServiceCooldownMinutes} Minuten warten."
            };
        }

        var sendResult = await SendResetEmailAsync(user);
        await MarkRateLimitedAsync(user.Email);

        if (!sendResult.Succeeded)
        {
            logger.LogWarning(
                "Admin-Passwortreset für Benutzer {UserId} fehlgeschlagen: {Detail}",
                userId,
                sendResult.DetailMessage ?? sendResult.Message);

            return new PasswordResetAdminResult
            {
                Succeeded = false,
                Message = "Passwortreset-Mail konnte nicht gesendet werden. Bitte Email-Einstellungen prüfen."
            };
        }

        return new PasswordResetAdminResult
        {
            Succeeded = true,
            Message = "Passwortreset-Mail wurde gesendet."
        };
    }

    public async Task<PasswordResetChangeResult> ChangePasswordAsync(
        string email,
        string encodedToken,
        string newPassword,
        string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(encodedToken))
        {
            return InvalidLinkResult();
        }

        if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            return new PasswordResetChangeResult
            {
                Succeeded = false,
                Message = "Bitte alle Felder ausfüllen.",
                FieldErrors = ["Passwort darf nicht leer sein."]
            };
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            return new PasswordResetChangeResult
            {
                Succeeded = false,
                Message = "Neues Passwort und Wiederholung stimmen nicht überein.",
                FieldErrors = ["Neues Passwort und Wiederholung müssen übereinstimmen."]
            };
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
        catch (FormatException)
        {
            return InvalidLinkResult();
        }

        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return InvalidLinkResult();
        }

        var result = await userManager.ResetPasswordAsync(user, decodedToken, newPassword);
        if (result.Succeeded)
        {
            return new PasswordResetChangeResult
            {
                Succeeded = true,
                Message = "Dein Passwort wurde geändert. Du kannst dich jetzt anmelden."
            };
        }

        if (result.Errors.Any(e => e.Code is "InvalidToken" or "ExpiredToken"))
        {
            return InvalidLinkResult();
        }

        return new PasswordResetChangeResult
        {
            Succeeded = false,
            Message = "Das Passwort konnte nicht geändert werden.",
            FieldErrors = result.Errors.Select(MapIdentityError).ToList()
        };
    }

    private async Task<EmailOperationResult> SendResetEmailAsync(ApplicationUser user)
    {
        if (user.Email is null)
        {
            return EmailOperationResult.Fail("Benutzer hat keine Emailadresse.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var resetLink = navigationManager.GetUriWithQueryParameters(
            navigationManager.ToAbsoluteUri("passwort-zuruecksetzen").AbsoluteUri,
            new Dictionary<string, object?>
            {
                ["email"] = user.Email,
                ["token"] = encodedToken
            });

        var displayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email : user.DisplayName;
        var expiresMinutes = (int)Math.Round(tokenOptions.Value.TokenLifespan.TotalMinutes);
        var supportEmail = await GetSupportEmailAsync();

        return await emailService.SendPasswordResetEmailAsync(
            user.Email,
            displayName,
            resetLink,
            expiresMinutes,
            user.TenantId,
            supportEmail);
    }

    private async Task<string> GetSupportEmailAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var settings = await db.EmailSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync();
        return settings?.SenderEmail ?? string.Empty;
    }

    private async Task<bool> IsRateLimitedAsync(string email)
    {
        var key = CacheKey(email);
        var value = await cache.GetStringAsync(key);
        return value is not null;
    }

    private Task MarkRateLimitedAsync(string email) =>
        cache.SetStringAsync(
            CacheKey(email),
            DateTime.UtcNow.ToString("O"),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = SelfServiceCooldown });

    private static string CacheKey(string email) =>
        $"dsms:pwd-reset:{email.Trim().ToLowerInvariant()}";

    private static PasswordResetChangeResult InvalidLinkResult() =>
        new()
        {
            Succeeded = false,
            Message = "Der Link ist ungültig oder abgelaufen. Bitte fordere einen neuen Passwortreset an."
        };

    private static string MapIdentityError(IdentityError error) => error.Code switch
    {
        "PasswordTooShort" => $"Das Passwort muss mindestens 8 Zeichen lang sein.",
        "PasswordRequiresDigit" => "Das Passwort muss mindestens eine Ziffer enthalten.",
        "PasswordRequiresLower" => "Das Passwort muss mindestens einen Kleinbuchstaben enthalten.",
        _ => error.Description
    };

    private static bool IsValidEmail(string? email)
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
