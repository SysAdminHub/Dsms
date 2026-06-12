using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dsms.Web.Services.Legal;

internal static class LegalPdfMarkdownComposer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public static byte[] Compose(LegalPdfDocumentRequest request) =>
        Compose([request]);

    public static byte[] Compose(IReadOnlyList<LegalPdfDocumentRequest> sections)
    {
        if (sections.Count == 0)
        {
            throw new ArgumentException("At least one document section is required.", nameof(sections));
        }

        return Document.Create(document =>
        {
            foreach (var section in sections)
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginVertical(36);
                    page.MarginHorizontal(48);
                    page.DefaultTextStyle(x => x.FontSize(10).LineHeight(1.45f).FontColor(Colors.Grey.Darken3));

                    page.Header().Column(header =>
                    {
                        header.Item().Text(section.ProductName).SemiBold().FontSize(11).FontColor(Colors.Grey.Darken4);
                        header.Item().PaddingTop(4).Text(section.Title).Bold().FontSize(14);
                        header.Item().PaddingTop(6).Text(text =>
                        {
                            text.Span("Dokumentversion: ").SemiBold();
                            text.Span(section.Version);
                        });
                        if (!string.IsNullOrWhiteSpace(section.EffectiveDate))
                        {
                            header.Item().PaddingTop(2).Text(text =>
                            {
                                text.Span("Stand: ").SemiBold();
                                text.Span(section.EffectiveDate);
                            });
                        }

                        header.Item().PaddingTop(2).Text(text =>
                        {
                            text.Span("Anbieter: ").SemiBold();
                            text.Span(section.ProviderName);
                        });
                        header.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingTop(12).Column(column =>
                    {
                        var markdownDocument = Markdown.Parse(section.Markdown, Pipeline);
                        foreach (var block in markdownDocument)
                        {
                            RenderBlock(column, block);
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Medium));
                        text.Span("Seite ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            }
        }).GeneratePdf();
    }

    private static void RenderBlock(ColumnDescriptor column, MarkdownObject block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                column.Item().PaddingTop(HeadingTopPadding(heading.Level)).Text(text =>
                {
                    text.DefaultTextStyle(x => x.Bold().FontSize(HeadingFontSize(heading.Level)));
                    RenderInlines(text, heading.Inline);
                });
                break;

            case ParagraphBlock paragraph:
                column.Item().PaddingTop(4).Text(text => RenderInlines(text, paragraph.Inline));
                break;

            case ListBlock list:
                RenderList(column, list);
                break;

            case QuoteBlock quote:
                column.Item().PaddingTop(6).BorderLeft(2).BorderColor(Colors.Grey.Lighten1).PaddingLeft(8).Column(inner =>
                {
                    foreach (var quoteBlock in quote)
                    {
                        RenderBlock(inner, quoteBlock);
                    }
                });
                break;

            case ThematicBreakBlock:
                column.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                break;

            case FencedCodeBlock code:
                column.Item().PaddingTop(6).Background(Colors.Grey.Lighten4).Padding(8).Text(code.Lines.ToString())
                    .FontFamily(Fonts.CourierNew).FontSize(9);
                break;

            case CodeBlock codeBlock:
                column.Item().PaddingTop(6).Background(Colors.Grey.Lighten4).Padding(8).Text(codeBlock.Lines.ToString())
                    .FontFamily(Fonts.CourierNew).FontSize(9);
                break;

            case Table table:
                RenderTable(column, table);
                break;

            default:
                if (block is ContainerBlock container)
                {
                    foreach (var child in container)
                    {
                        RenderBlock(column, child);
                    }
                }

                break;
        }
    }

    private static void RenderList(ColumnDescriptor column, ListBlock list)
    {
        var index = 1;
        foreach (var item in list)
        {
            if (item is not ListItemBlock listItem)
            {
                continue;
            }

            var prefix = list.IsOrdered ? $"{index}." : "•";
            index++;

            column.Item().PaddingTop(3).Row(row =>
            {
                row.ConstantItem(16).Text(prefix);
                row.RelativeItem().Column(itemColumn =>
                {
                    foreach (var child in listItem)
                    {
                        if (child is ParagraphBlock paragraph)
                        {
                            itemColumn.Item().Text(text => RenderInlines(text, paragraph.Inline));
                        }
                        else
                        {
                            RenderBlock(itemColumn, child);
                        }
                    }
                });
            });
        }
    }

    private static void RenderTable(ColumnDescriptor column, Table table)
    {
        var rows = table.OfType<TableRow>().ToList();
        if (rows.Count == 0)
        {
            return;
        }

        var columnCount = rows.Max(row => row.OfType<TableCell>().Count());
        if (columnCount == 0)
        {
            return;
        }

        column.Item().PaddingTop(8).Table(tableDescriptor =>
        {
            tableDescriptor.ColumnsDefinition(columns =>
            {
                for (var i = 0; i < columnCount; i++)
                {
                    columns.RelativeColumn();
                }
            });

            foreach (var row in rows)
            {
                var isHeader = row.IsHeader;
                foreach (var cell in row.OfType<TableCell>())
                {
                    tableDescriptor.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text =>
                    {
                        if (isHeader)
                        {
                            text.DefaultTextStyle(x => x.SemiBold());
                        }

                        foreach (var cellBlock in cell)
                        {
                            if (cellBlock is ParagraphBlock paragraph)
                            {
                                RenderInlines(text, paragraph.Inline);
                            }
                        }
                    });
                }
            }
        });
    }

    private static void RenderInlines(TextDescriptor text, ContainerInline? inline)
    {
        if (inline is null)
        {
            return;
        }

        foreach (var child in inline)
        {
            switch (child)
            {
                case LiteralInline literal:
                    text.Span(literal.Content.ToString());
                    break;

                case EmphasisInline emphasis when emphasis.DelimiterCount >= 2:
                    text.Span(GetInlineText(emphasis)).Bold();
                    break;

                case EmphasisInline emphasis:
                    text.Span(GetInlineText(emphasis)).Italic();
                    break;

                case LineBreakInline:
                    text.Span("\n");
                    break;

                case LinkInline link:
                    var label = GetInlineText(link);
                    if (string.IsNullOrWhiteSpace(label))
                    {
                        label = link.Url ?? string.Empty;
                    }

                    text.Span(label).FontColor(Colors.Blue.Darken2);
                    if (!string.IsNullOrWhiteSpace(link.Url) && !string.Equals(label, link.Url, StringComparison.Ordinal))
                    {
                        text.Span($" ({link.Url})").FontSize(9).FontColor(Colors.Grey.Medium);
                    }

                    break;

                case CodeInline code:
                    text.Span(code.Content).FontFamily(Fonts.CourierNew).FontSize(9);
                    break;

                default:
                    if (child is ContainerInline container)
                    {
                        RenderInlines(text, container);
                    }

                    break;
            }
        }
    }

    private static string GetInlineText(ContainerInline inline)
    {
        if (inline.FirstChild is LiteralInline literal)
        {
            return literal.Content.ToString();
        }

        return inline.ToString() ?? string.Empty;
    }

    private static float HeadingFontSize(int level) => level switch
    {
        1 => 16,
        2 => 14,
        3 => 12,
        _ => 11
    };

    private static float HeadingTopPadding(int level) => level switch
    {
        1 => 0,
        2 => 10,
        _ => 8
    };
}
