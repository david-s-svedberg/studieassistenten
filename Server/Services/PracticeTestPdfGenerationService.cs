using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudieAssistenten.Server.Services.Pdf;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services;

public interface IPracticeTestPdfGenerationService
{
    byte[] GeneratePracticeTestPdf(GeneratedContent content);
}

public class PracticeTestPdfGenerationService : BasePdfGenerationService, IPracticeTestPdfGenerationService
{
    private string? _documentTitle;

    public byte[] GeneratePracticeTestPdf(GeneratedContent content)
    {
        // Get test name from content.Test (generated content now belongs to test)
        _documentTitle = content.Test?.Name ?? content.StudyDocument?.Test?.Name ?? content.Title ?? "Övningsprov";
        return GeneratePdf(container => ComposeContent(container, content));
    }

    protected override string GetDocumentTitle()
    {
        return _documentTitle ?? "Övningsprov";
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

    void ComposeContent(IContainer container, GeneratedContent content)
    {
        container.Column(column =>
        {
            column.Spacing(15);

            // Split content into questions and answer key sections
            var fullContent = content.Content ?? string.Empty;
            var answerKeyIndex = fullContent.IndexOf("Facit", StringComparison.OrdinalIgnoreCase);
            if (answerKeyIndex == -1)
                answerKeyIndex = fullContent.IndexOf("Answer Key", StringComparison.OrdinalIgnoreCase);
            if (answerKeyIndex == -1)
                answerKeyIndex = fullContent.IndexOf("Svar:", StringComparison.OrdinalIgnoreCase);

            string questionsContent = answerKeyIndex > 0 ? fullContent.Substring(0, answerKeyIndex) : fullContent;
            string answerKeyContent = answerKeyIndex > 0 ? fullContent.Substring(answerKeyIndex) : string.Empty;

            // Instructions
            column.Item().Text("Instruktioner: Besvara frågorna nedan. Facit finns på sista sidan.").FontSize(10).Italic().FontColor(Colors.Grey.Darken1);

            // Parse and display questions from content
            var lines = questionsContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int questionNumber = 1;
            var currentQuestion = new List<string>();

            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith(questionNumber + ".") ||
                    line.TrimStart().StartsWith($"**{questionNumber}.") ||
                    line.TrimStart().StartsWith($"#{questionNumber}") ||
                    (questionNumber > 1 && (line.Contains("**Fråga") || line.Contains("Question"))))
                {
                    // Render previous question if exists
                    if (currentQuestion.Any())
                    {
                        column.Item().Element(container => ComposeQuestion(container, questionNumber - 1, currentQuestion));
                        currentQuestion.Clear();
                    }
                    questionNumber++;
                }

                currentQuestion.Add(line);
            }

            // Render last question
            if (currentQuestion.Any())
            {
                column.Item().Element(container => ComposeQuestion(container, questionNumber - 1, currentQuestion));
            }

            // Answer key section on separate page
            if (!string.IsNullOrWhiteSpace(answerKeyContent))
            {
                column.Item().PageBreak();
                column.Item().Text("Facit").FontSize(18).Bold().FontColor(Colors.Green.Medium);
                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Green.Medium);

                // Render answer key with markdown support
                column.Item().PaddingTop(15).Column(answerColumn =>
                {
                    var answerLines = answerKeyContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    int j = 0;

                    while (j < answerLines.Length)
                    {
                        var line = answerLines[j].Trim();

                        // Check if this is a table
                        if (line.Contains("|") && j + 1 < answerLines.Length &&
                            answerLines[j + 1].Trim().StartsWith("|") && answerLines[j + 1].Contains("---"))
                        {
                            var tableLines = new List<string> { line, answerLines[j + 1] };
                            j += 2;

                            while (j < answerLines.Length && answerLines[j].Trim().StartsWith("|"))
                            {
                                tableLines.Add(answerLines[j].Trim());
                                j++;
                            }

                            answerColumn.Item().Element(container => RenderMarkdownTable(container, tableLines));
                            continue;
                        }

                        RenderAnswerKeyLine(answerColumn, line);
                        j++;
                    }
                });
            }
        });
    }

    void ComposeQuestion(IContainer container, int number, List<string> questionLines)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Spacing(5);

            int i = 0;
            while (i < questionLines.Count)
            {
                var line = questionLines[i].Trim();

                // Check if this is a table
                if (line.Contains("|") && i + 1 < questionLines.Count &&
                    questionLines[i + 1].Trim().StartsWith("|") && questionLines[i + 1].Contains("---"))
                {
                    var tableLines = new List<string> { line, questionLines[i + 1] };
                    i += 2;

                    while (i < questionLines.Count && questionLines[i].Trim().StartsWith("|"))
                    {
                        tableLines.Add(questionLines[i].Trim());
                        i++;
                    }

                    column.Item().Element(container => RenderMarkdownTable(container, tableLines));
                    continue;
                }

                RenderMarkdownLine(column, line);
                i++;
            }
        });
    }

    void RenderMarkdownLine(ColumnDescriptor column, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        // Remove markdown headers (## or #)
        if (line.StartsWith("## "))
        {
            line = line.Substring(3);
        }
        else if (line.StartsWith("# "))
        {
            line = line.Substring(2);
        }

        // Check if it's an answer option
        bool isAnswerOption = line.StartsWith("A)") || line.StartsWith("B)") ||
                              line.StartsWith("C)") || line.StartsWith("D)");

        // Render with markdown support
        if (isAnswerOption)
        {
            column.Item().PaddingLeft(20).Text(text => RenderMarkdownText(text, line));
        }
        else
        {
            column.Item().Text(text => RenderMarkdownText(text, line));
        }
    }

    void RenderAnswerKeyLine(ColumnDescriptor column, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            column.Item().PaddingTop(5);
            return;
        }

        // Remove markdown headers (## or #)
        if (line.StartsWith("## "))
        {
            column.Item().PaddingTop(5).Text(line.Substring(3)).FontSize(12).Bold();
            return;
        }
        if (line.StartsWith("# "))
        {
            column.Item().PaddingTop(5).Text(line.Substring(2)).FontSize(12).Bold();
            return;
        }

        // Handle bullet points
        if (line.StartsWith("- ") || line.StartsWith("* "))
        {
            column.Item().DefaultTextStyle(x => x.FontSize(11)).Row(row =>
            {
                row.ConstantItem(15).Text("•");
                row.RelativeItem().Text(text => RenderMarkdownText(text, line.Substring(2).Trim()));
            });
            return;
        }

        // All other text with markdown support
        column.Item().DefaultTextStyle(x => x.FontSize(11)).Text(text => RenderMarkdownText(text, line));
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

            if (nextBold >= 0 && nextBold > i) nextMarker = Math.Min(nextMarker, nextBold);
            if (nextItalicStar >= 0 && nextItalicStar > i && nextItalicStar != nextBold) nextMarker = Math.Min(nextMarker, nextItalicStar);
            if (nextItalicUnderscore >= 0 && nextItalicUnderscore > i) nextMarker = Math.Min(nextMarker, nextItalicUnderscore);

            // Ensure we advance at least 1 character to prevent infinite loops
            if (nextMarker == i) nextMarker = i + 1;

            text.Span(content.Substring(i, nextMarker - i));
            i = nextMarker;
        }
    }
}
