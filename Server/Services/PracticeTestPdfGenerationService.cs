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
    public byte[] GeneratePracticeTestPdf(GeneratedContent content)
    {
        return GeneratePdf(container => ComposeContent(container, content));
    }

    protected override string GetDocumentTitle()
    {
        return "Övningsprov";
    }

    void ComposeContent(IContainer container, GeneratedContent content)
    {
        container.Column(column =>
        {
            column.Spacing(15);

            // Title
            column.Item().Text(content.Title ?? "Övningsprov").FontSize(16).Bold();

            // Instructions
            column.Item().Text("Instruktioner: Markera det rätta svaret för varje fråga.").FontSize(10).Italic().FontColor(Colors.Grey.Darken1);

            // Parse and display questions from content
            var lines = content.Content?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
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

            // Answer key section
            column.Item().PageBreak();
            column.Item().Text("Facit").FontSize(16).Bold().FontColor(Colors.Green.Medium);
            column.Item().PaddingTop(10).Text("(Svar finns på separata sidor för att underlätta övning)").FontSize(10).Italic();
        });
    }

    void ComposeQuestion(IContainer container, int number, List<string> questionLines)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Spacing(5);

            foreach (var line in questionLines)
            {
                var cleanLine = line.Trim()
                    .Replace("**", "")
                    .Replace("##", "")
                    .Replace("#", "");

                if (cleanLine.StartsWith("A)") || cleanLine.StartsWith("B)") ||
                    cleanLine.StartsWith("C)") || cleanLine.StartsWith("D)"))
                {
                    column.Item().PaddingLeft(20).Text(cleanLine);
                }
                else
                {
                    column.Item().Text(cleanLine).Bold();
                }
            }
        });
    }
}
