using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services;

public interface ISummaryPdfGenerationService
{
    byte[] GenerateSummaryPdf(GeneratedContent content);
}

public class SummaryPdfGenerationService : ISummaryPdfGenerationService
{
    public SummaryPdfGenerationService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateSummaryPdf(GeneratedContent content)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12).LineHeight(1.5f));

                page.Header().Element(ComposeHeader);
                page.Content().Element(container => ComposeContent(container, content));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Sida ");
                    text.CurrentPageNumber();
                    text.Span(" av ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Sammanfattning").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                column.Item().Text($"Genererad: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
            });

            row.ConstantItem(100).Height(50).Placeholder();
        });
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

                    // Headers (## or #)
                    if (trimmedLine.StartsWith("## "))
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
                            row.RelativeItem().Text(trimmedLine.Substring(2).Trim());
                        });
                    }
                    // Numbered lists
                    else if (System.Text.RegularExpressions.Regex.IsMatch(trimmedLine, @"^\d+\.\s"))
                    {
                        column.Item().PaddingLeft(10).Text(trimmedLine);
                    }
                    // Bold text (**text**)
                    else if (trimmedLine.Contains("**"))
                    {
                        column.Item().Text(text =>
                        {
                            var parts = trimmedLine.Split("**");
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
                    // Regular text
                    else
                    {
                        column.Item().Text(trimmedLine);
                    }
                }
            }
        });
    }
}
