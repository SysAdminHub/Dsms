using Dsms.Web.Data;
using Dsms.Web.Data.Seed;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.PageHelp;

public sealed class PageHelpContentService(
    ApplicationDbContext db,
    IUserAccessService access,
    ICurrentUserContext currentUser) : IPageHelpContentService
{
    private const string FallbackMessage = "Für diese Seite ist noch kein Hilfetext hinterlegt.";

    public async Task<PageHelpContentDto> GetByKeyAsync(string key)
    {
        if (!PageHelpContentKeys.IsValid(key))
        {
            return CreateFallbackDto(key);
        }

        await EnsureDefaultsAsync();

        var content = await db.PageHelpContents
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Key == key && h.IsActive);

        if (content is null)
        {
            return CreateFallbackDto(key);
        }

        return MapToDto(content);
    }

    public async Task EnsureDefaultsAsync()
    {
        await PageHelpContentSeeder.EnsureMissingDefaultsAsync(db);
    }

    public async Task<PageHelpOperationResult> UpdateAsync(string key, PageHelpContentUpdateModel model)
    {
        await EnsureSuperuserAsync();

        if (!PageHelpContentKeys.IsValid(key))
        {
            return PageHelpOperationResult.Fail("Ungültiger Hilfetext-Schlüssel.");
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return PageHelpOperationResult.Fail("Titel ist erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(model.Content))
        {
            return PageHelpOperationResult.Fail("Inhalt ist erforderlich.");
        }

        await EnsureDefaultsAsync();

        var content = await db.PageHelpContents.FirstOrDefaultAsync(h => h.Key == key);
        if (content is null)
        {
            return PageHelpOperationResult.Fail("Hilfetext wurde nicht gefunden.");
        }

        content.Title = model.Title.Trim();
        content.LegalReference = string.IsNullOrWhiteSpace(model.LegalReference)
            ? null
            : model.LegalReference.Trim();
        content.ShortDescription = string.IsNullOrWhiteSpace(model.ShortDescription)
            ? null
            : model.ShortDescription.Trim();
        content.Content = model.Content.Trim();
        content.IsActive = true;
        content.UpdatedAt = DateTime.UtcNow;
        content.UpdatedByUserId = await currentUser.GetUserIdAsync();

        await db.SaveChangesAsync();
        return PageHelpOperationResult.Ok("Hilfetext gespeichert.");
    }

    private static PageHelpContentDto MapToDto(PageHelpContent content) => new()
    {
        Id = content.Id,
        Key = content.Key,
        Title = content.Title,
        LegalReference = content.LegalReference,
        Content = content.Content,
        ShortDescription = content.ShortDescription,
        IsFallback = false
    };

    private static PageHelpContentDto CreateFallbackDto(string key) => new()
    {
        Key = key,
        Title = "Seitenhilfe",
        Content = FallbackMessage,
        IsFallback = true
    };

    private async Task EnsureSuperuserAsync()
    {
        if (!await access.IsSuperuserAsync())
        {
            throw new UnauthorizedAccessException("Nur Superuser dürfen Hilfetexte bearbeiten.");
        }
    }
}
