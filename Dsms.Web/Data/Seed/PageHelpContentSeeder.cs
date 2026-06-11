using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Data.Seed;

/// <summary>
/// Legt Standard-Hilfetexte für Fachseiten an, ohne bestehende angepasste Texte zu überschreiben.
/// </summary>
public static class PageHelpContentSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        foreach (var definition in PageHelpContentDefaults.All)
        {
            await SeedIfMissingAsync(db, definition);
        }
    }

    public static async Task EnsureMissingDefaultsAsync(ApplicationDbContext db)
    {
        await SeedAsync(db);
    }

    private static async Task SeedIfMissingAsync(ApplicationDbContext db, PageHelpContentDefaults.Definition definition)
    {
        if (await db.PageHelpContents.AnyAsync(h => h.Key == definition.Key))
        {
            return;
        }

        db.PageHelpContents.Add(new PageHelpContent
        {
            Key = definition.Key,
            Title = definition.Title,
            LegalReference = definition.LegalReference,
            ShortDescription = definition.ShortDescription,
            Content = definition.Content.Trim(),
            IsActive = true
        });
        await db.SaveChangesAsync();
    }
}
