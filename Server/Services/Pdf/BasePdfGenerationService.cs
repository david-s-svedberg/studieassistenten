using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace StudieAssistenten.Server.Services.Pdf;

/// <summary>
/// Base class for PDF generation services using QuestPDF.
/// Provides common page setup, header, and footer functionality.
/// </summary>
public abstract class BasePdfGenerationService
{
    protected BasePdfGenerationService()
    {
        // Set QuestPDF license (Community license is free for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Generates a PDF document with the provided content composition.
    /// </summary>
    protected byte[] GeneratePdf(Action<IContainer> composeContent)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);
                page.Header().Element(ComposeHeader);
                page.Content().Element(composeContent);
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Configures the page size, margins, and default text style.
    /// Override to customize page configuration.
    /// </summary>
    protected virtual void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(1, Unit.Centimetre);
        page.DefaultTextStyle(x => x.FontSize(12));
    }

    /// <summary>
    /// Composes the header section of the PDF.
    /// Override to customize the header.
    /// </summary>
    protected virtual void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(GetDocumentTitle()).FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                column.Item().Text($"Genererad: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
            });

            row.ConstantItem(100).Height(50).Placeholder();
        });
    }

    /// <summary>
    /// Composes the footer section of the PDF with page numbers.
    /// Override to customize the footer.
    /// </summary>
    protected virtual void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Sida ");
            text.CurrentPageNumber();
            text.Span(" av ");
            text.TotalPages();
        });
    }

    /// <summary>
    /// Returns the title to display in the header.
    /// Override to provide a specific document title.
    /// </summary>
    protected abstract string GetDocumentTitle();

    /// <summary>
    /// Renders text with markdown formatting (bold **text**, italic *text* or _text_).
    /// </summary>
    protected void RenderMarkdownText(TextDescriptor text, string content)
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

            if (nextBold >= 0 && nextBold > i) nextMarker = Math.Min(nextMarker, nextBold);
            if (nextItalicStar >= 0 && nextItalicStar > i && nextItalicStar != nextBold) nextMarker = Math.Min(nextMarker, nextItalicStar);
            if (nextItalicUnderscore >= 0 && nextItalicUnderscore > i) nextMarker = Math.Min(nextMarker, nextItalicUnderscore);

            // Ensure we advance at least 1 character to prevent infinite loops
            if (nextMarker == i) nextMarker = i + 1;

            text.Span(content.Substring(i, nextMarker - i));
            i = nextMarker;
        }
    }

    /// <summary>
    /// Renders a markdown table as a proper PDF table.
    /// </summary>
    protected void RenderMarkdownTable(IContainer container, List<string> tableLines)
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
}
