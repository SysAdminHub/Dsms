using System.Text.RegularExpressions;

namespace Dsms.Web.Services.Email;

/// <summary>Einfache Platzhalterersetzung für Email-Vorlagen.</summary>
public sealed partial class EmailTemplateRenderer : IEmailTemplateRenderer
{
    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    public RenderedEmail Render(
        string subject,
        string htmlContent,
        string? textContent,
        IReadOnlyDictionary<string, string> variables)
    {
        return new RenderedEmail
        {
            Subject = ReplacePlaceholders(subject, variables),
            HtmlBody = ReplacePlaceholders(htmlContent, variables),
            TextBody = textContent is null ? null : ReplacePlaceholders(textContent, variables)
        };
    }

    private static string ReplacePlaceholders(string input, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return PlaceholderRegex().Replace(input, match =>
        {
            var key = match.Groups[1].Value;
            return variables.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}
