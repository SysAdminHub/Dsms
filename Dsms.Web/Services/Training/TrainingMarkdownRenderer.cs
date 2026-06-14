using Markdig;

namespace Dsms.Web.Services.Training;

/// <summary>Sicheres Markdown-Rendering für Schulungsinhalte (HTML deaktiviert).</summary>
public static class TrainingMarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public static string ToHtml(string markdown) =>
        string.IsNullOrWhiteSpace(markdown) ? string.Empty : Markdown.ToHtml(markdown, Pipeline);
}
