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
                int i = 0;

                while (i < lines.Length)
                {
                    var trimmedLine = lines[i].Trim();

                    if (string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        column.Item().PaddingTop(5);
                        i++;
                        continue;
                    }

                    // Check if this is a table (line contains | and next line is separator)
                    if (trimmedLine.Contains("|") && i + 1 < lines.Length &&
                        lines[i + 1].Trim().StartsWith("|") && lines[i + 1].Contains("---"))
                    {
                        // Parse and render table
                        var tableLines = new List<string> { trimmedLine, lines[i + 1] };
                        i += 2;

                        // Collect all table rows
                        while (i < lines.Length && lines[i].Trim().StartsWith("|"))
                        {
                            tableLines.Add(lines[i].Trim());
                            i++;
                        }

                        column.Item().Element(container => RenderMarkdownTable(container, tableLines));
                        continue;
                    }

                    var line = trimmedLine;

                    // Headers (###, ##, or #)
                    if (line.StartsWith("### "))
                    {
                        column.Item().PaddingTop(8).Text(line.Replace("### ", ""))
                            .FontSize(13).Bold().FontColor(Colors.Blue.Darken1);
                    }
                    else if (line.StartsWith("## "))
                    {
                        column.Item().PaddingTop(10).Text(line.Replace("## ", ""))
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                    }
                    else if (line.StartsWith("# "))
                    {
                        column.Item().PaddingTop(10).Text(line.Replace("# ", ""))
                            .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    }
                    // Bullet points
                    else if (line.StartsWith("- ") || line.StartsWith("* "))
                    {
                        column.Item().Row(row =>
                        {
                            row.ConstantItem(20).Text("•");
                            row.RelativeItem().Text(text => RenderMarkdownText(text, line.Substring(2).Trim()));
                        });
                    }
                    // Numbered lists
                    else if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^\d+\.\s"))
                    {
                        column.Item().PaddingLeft(10).Text(text => RenderMarkdownText(text, line));
                    }
                    // All other text (with potential markdown)
                    else
                    {
                        column.Item().Text(text => RenderMarkdownText(text, line));
                    }

                    i++;
                }
            }
        });
    }

    /// <summary>
    /// Renders a markdown table as a proper PDF table
    /// </summary>
    void RenderMarkdownTable(IContainer container, List<string> tableLines)
    {
        if (tableLines.Count < 3) return; // Need at least header, separator, and one data row

        // Parse table structure
        var headerRow = tableLines[0].Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim()).ToArray();
        var dataRows = new List<string[]>();

        // Skip separator row (index 1) and parse data rows
        for (int i = 2; i < tableLines.Count; i++)
        {
            var cells = tableLines[i].Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim()).ToArray();
            if (cells.Length > 0)
                dataRows.Add(cells);
        }

        // Render table
        container.Padding(5).Table(table =>
        {
            // Define columns
            table.ColumnsDefinition(columns =>
            {
                for (int i = 0; i < headerRow.Length; i++)
                {
                    columns.RelativeColumn();
                }
            });

            // Header row
            table.Header(header =>
            {
                foreach (var cell in headerRow)
                {
                    header.Cell().Border(1).BorderColor(Colors.Grey.Medium)
                        .Background(Colors.Grey.Lighten3)
                        .Padding(5)
                        .Text(cell).FontSize(10).Bold();
                }
            });

            // Data rows
            foreach (var row in dataRows)
            {
                for (int i = 0; i < row.Length && i < headerRow.Length; i++)
                {
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1)
                        .Padding(5)
                        .DefaultTextStyle(x => x.FontSize(10))
                        .Text(text => RenderMarkdownText(text, row[i]));
                }
            }
        });
    }

    /// <summary>
    /// Renders text with markdown formatting (bold, italic)
    /// </summary>
    void RenderMarkdownText(TextDescriptor text, string content)
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
