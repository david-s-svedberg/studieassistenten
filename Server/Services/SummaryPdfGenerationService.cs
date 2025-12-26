using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudieAssistenten.Server.Services.Pdf;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services;

public interface ISummaryPdfGenerationService
{
    byte[] GenerateSummaryPdf(GeneratedContent content);
}

public class SummaryPdfGenerationService : BasePdfGenerationService, ISummaryPdfGenerationService
{
    private string? _documentTitle;

    public byte[] GenerateSummaryPdf(GeneratedContent content)
    {
        // Get test name from content.Test (generated content now belongs to test)
        _documentTitle = content.Test?.Name ?? content.StudyDocument?.Test?.Name ?? content.Title ?? "Sammanfattning";
        return GeneratePdf(container => ComposeContent(container, content));
    }

    protected override string GetDocumentTitle()
    {
        return _documentTitle ?? "Sammanfattning";
    }

    protected override void ComposeHeader(IContainer container)
    {
        // Override to remove the placeholder image and add padding
        container.PaddingBottom(15).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(GetDocumentTitle()).FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                column.Item().Text($"Genererad: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
            });
            // Removed: row.ConstantItem(100).Height(50).Placeholder();
        });
    }

    protected override void ConfigurePage(PageDescriptor page)
    {
        base.ConfigurePage(page);
        // Add custom line height for summaries
        page.DefaultTextStyle(x => x.FontSize(12).LineHeight(1.5f));
    }

    void ComposeContent(IContainer container, GeneratedContent content)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            // Title
            if (!string.IsNullOrWhiteSpace(content.Title))
            {
                column.Item().Text(content.Title).FontSize(16).Bold();
            }

            // Summary content with markdown-style formatting
            if (!string.IsNullOrWhiteSpace(content.Content))
            {
                var lines = content.Content.Split('\n');

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    if (string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        column.Item().PaddingTop(5);
                        continue;
                    }

                    // Headers (###, ##, or #)
                    if (trimmedLine.StartsWith("### "))
                    {
                        column.Item().PaddingTop(8).Text(trimmedLine.Replace("### ", ""))
                            .FontSize(13).Bold().FontColor(Colors.Blue.Darken1);
                    }
                    else if (trimmedLine.StartsWith("## "))
                    {
                        column.Item().PaddingTop(10).Text(trimmedLine.Replace("## ", ""))
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                    }
                    else if (trimmedLine.StartsWith("# "))
                    {
                        column.Item().PaddingTop(10).Text(trimmedLine.Replace("# ", ""))
                            .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    }
                    // Bullet points
                    else if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "))
                    {
                        column.Item().Row(row =>
                        {
                            row.ConstantItem(20).Text("•");
                            row.RelativeItem().Text(text => RenderMarkdownText(text, trimmedLine.Substring(2).Trim()));
                        });
                    }
                    // Numbered lists
                    else if (System.Text.RegularExpressions.Regex.IsMatch(trimmedLine, @"^\d+\.\s"))
                    {
                        column.Item().PaddingLeft(10).Text(text => RenderMarkdownText(text, trimmedLine));
                    }
                    // All other text (with potential markdown)
                    else
                    {
                        column.Item().Text(text => RenderMarkdownText(text, trimmedLine));
                    }
                }
            }
        });
    }

    /// <summary>
    /// Renders text with markdown formatting (bold, italic)
    /// </summary>
    void RenderMarkdownText(TextSpanDescriptor text, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        // Handle bold (**text**) and italic (*text* or _text_)
        int i = 0;
        while (i < content.Length)
        {
            // Check for bold (**text**)
            if (i < content.Length - 1 && content[i] == '*' && content[i + 1] == '*')
            {
                int endIndex = content.IndexOf("**", i + 2);
                if (endIndex > i + 2)
                {
                    // Found bold text
                    text.Span(content.Substring(i + 2, endIndex - i - 2)).Bold();
                    i = endIndex + 2;
                    continue;
                }
            }
            // Check for italic (*text* or _text_)
            else if (content[i] == '*' || content[i] == '_')
            {
                char marker = content[i];
                int endIndex = content.IndexOf(marker, i + 1);
                if (endIndex > i + 1)
                {
                    // Found italic text
                    text.Span(content.Substring(i + 1, endIndex - i - 1)).Italic();
                    i = endIndex + 1;
                    continue;
                }
            }

            // Regular character - find next markdown marker or end
            int nextMarker = content.Length;
            int nextBold = content.IndexOf("**", i);
            int nextItalicStar = content.IndexOf("*", i);
            int nextItalicUnderscore = content.IndexOf("_", i);

            if (nextBold >= 0) nextMarker = Math.Min(nextMarker, nextBold);
            if (nextItalicStar >= 0 && nextItalicStar != nextBold) nextMarker = Math.Min(nextMarker, nextItalicStar);
            if (nextItalicUnderscore >= 0) nextMarker = Math.Min(nextMarker, nextItalicUnderscore);

            text.Span(content.Substring(i, nextMarker - i));
            i = nextMarker;
        }
    }
}
