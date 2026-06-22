using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Data.Seed;

/// <summary>Legt die Standard-TOM-Kategorien pro Mandant idempotent an.</summary>
public static class TomCategorySeeder
{
    public sealed record DefaultCategoryDefinition(
        string Name,
        string? Description,
        int SortOrder);

    /// <summary>
    /// Standardkategorien für TOMs. Reihenfolge und Namen entsprechen den bisher
    /// fest im Code hinterlegten Kategorien.
    /// </summary>
    public static IReadOnlyList<DefaultCategoryDefinition> DefaultDefinitions { get; } =
    [
        new("Zutrittskontrolle", "Schutz vor unbefugtem Zutritt zu Räumen und Anlagen", 10),
        new("Zugangskontrolle", "Schutz vor unbefugter Systemnutzung", 20),
        new("Zugriffskontrolle", "Beschränkung der Datenzugriffe auf Berechtigte", 30),
        new("Weitergabekontrolle", "Schutz bei Transport und Übermittlung von Daten", 40),
        new("Eingabekontrolle", "Nachvollziehbarkeit von Eingabe, Änderung und Löschung", 50),
        new("Auftragskontrolle", "Weisungsgemäße Verarbeitung durch Auftragsverarbeiter", 60),
        new("Verfügbarkeitskontrolle", "Schutz vor Verlust und Sicherstellung der Verfügbarkeit", 70),
        new("Trennungsgebot", "Getrennte Verarbeitung zu unterschiedlichen Zwecken", 80),
        new("Verschlüsselung", "Schutz von Daten durch Verschlüsselungsverfahren", 90),
        new("Backup und Wiederherstellung", "Datensicherung und Wiederherstellbarkeit", 100),
        new("Protokollierung", "Protokollierung sicherheitsrelevanter Ereignisse", 110),
        new("Berechtigungskonzept", "Rollen- und Rechteverwaltung", 120),
        new("Schulung und Sensibilisierung", "Awareness und Schulung der Beschäftigten", 130),
        new("Sonstige", "Weitere technische und organisatorische Maßnahmen", 140)
    ];

    public static async Task EnsureDefaultCategoriesAsync(
        ApplicationDbContext db,
        int tenantId,
        string? userId = null,
        CancellationToken ct = default)
    {
        var existingNames = await db.TomCategories
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

            db.TomCategories.Add(new TomCategory
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

    public static async Task<TomCategory?> FindDefaultCategoryAsync(
        ApplicationDbContext db,
        int tenantId,
        string name,
        CancellationToken ct = default) =>
        await db.TomCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Name == name, ct);
}
