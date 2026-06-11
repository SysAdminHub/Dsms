namespace Dsms.Web.Services.PageHelp;

public interface IPageHelpContentService
{
    Task<PageHelpContentDto> GetByKeyAsync(string key);
    Task EnsureDefaultsAsync();
    Task<PageHelpOperationResult> UpdateAsync(string key, PageHelpContentUpdateModel model);
}
