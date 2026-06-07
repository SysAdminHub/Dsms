namespace Dsms.Web.Services.Email;

/// <summary>Gerenderte Email-Inhalte nach Platzhalterersetzung.</summary>
public sealed class RenderedEmail
{
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public string? TextBody { get; init; }
}
