using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Data.Seed;

/// <summary>
/// Legt Standard-Tarifvorlagen idempotent an (Erkennung über technischen <see cref="SubscriptionPlan.Name"/>).
/// Die Werte sind Demo-/Startwerte und können später angepasst werden.
/// </summary>
public static class SubscriptionPlanSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await SeedPlanIfMissingAsync(db, CreateFreePlan());
        await SeedPlanIfMissingAsync(db, CreateBasicPlan());
        await SeedPlanIfMissingAsync(db, CreateProPlan());
        await SeedPlanIfMissingAsync(db, CreateBusinessPlan());
    }

    private static async Task SeedPlanIfMissingAsync(ApplicationDbContext db, SubscriptionPlan plan)
    {
        if (await db.SubscriptionPlans.AnyAsync(p => p.Name == plan.Name))
        {
            return;
        }

        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync();
    }

    private static SubscriptionPlan CreateFreePlan() => new()
    {
        Id = Guid.NewGuid(),
        Name = "free",
        DisplayName = "Free",
        Description = "Kostenloser Einstieg zum Testen des DSMS.",
        IsActive = true,
        IsFree = true,
        SortOrder = 10,
        PriceMonthly = 0m,
        PriceYearly = 0m,
        Currency = "EUR",
        CreatedAt = DateTime.UtcNow,
        MaxTenants = 1,
        MaxAdmins = 1,
        MaxUsersPerTenant = 2,
        MaxAuditorsPerTenant = 1,
        MaxCustomAuditTemplatesPerTenant = 1,
        MaxActiveAuditsPerTenant = 1,
        MaxProcessingActivitiesPerTenant = 5,
        MaxDpiaPerTenant = 2,
        MaxTomsPerTenant = 5,
        MaxProcessorsPerTenant = 5,
        MaxActiveMeasuresPerTenant = 5,
        MaxStorageMb = 100,
        MaxEmailRemindersPerMonth = 10
    };

    private static SubscriptionPlan CreateBasicPlan() => new()
    {
        Id = Guid.NewGuid(),
        Name = "basic",
        DisplayName = "Basic",
        IsActive = true,
        IsFree = false,
        SortOrder = 20,
        PriceMonthly = null,
        PriceYearly = null,
        Currency = "EUR",
        CreatedAt = DateTime.UtcNow,
        MaxTenants = 1,
        MaxAdmins = 1,
        MaxUsersPerTenant = 5,
        MaxAuditorsPerTenant = 1,
        MaxCustomAuditTemplatesPerTenant = 2,
        MaxActiveAuditsPerTenant = 2,
        MaxProcessingActivitiesPerTenant = 15,
        MaxDpiaPerTenant = 5,
        MaxTomsPerTenant = 15,
        MaxProcessorsPerTenant = 10,
        MaxActiveMeasuresPerTenant = 10,
        MaxStorageMb = 250,
        MaxEmailRemindersPerMonth = 50
    };

    private static SubscriptionPlan CreateProPlan() => new()
    {
        Id = Guid.NewGuid(),
        Name = "pro",
        DisplayName = "Pro",
        IsActive = true,
        IsFree = false,
        SortOrder = 30,
        Currency = "EUR",
        CreatedAt = DateTime.UtcNow,
        MaxTenants = 5,
        MaxAdmins = 3,
        MaxUsersPerTenant = 25,
        MaxAuditorsPerTenant = 3,
        MaxCustomAuditTemplatesPerTenant = 10,
        MaxActiveAuditsPerTenant = 5,
        MaxProcessingActivitiesPerTenant = 50,
        MaxDpiaPerTenant = 20,
        MaxTomsPerTenant = 50,
        MaxProcessorsPerTenant = 50,
        MaxActiveMeasuresPerTenant = 50,
        MaxStorageMb = 1000,
        MaxEmailRemindersPerMonth = 250
    };

    private static SubscriptionPlan CreateBusinessPlan() => new()
    {
        Id = Guid.NewGuid(),
        Name = "business",
        DisplayName = "Business",
        IsActive = true,
        IsFree = false,
        SortOrder = 40,
        Currency = "EUR",
        CreatedAt = DateTime.UtcNow,
        MaxTenants = 25,
        MaxAdmins = 10,
        MaxUsersPerTenant = 100,
        MaxAuditorsPerTenant = 10,
        MaxCustomAuditTemplatesPerTenant = 50,
        MaxActiveAuditsPerTenant = 20,
        MaxProcessingActivitiesPerTenant = null,
        MaxDpiaPerTenant = null,
        MaxTomsPerTenant = null,
        MaxProcessorsPerTenant = null,
        MaxActiveMeasuresPerTenant = null,
        MaxStorageMb = 5000,
        MaxEmailRemindersPerMonth = 1000
    };
}
