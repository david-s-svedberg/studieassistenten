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
                    foreach (var line in answerLines)
                    {
                        RenderAnswerKeyLine(answerColumn, line.Trim());
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

            foreach (var line in questionLines)
            {
                RenderMarkdownLine(column, line.Trim());
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

        // Handle bold text (**text**)
        if (line.Contains("**"))
        {
            column.Item().PaddingLeft(isAnswerOption ? 20 : 0).Text(text =>
            {
                var parts = line.Split("**");
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        // Regular text - make question text bold, answer options normal
                        if (!isAnswerOption && !string.IsNullOrEmpty(parts[i]))
                        {
                            text.Span(parts[i]).Bold();
                        }
                        else
                        {
                            text.Span(parts[i]);
                        }
                    }
                    else
                    {
                        // Text within ** markers - always bold
                        text.Span(parts[i]).Bold();
                    }
                }
            });
        }
        else
        {
            // No markdown - render as-is
            if (isAnswerOption)
            {
                column.Item().PaddingLeft(20).Text(line);
            }
            else
            {
                column.Item().Text(line).Bold();
            }
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

        // Handle bold text (**text**)
        if (line.Contains("**"))
        {
            column.Item().DefaultTextStyle(x => x.FontSize(11)).Text(text =>
            {
                var parts = line.Split("**");
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        text.Span(parts[i]);
                    }
                    else
                    {
                        text.Span(parts[i]).Bold();
                    }
                }
            });
        }
        else
        {
            column.Item().Text(line).FontSize(11);
        }
    }
}
