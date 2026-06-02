using Avalonia;
using Avalonia.Media.Imaging;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.Desktop.Controls;

namespace SymphoniaLegato.Desktop.Services;

/// <summary>
/// Renders score pages to PNG byte arrays using Avalonia's off-screen renderer.
/// Must be called on the UI thread. The output byte arrays can be embedded
/// in a PDF by <see cref="SymphoniaLegato.PdfEngine.ScorePdfExporter"/>.
/// </summary>
public sealed class ScorePngExporter
{
    private readonly ILayoutEngine _layout;

    public ScorePngExporter(ILayoutEngine layout) => _layout = layout;

    /// <summary>Returns one PNG per page, at the given DPI and zoom.</summary>
    public IReadOnlyList<byte[]> ExportAllPages(Score score, double dpi = 150, double zoom = 1.0)
    {
        var options = new LayoutOptions { ForPrinting = true, Zoom = zoom, DPI = dpi };
        var layoutResult = _layout.ComputeLayout(score, options);
        var pages = new List<byte[]>(layoutResult.Pages.Count);

        foreach (var page in layoutResult.Pages)
        {
            var png = RenderPage(page, layoutResult, dpi);
            pages.Add(png);
        }

        return pages;
    }

    /// <summary>Returns a single PNG for a specific page number (1-based).</summary>
    public byte[] ExportPage(Score score, int pageNumber = 1, double dpi = 150, double zoom = 1.0)
    {
        var options = new LayoutOptions { ForPrinting = true, Zoom = zoom, DPI = dpi };
        var layoutResult = _layout.ComputePageLayout(score, options, pageNumber);

        if (layoutResult.Pages.Count == 0)
            throw new InvalidOperationException($"Page {pageNumber} does not exist in the score.");

        return RenderPage(layoutResult.Pages[0], layoutResult, dpi);
    }

    private static byte[] RenderPage(RenderedPage page, LayoutResult layout, double dpi)
    {
        int w = Math.Max(1, (int)page.WidthPx);
        int h = Math.Max(1, (int)page.HeightPx);

        var canvas = new ScoreCanvas
        {
            LayoutResult  = layout,
            ShowHandColoring = false
        };

        canvas.Measure(new Size(w, h));
        canvas.Arrange(new Rect(0, 0, w, h));

        var bitmap = new RenderTargetBitmap(new PixelSize(w, h), new Vector(dpi, dpi));
        bitmap.Render(canvas);

        using var ms = new MemoryStream();
        bitmap.Save(ms);
        return ms.ToArray();
    }
}
