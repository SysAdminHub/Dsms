using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Data.Seed;

/// <summary>Legt die Standard-Dienstleister-Arten pro Mandant idempotent an.</summary>
public static class ServiceProviderCategorySeeder
{
    public sealed record DefaultCategoryDefinition(
        string Name,
        string? Description,
        int SortOrder);

    /// <summary>
    /// Standard-Arten für Dienstleister. Reihenfolge und Namen entsprechen den bisher
    /// fest im Code (Enum <c>ServiceProviderType</c>) hinterlegten Werten.
    /// </summary>
    public static IReadOnlyList<DefaultCategoryDefinition> DefaultDefinitions { get; } =
    [
        new("Hosting", "Hosting- und Rechenzentrumsleistungen", 10),
        new("Cloud-Dienst", "Cloud- und SaaS-Dienste", 20),
        new("IT-Support", "IT-Betreuung und Support", 30),
        new("Softwareanbieter", "Anbieter von Softwarelösungen", 40),
        new("Lohnabrechnung", "Lohn- und Gehaltsabrechnung", 50),
        new("Buchhaltung", "Buchhaltung und Finanzwesen", 60),
        new("Newsletter", "Newsletter- und E-Mail-Versand", 70),
        new("CRM", "Kundenbeziehungsmanagement", 80),
        new("Aktenvernichtung", "Akten- und Datenträgervernichtung", 90),
        new("Wartung", "Wartung und technischer Service", 100),
        new("Beratung", "Beratungsleistungen", 110),
        new("Sonstige", "Weitere Dienstleister-Arten", 120)
    ];

    public static async Task EnsureDefaultCategoriesAsync(
        ApplicationDbContext db,
        int tenantId,
        string? userId = null,
        CancellationToken ct = default)
    {
        var existingNames = await db.ServiceProviderCategories
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId)
            .Select(c => c.Name)
            .ToListAsync(ct);

        var existingSet = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var added = false;

        foreach (var definition in DefaultDefinitions)
        {
            if (existingSet.Contains(definition.Name))
            {
                continue;
            }

            db.ServiceProviderCategories.Add(new ServiceProviderCategory
            {
                TenantId = tenantId,
                Name = definition.Name,
                Description = definition.Description,
                SortOrder = definition.SortOrder,
                IsActive = true,
                IsSystemDefault = true,
                CreatedAt = now,
                CreatedByUserId = userId
            });
            added = true;
        }

        if (added)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    public static async Task<ServiceProviderCategory?> FindDefaultCategoryAsync(
        ApplicationDbContext db,
        int tenantId,
        string name,
        CancellationToken ct = default) =>
        await db.ServiceProviderCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Name == name, ct);
}
