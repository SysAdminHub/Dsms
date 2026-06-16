namespace Dsms.Web.Services.Email;

/// <summary>Ersetzt Platzhalter im Format {{VariableName}} in Email-Vorlagen.</summary>
public interface IEmailTemplateRenderer
{
    RenderedEmail Render(string subject, string htmlContent, string? textContent, IReadOnlyDictionary<string, string> variables);
}
