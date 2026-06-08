namespace Dsms.Web.Services.Licenses;

/// <summary>
/// Hilfslogik für Limit-Anzeige in der UI. null = unbegrenzt.
/// Per-Tenant-Limits werden nur über Einzelmandanten-Werte bewertet, nicht über Gesamtcounts.
/// </summary>
public static class LicenseLimitHelper
{
    public const int WarningThresholdPercent = 80;

    public static LicenseLimitUsageItemDto CreateItem(string name, int current, int? limit)
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
            IsExceeded = isExceeded
        };
    }

    public static TenantLimitUsageDto BuildTenantLimitUsage(
        TenantUsageDto tenant,
        LicenseDetailsDto license)
    {
        return new TenantLimitUsageDto
        {
            TenantId = tenant.TenantId,
            TenantName = tenant.TenantName,
            Items =
            [
                CreateItem("Benutzer", tenant.CurrentUsers, license.MaxUsersPerTenant),
                CreateItem("Auditoren", tenant.CurrentAuditors, license.MaxAuditorsPerTenant),
                CreateItem("Eigene Auditvorlagen", tenant.CurrentCustomAuditTemplates, license.MaxCustomAuditTemplatesPerTenant),
                CreateItem("Laufende Audits", tenant.CurrentActiveAudits, license.MaxActiveAuditsPerTenant),
                CreateItem("Verarbeitungstätigkeiten", tenant.CurrentProcessingActivities, license.MaxProcessingActivitiesPerTenant),
                CreateItem("DSFA", tenant.CurrentDpia, license.MaxDpiaPerTenant),
                CreateItem("TOMs", tenant.CurrentToms, license.MaxTomsPerTenant),
                CreateItem("Dienstleister", tenant.CurrentProcessors, license.MaxProcessorsPerTenant),
                CreateItem("Laufende Maßnahmen", tenant.CurrentActiveMeasures, license.MaxActiveMeasuresPerTenant)
            ]
        };
    }

    public static string FormatPerTenantLimit(int? limit) =>
        limit.HasValue ? $"{limit} je Mandant" : "unbegrenzt je Mandant";

    public static string FormatTotalWithPerTenantLimit(int total, int? perTenantLimit) =>
        $"{total} gesamt / {FormatPerTenantLimit(perTenantLimit)}";
}
