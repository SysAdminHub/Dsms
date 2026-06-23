using System.Globalization;

namespace Dsms.Web.Services.Licenses;

/// <summary>
/// Hilfslogik für Limit-Anzeige in der UI. null = unbegrenzt.
/// Per-Tenant-Limits werden nur über Einzelmandanten-Werte bewertet, nicht über Gesamtcounts.
/// </summary>
public static class LicenseLimitHelper
{
    public const int WarningThresholdPercent = 80;

    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public const string PaidPlanUnlimitedObjectsHint =
        "Datenschutzobjekte sind im bezahlten Zugang unbegrenzt nutzbar.";

    public const string PaidPlanUnlimitedObjectsHintDetailed =
        "Verarbeitungstätigkeiten, DSFAs, Maßnahmen, TOMs und Dienstleister sind im bezahlten Zugang unbegrenzt enthalten.";

    /// <summary>
    /// Anzeigename des lizenzweiten Zugangslimits im bezahlten Zugang.
    /// Im neuen Preis-/Lizenzmodell werden Admins und Benutzer nicht mehr getrennt limitiert,
    /// sondern gemeinsam als "Zugänge" gezählt.
    /// </summary>
    public const string LicensedAccessLimitName = "Zugänge";

    public const string LicensedAccessLimitReachedMessage =
        "Die maximale Anzahl lizenzierter Zugänge ist erreicht. Bitte wenden Sie sich an Ihren Administrator oder den Support.";

    public static LicenseLimitUsageItemDto CreateItem(
        string name,
        int current,
        int? limit,
        bool isBusinessObjectLimit = false)
    {
        var isUnlimited = !limit.HasValue;
        int? percentage = null;
        var isExceeded = false;
        var isWarning = false;

        if (!isUnlimited)
        {
            if (limit!.Value > 0)
            {
                percentage = (int)Math.Round(100.0 * current / limit.Value);
            }

            isExceeded = current >= limit.Value;
            isWarning = !isExceeded && percentage >= WarningThresholdPercent;
        }

        return new LicenseLimitUsageItemDto
        {
            Name = name,
            CurrentValue = current,
            LimitValue = limit,
            IsUnlimited = isUnlimited,
            Percentage = percentage,
            IsWarning = isWarning,
            IsExceeded = isExceeded,
            IsBusinessObjectLimit = isBusinessObjectLimit
        };
    }

    public static TenantLimitUsageDto BuildTenantLimitUsage(
        TenantUsageDto tenant,
        LicenseDetailsDto license)
    {
        // Im bezahlten Zugang entfallen die fachlichen Objekt-Limits (Fair-Use-Modell);
        // Free-Zugänge nutzen weiterhin die konfigurierten Per-Mandant-Limits.
        var businessObjectsWaived = license.PaidPlanEnabled;
        int? BusinessLimit(int? configured) => businessObjectsWaived ? null : configured;

        var items = new List<LicenseLimitUsageItemDto>();

        // Im bezahlten Zugang werden Benutzer und Auditoren nicht mehr getrennt je Mandant
        // limitiert, sondern lizenzweit als "Zugänge" gezählt. Daher entfallen die
        // separaten Per-Mandant-Anzeigen für Benutzer/Auditoren; Free-Zugänge behalten sie.
        if (!license.PaidPlanEnabled)
        {
            items.Add(CreateItem("Benutzer", tenant.CurrentUsers, license.MaxUsersPerTenant));
            items.Add(CreateItem("Auditoren", tenant.CurrentAuditors, license.MaxAuditorsPerTenant));
        }

        items.Add(CreateItem("Eigene Auditvorlagen", tenant.CurrentCustomAuditTemplates, license.MaxCustomAuditTemplatesPerTenant));
        items.Add(CreateItem("Laufende Audits", tenant.CurrentActiveAudits, license.MaxActiveAuditsPerTenant));
        items.Add(CreateItem("Verarbeitungstätigkeiten", tenant.CurrentProcessingActivities, BusinessLimit(license.MaxProcessingActivitiesPerTenant), isBusinessObjectLimit: true));
        items.Add(CreateItem("DSFA", tenant.CurrentDpia, BusinessLimit(license.MaxDpiaPerTenant), isBusinessObjectLimit: true));
        items.Add(CreateItem("TOMs", tenant.CurrentToms, BusinessLimit(license.MaxTomsPerTenant), isBusinessObjectLimit: true));
        items.Add(CreateItem("Dienstleister", tenant.CurrentProcessors, BusinessLimit(license.MaxProcessorsPerTenant), isBusinessObjectLimit: true));
        items.Add(CreateItem("Laufende Maßnahmen", tenant.CurrentActiveMeasures, BusinessLimit(license.MaxActiveMeasuresPerTenant), isBusinessObjectLimit: true));

        return new TenantLimitUsageDto
        {
            TenantId = tenant.TenantId,
            TenantName = tenant.TenantName,
            Items = items
        };
    }

    /// <summary>
    /// Formatiert die Speichernutzung in GB, z. B. "2,4 GB von 20 GB verwendet".
    /// <paramref name="totalLimitGb"/> = null bedeutet unbegrenzt.
    /// </summary>
    public static string FormatStorageUsageGb(int currentMb, int? totalLimitGb)
    {
        var currentGb = (currentMb / 1024.0).ToString("0.0", GermanCulture);
        return totalLimitGb.HasValue
            ? $"{currentGb} GB von {totalLimitGb} GB verwendet"
            : $"{currentGb} GB verwendet · unbegrenzt";
    }

    public static string FormatPerTenantLimit(int? limit) =>
        limit.HasValue ? $"{limit} je Mandant" : "unbegrenzt je Mandant";

    public static string FormatTotalWithPerTenantLimit(int total, int? perTenantLimit) =>
        $"{total} gesamt / {FormatPerTenantLimit(perTenantLimit)}";

    public const string DefaultBlockedMessage =
        "Das Lizenzlimit für diesen Bereich wurde erreicht. Bitte wenden Sie sich an Ihren Administrator oder den Support.";

    public const string LicenseExpiredMessage =
        "Ihre Lizenz ist abgelaufen. Neue Einträge können derzeit nicht erstellt werden. Bitte wenden Sie sich an Ihren Administrator oder den Support.";

    public const string LicenseInactiveMessage =
        "Ihre Lizenz ist derzeit inaktiv. Neue Einträge können derzeit nicht erstellt werden. Bitte wenden Sie sich an Ihren Administrator oder den Support.";

    public const string LicenseSuspendedMessage =
        "Ihre Lizenz ist derzeit gesperrt. Neue Einträge können derzeit nicht erstellt werden. Bitte wenden Sie sich an Ihren Administrator oder den Support.";

    public const string LicenseNotFoundMessage =
        "Die zugeordnete Lizenz konnte nicht gefunden werden. Bitte wenden Sie sich an den Support.";

    public static string SpecificBlockedMessage(string area) =>
        $"Das Lizenzlimit für {area} wurde erreicht. Bitte wenden Sie sich an Ihren Administrator oder den Support.";

    public static DateTime GetTodayForLicenseCheck() => DateTime.UtcNow.Date;

    public static bool IsActiveStatus(string? status) =>
        string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);

    public static bool IsSuspendedStatus(string? status) =>
        string.Equals(status, "Suspended", StringComparison.OrdinalIgnoreCase);

    public static bool IsInactiveStatus(string? status) =>
        string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase);

    public static bool IsLicenseExpired(DateTime? validUntil, DateTime? referenceDate = null)
    {
        if (!validUntil.HasValue)
        {
            return false;
        }

        var today = (referenceDate ?? GetTodayForLicenseCheck()).Date;
        return validUntil.Value.Date < today;
    }

    public static LicenseUsabilityInfo EvaluateUsability(string? status, DateTime? validUntil, DateTime? referenceDate = null)
    {
        var today = (referenceDate ?? GetTodayForLicenseCheck()).Date;
        var isExpired = IsLicenseExpired(validUntil, today);
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim();

        if (IsSuspendedStatus(normalizedStatus))
        {
            return BuildUsabilityInfo(false, LicenseBlockReason.LicenseSuspended, normalizedStatus, validUntil, isExpired);
        }

        if (!IsActiveStatus(normalizedStatus))
        {
            return BuildUsabilityInfo(false, LicenseBlockReason.LicenseInactive, normalizedStatus, validUntil, isExpired);
        }

        if (isExpired)
        {
            return BuildUsabilityInfo(false, LicenseBlockReason.LicenseExpired, normalizedStatus, validUntil, true);
        }

        return BuildUsabilityInfo(true, LicenseBlockReason.None, normalizedStatus, validUntil, false);
    }

    public static LicenseLimitCheckResult? BuildUsabilityBlockResult(
        string? status,
        DateTime? validUntil,
        Guid? licenseId = null,
        int? tenantId = null,
        DateTime? referenceDate = null)
    {
        var usability = EvaluateUsability(status, validUntil, referenceDate);
        if (usability.IsUsableForCreation)
        {
            return null;
        }

        return new LicenseLimitCheckResult
        {
            IsAllowed = false,
            Message = GetBlockMessage(usability.BlockReason),
            LimitName = "Lizenz",
            LicenseId = licenseId,
            TenantId = tenantId,
            LicenseStatus = status,
            ValidUntil = validUntil,
            BlockReason = usability.BlockReason,
            IsExceeded = true
        };
    }

    public static string GetBlockMessage(LicenseBlockReason reason) => reason switch
    {
        LicenseBlockReason.LicenseExpired => LicenseExpiredMessage,
        LicenseBlockReason.LicenseInactive => LicenseInactiveMessage,
        LicenseBlockReason.LicenseSuspended => LicenseSuspendedMessage,
        LicenseBlockReason.LicenseNotFound => LicenseNotFoundMessage,
        LicenseBlockReason.LicenseNotAssigned => NoLicenseAssigned().Message,
        LicenseBlockReason.LimitReached => DefaultBlockedMessage,
        _ => DefaultBlockedMessage
    };

    public static string FormatStatusLabel(string? status) => status switch
    {
        "Active" => "Aktiv",
        "Suspended" => "Gesperrt",
        "Inactive" => "Inaktiv",
        _ => string.IsNullOrWhiteSpace(status) ? "Unbekannt" : status
    };

    public static string GetSuperuserUsabilityLabel(LicenseUsabilityInfo usability) =>
        usability.IsUsableForCreation
            ? "Nutzbar: Ja"
            : $"Nutzbar: Nein · {GetSuperuserBlockReasonLabel(usability.BlockReason)}";

    private static string GetSuperuserBlockReasonLabel(LicenseBlockReason reason) => reason switch
    {
        LicenseBlockReason.LicenseExpired => "Abgelaufen",
        LicenseBlockReason.LicenseInactive => "Inaktiv",
        LicenseBlockReason.LicenseSuspended => "Gesperrt",
        LicenseBlockReason.LicenseNotFound => "Nicht gefunden",
        LicenseBlockReason.LicenseNotAssigned => "Keine Lizenz",
        _ => "Nicht nutzbar"
    };

    private static LicenseUsabilityInfo BuildUsabilityInfo(
        bool isUsable,
        LicenseBlockReason blockReason,
        string status,
        DateTime? validUntil,
        bool isExpired) => new()
    {
        IsUsableForCreation = isUsable,
        BlockReason = blockReason,
        Status = status,
        ValidUntil = validUntil,
        IsExpired = isExpired,
        SuperuserDisplayLabel = isUsable
            ? "Nutzbar: Ja"
            : $"Nutzbar: Nein · {GetSuperuserBlockReasonLabel(blockReason)}",
        AdminHintMessage = isUsable ? null : GetAdminUsabilityHint(blockReason, isExpired)
    };

    private static string? GetAdminUsabilityHint(LicenseBlockReason blockReason, bool isExpired) => blockReason switch
    {
        LicenseBlockReason.LicenseExpired => "Diese Lizenz ist abgelaufen. Neue Einträge können nicht erstellt werden.",
        LicenseBlockReason.LicenseInactive => "Diese Lizenz ist derzeit inaktiv. Neue Einträge können nicht erstellt werden.",
        LicenseBlockReason.LicenseSuspended => "Diese Lizenz ist derzeit gesperrt. Neue Einträge können nicht erstellt werden.",
        _ when isExpired => "Diese Lizenz ist abgelaufen. Neue Einträge können nicht erstellt werden.",
        _ => null
    };

    public static LicenseLimitCheckResult BuildCheckResult(
        string limitName,
        int current,
        int? limit,
        string blockedMessage,
        Guid? licenseId = null,
        int? tenantId = null)
    {
        var item = CreateItem(limitName, current, limit);
        var isAllowed = item.IsUnlimited || (limit.HasValue && current < limit.Value);

        return new LicenseLimitCheckResult
        {
            IsAllowed = isAllowed,
            Message = isAllowed ? string.Empty : blockedMessage,
            LimitName = limitName,
            CurrentValue = current,
            LimitValue = limit,
            IsUnlimited = item.IsUnlimited,
            IsWarning = item.IsWarning,
            IsExceeded = item.IsExceeded,
            LicenseId = licenseId,
            TenantId = tenantId,
            BlockReason = isAllowed ? LicenseBlockReason.None : LicenseBlockReason.LimitReached
        };
    }

    public static LicenseLimitCheckResult NoLicenseAssigned(int? tenantId = null) => new()
    {
        IsAllowed = false,
        Message = "Für diesen Mandanten ist keine Lizenz zugeordnet. Bitte wenden Sie sich an den Support.",
        LimitName = "Lizenz",
        TenantId = tenantId,
        BlockReason = LicenseBlockReason.LicenseNotAssigned
    };

    public static LicenseLimitCheckResult LicenseNotFound(int? tenantId = null) => new()
    {
        IsAllowed = false,
        Message = LicenseNotFoundMessage,
        LimitName = "Lizenz",
        TenantId = tenantId,
        BlockReason = LicenseBlockReason.LicenseNotFound
    };

    public static LicenseLimitCheckResult NoTenantContext() => new()
    {
        IsAllowed = false,
        Message = "Bitte wählen Sie zuerst einen Mandanten aus.",
        LimitName = "Mandant"
    };

    public static string FormatUsageBadgeText(LicenseLimitCheckResult check)
    {
        if (check.IsUnlimited)
        {
            return $"{check.LimitName}: {check.CurrentValue} genutzt · unbegrenzt";
        }

        var baseText = $"{check.LimitName}: {check.CurrentValue} von {check.LimitValue} genutzt";
        if (check.IsExceeded)
        {
            return check.CurrentValue > check.LimitValue
                ? $"{baseText} · Limit überschritten"
                : $"{baseText} · Limit erreicht";
        }

        return baseText;
    }

    public static string FormatUsageItemBadgeText(LicenseLimitUsageItemDto item)
    {
        if (item.IsUnlimited)
        {
            return $"{item.Name}: {item.CurrentValue} genutzt · unbegrenzt";
        }

        var baseText = $"{item.Name}: {item.CurrentValue} von {item.LimitValue} genutzt";
        if (item.IsExceeded)
        {
            return item.CurrentValue > item.LimitValue
                ? $"{baseText} · Limit überschritten"
                : $"{baseText} · Limit erreicht";
        }

        return baseText;
    }
}
