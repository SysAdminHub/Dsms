using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Data.Seed;

/// <summary>Legt Standard-Dokumentkategorien pro Mandant idempotent an.</summary>
public static class DocumentCategorySeeder
{
    public sealed record DefaultCategoryDefinition(
        string Name,
        string? Description,
        string Color,
        int SortOrder);

    public static IReadOnlyList<DefaultCategoryDefinition> DefaultDefinitions { get; } =
    [
        new("Datenschutz", "Datenschutzbezogene Dokumente", DocumentCategoryColors.Blue, 10),
        new("IT-Sicherheit", "IT- und Informationssicherheit", DocumentCategoryColors.Cyan, 20),
        new("HR", "Personal- und HR-bezogene Dokumente", DocumentCategoryColors.Purple, 30),
        new("Lieferanten", "Dienstleister- und Lieferantenunterlagen", DocumentCategoryColors.Orange, 40),
        new("Betroffenenrechte", "Anfragen und Rechte betroffener Personen", DocumentCategoryColors.Green, 50),
        new("Datenschutzvorfälle", "Vorfälle und Meldungen", DocumentCategoryColors.Red, 60),
        new("Informationspflichten", "Informations- und Transparenzpflichten", DocumentCategoryColors.Blue, 70),
        new("Allgemein", "Allgemeine Dokumente", DocumentCategoryColors.Gray, 80)
    ];

    public static async Task EnsureDefaultCategoriesAsync(
        ApplicationDbContext db,
        int tenantId,
        string? userId = null,
        CancellationToken ct = default)
    {
        var existingNames = await db.DocumentCategories
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

            db.DocumentCategories.Add(new DocumentCategory
            {
                TenantId = tenantId,
                Name = definition.Name,
                Description = definition.Description,
                Color = definition.Color,
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

    public static async Task<DocumentCategory?> FindDefaultCategoryAsync(
        ApplicationDbContext db,
        int tenantId,
        string name,
        CancellationToken ct = default) =>
        await db.DocumentCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Name == name, ct);
}
