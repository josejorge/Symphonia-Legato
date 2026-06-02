using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.PdfEngine;

/// <summary>
/// Generates a PDF from pre-rendered page images (produced by the Desktop's PNG exporter).
/// Keeps the PdfEngine free of Avalonia dependencies while still producing
/// pixel-perfect output that matches the on-screen rendering exactly.
/// </summary>
public sealed class ScorePdfExporter
{
    static ScorePdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Builds a PDF document from a list of PNG-encoded page images.
    /// Each element in <paramref name="pageImages"/> becomes one PDF page.
    /// </summary>
    public byte[] GenerateFromImages(Score score, IReadOnlyList<byte[]> pageImages)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(pageImages);

        if (pageImages.Count == 0)
            throw new ArgumentException("At least one page image is required.", nameof(pageImages));

        float pageWidthPt  = MmToPt((float)score.PageWidthMm);
        float pageHeightPt = MmToPt((float)score.PageHeightMm);

        return Document.Create(container =>
        {
            // Title page header on first page, continuation pages after
            for (int i = 0; i < pageImages.Count; i++)
            {
                int pageIndex = i;
                byte[] png = pageImages[pageIndex];

                container.Page(page =>
                {
                    page.Size(pageWidthPt, pageHeightPt, Unit.Point);
                    page.Margin(0);
                    page.PageColor(Colors.White);

                    page.Content().Column(col =>
                    {
                        if (pageIndex == 0)
                        {
                            // Score header block
                            col.Item().PaddingTop(20).PaddingHorizontal(40).Column(header =>
                            {
                                if (!string.IsNullOrWhiteSpace(score.Title))
                                    header.Item().Text(score.Title)
                                        .Bold().FontSize(18).FontColor(Colors.Black)
                                        .AlignCenter();

                                if (!string.IsNullOrWhiteSpace(score.Subtitle))
                                    header.Item().Text(score.Subtitle)
                                        .FontSize(12).FontColor(Colors.Grey.Darken2)
                                        .AlignCenter();

                                if (!string.IsNullOrWhiteSpace(score.Composer))
                                    header.Item().PaddingTop(2).Text(score.Composer)
                                        .Italic().FontSize(11).FontColor(Colors.Grey.Darken1)
                                        .AlignRight();

                                if (!string.IsNullOrWhiteSpace(score.Lyricist))
                                    header.Item().Text($"Lyrics: {score.Lyricist}")
                                        .FontSize(10).FontColor(Colors.Grey.Medium)
                                        .AlignLeft();

                                if (!string.IsNullOrWhiteSpace(score.Copyright))
                                    header.Item().PaddingTop(2).Text(score.Copyright)
                                        .FontSize(8).FontColor(Colors.Grey.Medium)
                                        .AlignCenter();
                            });
                        }

                        // Score image — fill the page
                        col.Item().Extend().Image(png).FitArea();
                    });
                });
            }
        }).GeneratePdf();
    }

    /// <summary>
    /// Convenience overload: produces a single-page PDF from one PNG image.
    /// </summary>
    public byte[] GenerateFromImages(Score score, byte[] singlePagePng) =>
        GenerateFromImages(score, [singlePagePng]);

    private static float MmToPt(float mm) => mm * 2.8346f;
}
