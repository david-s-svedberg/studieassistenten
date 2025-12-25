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
}
