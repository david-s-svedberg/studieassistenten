using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudieAssistenten.Server.Services.Pdf;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services;

public interface IFlashcardPdfGenerationService
{
    byte[] GenerateFlashcardPdf(GeneratedContent content);
}

public class FlashcardPdfGenerationService : BasePdfGenerationService, IFlashcardPdfGenerationService
{
    public byte[] GenerateFlashcardPdf(GeneratedContent content)
    {
        return GeneratePdf(container => ComposeContent(container, content));
    }

    protected override string GetDocumentTitle()
    {
        return "Flashcards";
    }

    void ComposeContent(IContainer container, GeneratedContent content)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            // Title
            column.Item().Text(content.Title ?? "Flashcards").FontSize(16).Bold();

            // Flashcards
            if (content.Flashcards != null && content.Flashcards.Any())
            {
                foreach (var flashcard in content.Flashcards.OrderBy(f => f.Order))
                {
                    column.Item().Element(container => ComposeFlashcard(container, flashcard));
                }
            }
        });
    }

    void ComposeFlashcard(IContainer container, Flashcard flashcard)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Spacing(5);

            // Question
            column.Item().Row(row =>
            {
                row.ConstantItem(30).Text($"{flashcard.Order}.").Bold();
                row.RelativeItem().Text(flashcard.Question).Bold();
            });

            // Answer
            column.Item().PaddingLeft(30).Text(flashcard.Answer);
        });
    }
}
