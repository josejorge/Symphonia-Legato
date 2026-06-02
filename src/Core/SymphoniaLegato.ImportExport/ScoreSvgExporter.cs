using System.Globalization;
using System.Text;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.ImportExport;

/// <summary>
/// Exports a score page as an SVG document by walking the LayoutResult tree.
/// All coordinates are taken directly from the layout engine so the output
/// matches the on-screen appearance pixel-for-pixel (at zoom=1).
/// </summary>
public sealed class ScoreSvgExporter
{
    private readonly ILayoutEngine _layout;

    public ScoreSvgExporter(ILayoutEngine layout) => _layout = layout;

    /// <summary>Exports the first page of the score to an SVG string.</summary>
    public string ExportPage(Score score, LayoutOptions? options = null, int pageNumber = 1)
    {
        options ??= new LayoutOptions { ForPrinting = true, Zoom = 1.0 };
        var result = _layout.ComputePageLayout(score, options, pageNumber);

        if (result.Pages.Count == 0)
            return "<svg xmlns=\"http://www.w3.org/2000/svg\"/>";

        var page = result.Pages[0];
        var sb = new StringBuilder();
        double w = page.WidthPx;
        double h = page.HeightPx;

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" " +
                      $"width=\"{F(w)}\" height=\"{F(h)}\" " +
                      $"viewBox=\"0 0 {F(w)} {F(h)}\">");

        // Background
        Rect(sb, 0, 0, w, h, "#262626");

        // Score title
        if (!string.IsNullOrWhiteSpace(score.Title))
            Text(sb, score.Title, w / 2, 28, "#DDDDDD", 18, "bold", "middle");
        if (!string.IsNullOrWhiteSpace(score.Composer))
            Text(sb, score.Composer, w - 40, 48, "#AAAAAA", 11, "italic", "end");

        foreach (var system in page.Systems)
            DrawSystem(sb, system);

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    /// <summary>Exports all pages and saves them to a directory as SVG files.</summary>
    public async Task ExportAllPagesAsync(Score score, string outputDir, LayoutOptions? options = null)
    {
        options ??= new LayoutOptions { ForPrinting = true, Zoom = 1.0 };
        var result = _layout.ComputeLayout(score, options);
        Directory.CreateDirectory(outputDir);

        for (int i = 0; i < result.Pages.Count; i++)
        {
            string svg = ExportPage(score, options, i + 1);
            string path = Path.Combine(outputDir, $"page_{i + 1:D2}.svg");
            await File.WriteAllTextAsync(path, svg);
        }
    }

    // ── Drawing helpers ───────────────────────────────────────────────

    private static void DrawSystem(StringBuilder sb, RenderedSystem system)
    {
        foreach (var staff in system.Staves)
            DrawStaff(sb, staff, system);
    }

    private static void DrawStaff(StringBuilder sb, RenderedStaff staff, RenderedSystem system)
    {
        double sp = staff.Height / 8.0; // staff space

        // Draw staff lines for each measure
        foreach (var measure in staff.Measures)
        {
            double x1 = measure.X;
            double x2 = measure.X + measure.Width;

            for (int line = 0; line < 5; line++)
            {
                double y = staff.Y + (4 - line) * 2 * sp;
                Line(sb, x1, y, x2, y, "#828282", 0.8);
            }

            DrawMeasureContent(sb, measure, staff, sp);
        }
    }

    private static void DrawMeasureContent(StringBuilder sb, RenderedMeasure measure, RenderedStaff staff, double sp)
    {
        double staffTop = staff.Y;
        double staffBot = staff.Y + 4 * 2 * sp;

        // Barlines
        DrawBarline(sb, measure.StartBarline, measure.X, staffTop, staffBot, sp);
        DrawBarline(sb, measure.EndBarline,   measure.X + measure.Width, staffTop, staffBot, sp);

        // Clef
        if (measure.ShowClef)
        {
            string clefGlyph = measure.ClefType == ClefType.Bass ? "𝄢" : "𝄞";
            double clefY = measure.ClefType == ClefType.Bass
                ? staffTop + 2 * sp * 2 + sp
                : staffTop + 4 * 2 * sp + sp;
            Text(sb, clefGlyph, measure.X + 4, clefY, "#CCCCCC", (int)(sp * 3.5), "normal", "start",
                 "Segoe UI Symbol, Noto Music, serif");
        }

        // Time signature
        if (measure.ShowTimeSignature)
        {
            double tsX = measure.X + (measure.ShowClef ? 28 : 4);
            double midY = staffTop + 4 * sp;
            Text(sb, measure.TimeSignature.Numerator.ToString(), tsX, midY, "#CCCCCC", (int)(sp * 2), "bold", "middle");
            Text(sb, measure.TimeSignature.Denominator.ToString(), tsX, midY + sp * 2, "#CCCCCC", (int)(sp * 2), "bold", "middle");
        }

        // Beams
        foreach (var beam in measure.Beams)
        {
            double thickness = sp * 0.5 * (1 - beam.BeamLevel * 0.1);
            Line(sb, beam.StartX, beam.StartY, beam.EndX, beam.EndY, "#DCDCDC", thickness);
        }

        // Notes & rests
        foreach (var el in measure.Elements)
        {
            double noteY = staffTop + (9 - el.StaffPosition) * sp;
            double rx = sp * 0.55, ry = sp * 0.40;

            if (el.IsRest)
            {
                DrawRest(sb, el, el.X, noteY, sp);
            }
            else
            {
                // Ledger lines
                if (el.NeedsLedgerLines)
                {
                    int dir = el.LedgerLinesAbove ? 1 : -1;
                    double lBase = el.LedgerLinesAbove ? staffTop - sp * 2 : staffBot + sp * 2;
                    for (int l = 0; l < el.LedgerLineCount; l++)
                        Line(sb, el.X - sp, lBase - dir * l * sp * 2, el.X + sp, lBase - dir * l * sp * 2, "#828282", 0.8);
                }

                // Accidental
                if (el.ShowAccidental)
                {
                    string acc = el.Accidental switch
                    {
                        Accidental.Sharp   => "♯",
                        Accidental.Flat    => "♭",
                        Accidental.Natural => "♮",
                        _ => ""
                    };
                    if (acc.Length > 0)
                        Text(sb, acc, el.X - sp * 1.2, noteY + ry, "#CCCCCC", (int)(sp * 1.5), "normal", "end");
                }

                // Notehead
                string fill = el.NoteValue == NoteValue.Whole || el.NoteValue == NoteValue.Half
                    ? "none" : "#DCDCDC";
                string stroke = "#DCDCDC";
                Ellipse(sb, el.X, noteY, rx, ry, fill, stroke);

                // Half-note hole
                if (el.NoteValue == NoteValue.Half)
                    Ellipse(sb, el.X, noteY, rx * 0.45, ry * 0.5, "#262626", "none");

                // Stem
                if (el.Stem != StemDirection.None && el.NoteValue != NoteValue.Whole)
                {
                    double stemX = el.Stem == StemDirection.Up ? el.X + rx : el.X - rx;
                    Line(sb, stemX, noteY, stemX, el.StemEndY, "#DCDCDC", 1.2);
                }
            }

            // Lyrics
            foreach (var (verse, lyricText, _) in el.Lyrics)
                Text(sb, lyricText, el.X, staffBot + sp * 2.5 + verse * sp * 1.8, "#B4B4B4", (int)(sp * 1.1), "italic", "middle");
        }

        // Hairpins
        foreach (var hp in measure.Hairpins)
        {
            double mid = hp.Y;
            double open = sp * 1.2;
            if (hp.Type == HairpinType.Crescendo)
            {
                Line(sb, hp.StartX, mid, hp.EndX, mid - open, "#B4B4B4", 1.5);
                Line(sb, hp.StartX, mid, hp.EndX, mid + open, "#B4B4B4", 1.5);
            }
            else
            {
                Line(sb, hp.StartX, mid - open, hp.EndX, mid, "#B4B4B4", 1.5);
                Line(sb, hp.StartX, mid + open, hp.EndX, mid, "#B4B4B4", 1.5);
            }
        }

        // Slurs
        foreach (var slur in measure.Slurs)
        {
            sb.AppendLine($"  <path d=\"M {F(slur.StartX)} {F(slur.StartY)} " +
                          $"C {F(slur.Cp1X)} {F(slur.Cp1Y)} {F(slur.Cp2X)} {F(slur.Cp2Y)} " +
                          $"{F(slur.EndX)} {F(slur.EndY)}\" " +
                          $"fill=\"none\" stroke=\"#B4B4B4\" stroke-width=\"1.5\"/>");
        }

        // Dynamics
        foreach (var dyn in measure.Dynamics)
            Text(sb, dyn.Symbol, dyn.X, dyn.Y, "#C8C8C8", (int)(sp * 1.4), "italic", "start");

        // Tempo
        foreach (var tempo in measure.TempoMarkings)
        {
            string text = tempo.BPM.HasValue ? $"♩= {tempo.BPM}" : tempo.Text;
            Text(sb, text, tempo.X, tempo.Y, "#C8C8C8", (int)(sp * 1.2), "normal", "start");
        }
    }

    private static void DrawRest(StringBuilder sb, RenderedNoteElement el, double x, double y, double sp)
    {
        switch (el.NoteValue)
        {
            case NoteValue.Whole:
                Rect(sb, x - sp * 0.8, y, sp * 1.6, sp * 0.5, "#DCDCDC");
                break;
            case NoteValue.Half:
                Rect(sb, x - sp * 0.8, y, sp * 1.6, sp * 0.5, "none", "#DCDCDC", 1.2);
                break;
            case NoteValue.Quarter:
                sb.AppendLine($"  <text x=\"{F(x)}\" y=\"{F(y + sp)}\" font-family=\"Segoe UI Symbol,serif\" " +
                              $"font-size=\"{F(sp * 2.5)}\" fill=\"#B4B4B4\" text-anchor=\"middle\">𝄽</text>");
                break;
            default:
                sb.AppendLine($"  <text x=\"{F(x)}\" y=\"{F(y + sp)}\" font-family=\"Segoe UI Symbol,serif\" " +
                              $"font-size=\"{F(sp * 2.5)}\" fill=\"#B4B4B4\" text-anchor=\"middle\">𝄾</text>");
                break;
        }
    }

    private static void DrawBarline(StringBuilder sb, BarlineType type, double x, double yTop, double yBot, double sp)
    {
        switch (type)
        {
            case BarlineType.Single:
            case BarlineType.Dashed:
            case BarlineType.Dotted:
                Line(sb, x, yTop, x, yBot, "#828282", 1.5);
                break;
            case BarlineType.Double:
                Line(sb, x - 2, yTop, x - 2, yBot, "#828282", 1.0);
                Line(sb, x + 2, yTop, x + 2, yBot, "#828282", 1.0);
                break;
            case BarlineType.Final:
                Line(sb, x - 3, yTop, x - 3, yBot, "#B4B4B4", 1.5);
                Line(sb, x + 1, yTop, x + 1, yBot, "#B4B4B4", 4.0);
                break;
            case BarlineType.RepeatStart:
                Line(sb, x, yTop, x, yBot, "#828282", 1.5);
                break;
            case BarlineType.RepeatEnd:
                Line(sb, x, yTop, x, yBot, "#828282", 1.5);
                break;
        }
    }

    // ── SVG element builders ──────────────────────────────────────────

    private static void Line(StringBuilder sb, double x1, double y1, double x2, double y2, string color, double width) =>
        sb.AppendLine($"  <line x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\" " +
                      $"stroke=\"{color}\" stroke-width=\"{F(width)}\"/>");

    private static void Rect(StringBuilder sb, double x, double y, double w, double h, string fill,
        string stroke = "none", double strokeW = 0) =>
        sb.AppendLine($"  <rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(w)}\" height=\"{F(h)}\" " +
                      $"fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\"/>");

    private static void Ellipse(StringBuilder sb, double cx, double cy, double rx, double ry,
        string fill, string stroke, double strokeW = 1.0) =>
        sb.AppendLine($"  <ellipse cx=\"{F(cx)}\" cy=\"{F(cy)}\" rx=\"{F(rx)}\" ry=\"{F(ry)}\" " +
                      $"fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\"/>");

    private static void Text(StringBuilder sb, string content, double x, double y, string color,
        int size, string weight, string anchor, string font = "Inter,sans-serif") =>
        sb.AppendLine($"  <text x=\"{F(x)}\" y=\"{F(y)}\" font-family=\"{font}\" font-size=\"{size}\" " +
                      $"font-weight=\"{weight}\" fill=\"{color}\" text-anchor=\"{anchor}\">" +
                      $"{System.Security.SecurityElement.Escape(content)}</text>");

    private static string F(double v) => v.ToString("F2", CultureInfo.InvariantCulture);
}
