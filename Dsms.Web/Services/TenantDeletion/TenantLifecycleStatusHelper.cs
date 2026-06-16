using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Services.TenantDeletion;

public static class TenantLifecycleStatusHelper
{
    public static TenantLifecycleStatus GetStatus(Tenant tenant)
    {
        if (tenant.IsDeletionRequested)
        {
            return TenantLifecycleStatus.ZurLoeschungVorgemerkt;
        }

        return tenant.IsActive
            ? TenantLifecycleStatus.Aktiv
            : TenantLifecycleStatus.Deaktiviert;
    }

    public static string GetDisplayName(TenantLifecycleStatus status) => status switch
    {
        TenantLifecycleStatus.Aktiv => "Aktiv",
        TenantLifecycleStatus.Deaktiviert => "Deaktiviert",
        TenantLifecycleStatus.ZurLoeschungVorgemerkt => "Zur Löschung vorgemerkt",
        _ => status.ToString()
    };

    public static string GetBadgeVariant(TenantLifecycleStatus status) => status switch
    {
        TenantLifecycleStatus.Aktiv => "success",
        TenantLifecycleStatus.Deaktiviert => "default",
        TenantLifecycleStatus.ZurLoeschungVorgemerkt => "warning",
        _ => "default"
    };
}
