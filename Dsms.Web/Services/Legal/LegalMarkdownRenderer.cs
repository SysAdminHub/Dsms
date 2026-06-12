using Markdig;

namespace Dsms.Web.Services.Legal;

internal static class LegalMarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public static string ToHtml(string markdown) =>
        Markdown.ToHtml(markdown, Pipeline);
}
