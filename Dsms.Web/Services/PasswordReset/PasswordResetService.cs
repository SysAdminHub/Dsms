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

        if (await IsRateLimitedAsync("pwd-reset", normalizedEmail))
        {
            return new PasswordResetSelfServiceResult { Message = NeutralSelfServiceMessage };
        }

        var sendResult = await SendResetEmailAsync(user);
        await MarkRateLimitedAsync("pwd-reset", normalizedEmail);

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
        var user = await FindManagedUserAsync(userId);
        if (user.Error is not null)
        {
            return user.Error;
        }

        if (user.User!.Email is null)
        {
            return FailAdmin("Passwortreset-Mail konnte nicht gesendet werden. Bitte Email-Einstellungen prüfen.");
        }

        if (await IsRateLimitedAsync("pwd-reset", user.User.Email))
        {
            return FailAdmin(
                $"Für diesen Benutzer wurde kürzlich bereits eine Passwortreset-Mail versendet. Bitte {SelfServiceCooldownMinutes} Minuten warten.");
        }

        var sendResult = await SendResetEmailAsync(user.User);
        await MarkRateLimitedAsync("pwd-reset", user.User.Email);

        if (!sendResult.Succeeded)
        {
            logger.LogWarning(
                "Admin-Passwortreset für Benutzer {UserId} fehlgeschlagen: {Detail}",
                userId,
                sendResult.DetailMessage ?? sendResult.Message);

            return FailAdmin("Passwortreset-Mail konnte nicht gesendet werden. Bitte Email-Einstellungen prüfen.");
        }

        return new PasswordResetAdminResult
        {
            Succeeded = true,
            Message = "Passwortreset-Mail wurde gesendet."
        };
    }

    public async Task<PasswordResetAdminResult> SendWelcomeInvitationAsync(string userId)
    {
        var user = await FindManagedUserAsync(userId);
        if (user.Error is not null)
        {
            return user.Error;
        }

        if (user.User!.Email is null)
        {
            return FailAdmin("Willkommensmail konnte nicht gesendet werden. Bitte Email-Einstellungen prüfen.");
        }

        if (await IsRateLimitedAsync("invite", user.User.Email))
        {
            return FailAdmin(
                $"Für diesen Benutzer wurde kürzlich bereits eine Willkommensmail versendet. Bitte {SelfServiceCooldownMinutes} Minuten warten.");
        }

        var tenantName = await ResolveTenantNameAsync(user.User);
        var sendResult = await SendWelcomeEmailAsync(user.User, tenantName);
        await MarkRateLimitedAsync("invite", user.User.Email);

        if (!sendResult.Succeeded)
        {
            logger.LogWarning(
                "Willkommensmail für Benutzer {UserId} fehlgeschlagen: {Detail}",
                userId,
                sendResult.DetailMessage ?? sendResult.Message);

            return FailAdmin("Willkommensmail konnte nicht gesendet werden. Bitte Email-Einstellungen prüfen.");
        }

        return new PasswordResetAdminResult
        {
            Succeeded = true,
            Message = "Willkommensmail wurde erneut gesendet."
        };
    }

    public async Task<PasswordResetAdminResult> SendProvisioningWelcomeEmailAsync(string userId, string tenantName)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            return FailAdmin("Benutzer nicht gefunden oder inaktiv.");
        }

        if (user.Email is null)
        {
            return FailAdmin("Willkommensmail konnte nicht gesendet werden. Benutzer hat keine E-Mail-Adresse.");
        }

        if (await IsRateLimitedAsync("invite", user.Email))
        {
            return FailAdmin(
                $"Für diesen Benutzer wurde kürzlich bereits eine Willkommensmail versendet. Bitte {SelfServiceCooldownMinutes} Minuten warten.");
        }

        var sendResult = await SendWelcomeEmailAsync(user, tenantName);
        await MarkRateLimitedAsync("invite", user.Email);

        if (!sendResult.Succeeded)
        {
            logger.LogWarning(
                "Provisioning-Willkommensmail für Benutzer {UserId} fehlgeschlagen: {Detail}",
                userId,
                sendResult.DetailMessage ?? sendResult.Message);

            return FailAdmin("Die Passwortvergabe-Mail konnte nicht versendet werden.");
        }

        return new PasswordResetAdminResult
        {
            Succeeded = true,
            Message = "Willkommensmail wurde gesendet."
        };
    }

    public async Task<PasswordResetChangeResult> ChangePasswordAsync(
        string? email,
        string? userId,
        string encodedToken,
        string newPassword,
        string confirmPassword,
        bool isInviteMode = false)
    {
        if (string.IsNullOrWhiteSpace(encodedToken))
        {
            return InvalidLinkResult(isInviteMode);
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
            return InvalidLinkResult(isInviteMode);
        }

        var user = await ResolveUserAsync(email, userId);
        if (user is null)
        {
            return InvalidLinkResult(isInviteMode);
        }

        var result = await userManager.ResetPasswordAsync(user, decodedToken, newPassword);
        if (result.Succeeded)
        {
            return new PasswordResetChangeResult
            {
                Succeeded = true,
                Message = isInviteMode
                    ? "Dein Passwort wurde festgelegt. Du kannst dich jetzt anmelden."
                    : "Dein Passwort wurde geändert. Du kannst dich jetzt anmelden."
            };
        }

        if (result.Errors.Any(e => e.Code is "InvalidToken" or "ExpiredToken"))
        {
            return InvalidLinkResult(isInviteMode);
        }

        return new PasswordResetChangeResult
        {
            Succeeded = false,
            Message = "Das Passwort konnte nicht geändert werden.",
            FieldErrors = result.Errors.Select(MapIdentityError).ToList()
        };
    }

    private async Task<(ApplicationUser? User, PasswordResetAdminResult? Error)> FindManagedUserAsync(string userId)
    {
        if (!await access.CanManageUsersAsync())
        {
            return (null, FailAdmin("Keine Berechtigung zur Benutzerverwaltung."));
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !await access.CanManageUserAsync(user))
        {
            return (null, FailAdmin("Benutzer nicht gefunden oder kein Zugriff."));
        }

        return (user, null);
    }

    private async Task<ApplicationUser?> ResolveUserAsync(string? email, string? userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return await userManager.FindByIdAsync(userId);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            return await userManager.FindByEmailAsync(email.Trim());
        }

        return null;
    }

    private async Task<EmailOperationResult> SendResetEmailAsync(ApplicationUser user)
    {
        if (user.Email is null)
        {
            return EmailOperationResult.Fail("Benutzer hat keine Emailadresse.");
        }

        var (encodedToken, expiresMinutes) = await GenerateEncodedTokenAsync(user);
        var resetLink = BuildPasswordResetLink(user, encodedToken);
        var displayName = GetDisplayName(user);
        var supportEmail = await GetSupportEmailAsync();

        return await emailService.SendPasswordResetEmailAsync(
            user.Email,
            displayName,
            resetLink,
            expiresMinutes,
            user.TenantId,
            supportEmail);
    }

    private async Task<EmailOperationResult> SendWelcomeEmailAsync(ApplicationUser user, string tenantName)
    {
        if (user.Email is null)
        {
            return EmailOperationResult.Fail("Benutzer hat keine Emailadresse.");
        }

        var (encodedToken, expiresMinutes) = await GenerateEncodedTokenAsync(user);
        var inviteLink = BuildInviteLink(user, encodedToken);
        var displayName = GetDisplayName(user);
        var supportEmail = await GetSupportEmailAsync();

        return await emailService.SendWelcomeSetPasswordEmailAsync(
            user.Email,
            displayName,
            tenantName,
            inviteLink,
            expiresMinutes,
            user.TenantId,
            supportEmail);
    }

    private async Task<(string EncodedToken, int ExpiresMinutes)> GenerateEncodedTokenAsync(ApplicationUser user)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var expiresMinutes = (int)Math.Round(tokenOptions.Value.TokenLifespan.TotalMinutes);
        return (encodedToken, expiresMinutes);
    }

    private string BuildPasswordResetLink(ApplicationUser user, string encodedToken) =>
        navigationManager.GetUriWithQueryParameters(
            navigationManager.ToAbsoluteUri("passwort-zuruecksetzen").AbsoluteUri,
            new Dictionary<string, object?>
            {
                ["email"] = user.Email,
                ["token"] = encodedToken
            });

    private string BuildInviteLink(ApplicationUser user, string encodedToken) =>
        navigationManager.GetUriWithQueryParameters(
            navigationManager.ToAbsoluteUri("passwort-zuruecksetzen").AbsoluteUri,
            new Dictionary<string, object?>
            {
                ["userId"] = user.Id,
                ["token"] = encodedToken,
                ["mode"] = "invite"
            });

    private async Task<string> ResolveTenantNameAsync(ApplicationUser user)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var tenantIds = await db.UserTenants
            .IgnoreQueryFilters()
            .Where(ut => ut.UserId == user.Id)
            .Select(ut => ut.TenantId)
            .ToListAsync();

        if (tenantIds.Count == 0 && user.TenantId is int legacyId)
        {
            tenantIds.Add(legacyId);
        }

        if (tenantIds.Count == 0)
        {
            return "Ihr Mandant";
        }

        var names = await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.Id))
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .ToListAsync();

        return names.FirstOrDefault() ?? "Ihr Mandant";
    }

    private static string GetDisplayName(ApplicationUser user) =>
        string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email ?? "" : user.DisplayName;

    private async Task<string> GetSupportEmailAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var settings = await db.EmailSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync();
        return settings?.SenderEmail ?? string.Empty;
    }

    private async Task<bool> IsRateLimitedAsync(string purpose, string email)
    {
        var key = CacheKey(purpose, email);
        var value = await cache.GetStringAsync(key);
        return value is not null;
    }

    private Task MarkRateLimitedAsync(string purpose, string email) =>
        cache.SetStringAsync(
            CacheKey(purpose, email),
            DateTime.UtcNow.ToString("O"),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = SelfServiceCooldown });

    private static string CacheKey(string purpose, string email) =>
        $"dsms:{purpose}:{email.Trim().ToLowerInvariant()}";

    private static PasswordResetAdminResult FailAdmin(string message) =>
        new() { Succeeded = false, Message = message };

    private static PasswordResetChangeResult InvalidLinkResult(bool isInviteMode) =>
        new()
        {
            Succeeded = false,
            Message = isInviteMode
                ? "Der Link ist ungültig oder abgelaufen. Bitte wende dich an deinen Administrator, um eine neue Einladung zu erhalten."
                : "Der Link ist ungültig oder abgelaufen. Bitte fordere einen neuen Passwortreset an."
        };

    private static string MapIdentityError(IdentityError error) => error.Code switch
    {
        "PasswordTooShort" => "Das Passwort muss mindestens 8 Zeichen lang sein.",
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
