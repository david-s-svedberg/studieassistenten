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
    private string? _documentTitle;

    public byte[] GenerateFlashcardPdf(GeneratedContent content)
    {
        // Get test name from content.Test (generated content now belongs to test)
        _documentTitle = content.Test?.Name ?? content.StudyDocument?.Test?.Name ?? content.Title ?? "Flashcards";
        return GeneratePdf(container => ComposeContent(container, content));
    }

    protected override string GetDocumentTitle()
    {
        return _documentTitle ?? "Flashcards";
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
            // Flashcards in 2-column layout (question | answer)
            if (content.Flashcards != null && content.Flashcards.Any())
            {
                foreach (var flashcard in content.Flashcards.OrderBy(f => f.Order))
                {
                    column.Item().EnsureSpace().Element(container => ComposeFlashcard(container, flashcard));
                    column.Spacing(4); // Small spacing between flashcard rows
                }
            }
        });
    }

    void ComposeFlashcard(IContainer container, Flashcard flashcard)
    {
        const int verticalPadding = 20;

        container.Row(row =>
        {
            // Question (left column)
            row.RelativeItem()
                .Border(2)
                .BorderColor(Colors.Black)
                .PaddingVertical(verticalPadding)
                .PaddingHorizontal(10)
                .AlignCenter()
                .AlignMiddle()
                .Text(flashcard.Question);

            // Small gap between columns
            row.ConstantItem(4);

            // Answer (right column)
            row.RelativeItem()
                .Border(2)
                .BorderColor(Colors.Black)
                .PaddingVertical(verticalPadding)
                .PaddingHorizontal(10)
                .AlignCenter()
                .AlignMiddle()
                .Text(flashcard.Answer);
        });
    }
}
