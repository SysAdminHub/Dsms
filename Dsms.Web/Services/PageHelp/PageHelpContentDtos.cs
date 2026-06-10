namespace Dsms.Web.Services.PageHelp;

public sealed class PageHelpContentDto
{
    public int Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? LegalReference { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public bool IsFallback { get; init; }
}

public sealed class PageHelpContentUpdateModel
{
    public string Title { get; set; } = string.Empty;
    public string? LegalReference { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
}

public sealed class PageHelpOperationResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }

    public static PageHelpOperationResult Ok(string? message = null) =>
        new() { Succeeded = true, Message = message };

    public static PageHelpOperationResult Fail(string message) =>
        new() { Succeeded = false, Message = message };
}
