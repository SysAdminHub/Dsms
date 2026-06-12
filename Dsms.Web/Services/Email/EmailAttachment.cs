namespace Dsms.Web.Services.Email;

public sealed class EmailAttachment
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required byte[] ContentBytes { get; init; }
}
