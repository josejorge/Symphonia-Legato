using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Rendering;

public sealed class NoteClickedEventArgs(Guid noteId, int measureNumber, Guid staffId) : EventArgs
{
    public Guid NoteId { get; } = noteId;
    public int MeasureNumber { get; } = measureNumber;
    public Guid StaffId { get; } = staffId;
}

public sealed class StaffPositionClickedEventArgs(
    Guid staffId, int measureNumber, int staffPosition,
    Clef clef, KeySignature keySignature) : EventArgs
{
    public Guid StaffId { get; } = staffId;
    public int MeasureNumber { get; } = measureNumber;
    public int StaffPosition { get; } = staffPosition;
    public Clef Clef { get; } = clef;
    public KeySignature KeySignature { get; } = keySignature;
}

/// <summary>
/// Score canvas rendering all notation elements from a <see cref="LayoutResult"/>:
/// noteheads, stems, beams, flags, accidentals, rests, clefs,
/// time/key signatures, dynamics, hairpins, slurs, articulations,
/// tempo markings, lyrics, hand coloring, and note selection.
/// </summary>
public sealed class ScoreCanvas : Control
{
    // ── Styled properties ─────────────────────────────────────────────

    public static readonly StyledProperty<LayoutResult?> LayoutResultProperty =
        AvaloniaProperty.Register<ScoreCanvas, LayoutResult?>(nameof(LayoutResult));
    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<ScoreCanvas, double>(nameof(Zoom), 1.0);
    public static readonly StyledProperty<Guid?> SelectedNoteIdProperty =
        AvaloniaProperty.Register<ScoreCanvas, Guid?>(nameof(SelectedNoteId));
    public static readonly StyledProperty<bool> ShowHandColoringProperty =
        AvaloniaProperty.Register<ScoreCanvas, bool>(nameof(ShowHandColoring));

    public LayoutResult? LayoutResult { get => GetValue(LayoutResultProperty); set => SetValue(LayoutResultProperty, value); }
    public double Zoom { get => GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }
    public Guid? SelectedNoteId { get => GetValue(SelectedNoteIdProperty); set => SetValue(SelectedNoteIdProperty, value); }
    public bool ShowHandColoring { get => GetValue(ShowHandColoringProperty); set => SetValue(ShowHandColoringProperty, value); }

    // ── Events ────────────────────────────────────────────────────────

    public event EventHandler<NoteClickedEventArgs>? NoteClicked;
    public event EventHandler<StaffPositionClickedEventArgs>? StaffPositionClicked;

    // ── Brushes & pens ────────────────────────────────────────────────

    private static readonly IBrush PageBrush       = new SolidColorBrush(Color.FromRgb(38, 38, 38));
    private static readonly IBrush NoteBrush       = new SolidColorBrush(Color.FromRgb(220, 220, 220));
    private static readonly IBrush RestBrush       = new SolidColorBrush(Color.FromRgb(180, 180, 180));
    private static readonly IBrush SelectBrush     = new SolidColorBrush(Color.FromArgb(190, 100, 149, 237));
    private static readonly IBrush RightHandBrush  = new SolidColorBrush(Color.FromRgb(100, 149, 237));
    private static readonly IBrush LeftHandBrush   = new SolidColorBrush(Color.FromRgb(205, 92, 92));
    private static readonly IBrush DynBrush        = new SolidColorBrush(Color.FromRgb(200, 200, 200));
    private static readonly IBrush TempoBrush      = new SolidColorBrush(Color.FromRgb(200, 200, 200));
    private static readonly IBrush MeasureNumBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
    private static readonly IBrush LyricBrush      = new SolidColorBrush(Color.FromRgb(180, 180, 180));
    private static readonly IBrush AccBrush        = new SolidColorBrush(Color.FromRgb(200, 200, 200));
    private static readonly IBrush ClefBrush       = new SolidColorBrush(Color.FromRgb(200, 200, 200));

    private static readonly Pen StaffPen      = new(new SolidColorBrush(Color.FromRgb(130, 130, 130)), 0.8);
    private static readonly Pen BarlinePen    = new(new SolidColorBrush(Color.FromRgb(130, 130, 130)), 1.5);
    private static readonly Pen FinalBarPen   = new(new SolidColorBrush(Color.FromRgb(180, 180, 180)), 4.0);
    private static readonly Pen LedgerPen     = new(new SolidColorBrush(Color.FromRgb(130, 130, 130)), 0.8);
    private static readonly Pen StemPen       = new(new SolidColorBrush(Color.FromRgb(200, 200, 200)), 1.2);
    private static readonly Pen NoteOutline   = new(new SolidColorBrush(Color.FromRgb(150, 150, 150)), 1.0);
    private static readonly Pen HairpinPen    = new(new SolidColorBrush(Color.FromRgb(180, 180, 180)), 1.5);
    private static readonly Pen SlurPen       = new(new SolidColorBrush(Color.FromRgb(180, 180, 180)), 1.5);
    private static readonly Pen RepeatBarPen  = new(new SolidColorBrush(Color.FromRgb(160, 160, 160)), 3.0);

    private static readonly Typeface MusicTypeface  = new(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
    private static readonly Typeface BoldTypeface   = new(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
    private static readonly Typeface ItalicTypeface = new(FontFamily.Default, FontStyle.Italic, FontWeight.Normal);

    // ── Constructor ───────────────────────────────────────────────────

    public ScoreCanvas()
    {
        LayoutResultProperty.Changed.AddClassHandler<ScoreCanvas>((s, _) => s.InvalidateVisual());
        ZoomProperty.Changed.AddClassHandler<ScoreCanvas>((s, _) => s.InvalidateVisual());
        SelectedNoteIdProperty.Changed.AddClassHandler<ScoreCanvas>((s, _) => s.InvalidateVisual());
        ShowHandColoringProperty.Changed.AddClassHandler<ScoreCanvas>((s, _) => s.InvalidateVisual());
        this.AddHandler(PointerPressedEvent, OnPointerPressed);
    }

    // ── Size ─────────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        var layout = LayoutResult;
        if (layout is null || layout.Pages.Count == 0) return base.MeasureOverride(availableSize);
        double w = layout.Pages.Max(p => p.WidthPx);
        double h = layout.Pages.Sum(p => p.HeightPx);
        return new Size(w, h);
    }

    // ── Render ────────────────────────────────────────────────────────

    public override void Render(DrawingContext ctx)
    {
        var layout = LayoutResult;
        if (layout is null) return;
        foreach (var page in layout.Pages)
            DrawPage(ctx, page);
    }

    private void DrawPage(DrawingContext ctx, RenderedPage page)
    {
        ctx.FillRectangle(PageBrush,
            new Rect(page.WidthPx * 0.04, 16, page.WidthPx * 0.92, page.HeightPx - 32));
        foreach (var system in page.Systems)
            DrawSystem(ctx, system);
    }

    private void DrawSystem(DrawingContext ctx, RenderedSystem system)
    {
        foreach (var staff in system.Staves)
            DrawStaff(ctx, staff);
    }

    // ── Staff ─────────────────────────────────────────────────────────

    private void DrawStaff(DrawingContext ctx, RenderedStaff staff)
    {
        if (staff.Measures.Count == 0) return;
        double sp = staff.Height / 4.0;  // pixels per staff space

        double startX = staff.Measures[0].X;
        double endX   = staff.Measures[^1].X + staff.Measures[^1].Width;

        // 5 staff lines
        for (int line = 0; line < 5; line++)
        {
            double ly = staff.Y + line * sp;
            ctx.DrawLine(StaffPen, new Point(startX, ly), new Point(endX, ly));
        }

        foreach (var measure in staff.Measures)
            DrawMeasure(ctx, measure, staff, sp);
    }

    // ── Measure ───────────────────────────────────────────────────────

    private void DrawMeasure(DrawingContext ctx, RenderedMeasure m, RenderedStaff staff, double sp)
    {
        double x = m.X;
        double y = staff.Y;
        double h = staff.Height;

        // Header decorations
        if (m.ShowClef)
            DrawClef(ctx, m.ClefType, x + 2, y, sp);

        if (m.ShowKeySignature && m.KeySignature.Fifths != 0)
        {
            double ksX = x + (m.ShowClef ? 26 * sp / 10.0 : 4);
            DrawKeySignature(ctx, m.KeySignature, m.ClefType, ksX, y, sp);
        }

        if (m.ShowTimeSignature)
        {
            double ksW = m.ShowKeySignature ? Math.Abs(m.KeySignature.Fifths) * 8 * sp / 10.0 : 0;
            double tsX = x + (m.ShowClef ? 26 * sp / 10.0 : 4) + ksW + 2;
            DrawTimeSignature(ctx, m.TimeSignature, tsX, y, sp);
        }

        // Tempo
        foreach (var tempo in m.TempoMarkings)
            DrawTempo(ctx, tempo, sp);

        // Measure number
        var ft = new FormattedText(m.MeasureNumber.ToString(),
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, MusicTypeface, sp * 1.0, MeasureNumBrush);
        ctx.DrawText(ft, new Point(x + 2, y - sp * 1.4));

        // Barlines
        DrawBarline(ctx, m.StartBarline, x, y, h, sp, isStart: true);
        DrawBarline(ctx, m.EndBarline, x + m.Width, y, h, sp, isStart: false);

        // Beams first (drawn behind notes)
        foreach (var beam in m.Beams)
            DrawBeam(ctx, beam, sp);

        // Notes / rests
        foreach (var elem in m.Elements)
            DrawNoteElement(ctx, elem, y, h, sp);

        // Dynamics
        foreach (var dyn in m.Dynamics)
            DrawDynamic(ctx, dyn, sp);

        // Hairpins
        foreach (var hp in m.Hairpins)
            DrawHairpin(ctx, hp, sp);

        // Slurs
        foreach (var slur in m.Slurs)
            DrawSlur(ctx, slur, sp);
    }

    // ── Clef ─────────────────────────────────────────────────────────

    private static void DrawClef(DrawingContext ctx, ClefType clef, double x, double y, double sp)
    {
        // Unicode musical symbols — rendered as text
        string symbol = clef switch
        {
            ClefType.Treble => "𝄞",
            ClefType.Bass   => "𝄢",
            ClefType.Alto   => "𝄡",
            ClefType.Tenor  => "𝄡",
            _               => "𝄞"
        };

        double fontSize = clef switch
        {
            ClefType.Treble => sp * 4.5,
            ClefType.Bass   => sp * 2.5,
            _               => sp * 2.5
        };
        double yOff = clef == ClefType.Treble ? -sp * 1.8 : -sp * 0.2;

        var ft = new FormattedText(symbol,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, MusicTypeface, fontSize, ClefBrush);
        ctx.DrawText(ft, new Point(x, y + yOff));
    }

    // ── Key Signature ─────────────────────────────────────────────────

    private static readonly int[] SharpPositionsTreble = { 8, 5, 9, 6, 3, 7, 4 }; // staff positions
    private static readonly int[] FlatPositionsTreble  = { 4, 7, 3, 6, 2, 5, 1 };
    private static readonly int[] SharpPositionsBass   = { 6, 3, 7, 4, 1, 5, 2 };
    private static readonly int[] FlatPositionsBass    = { 2, 5, 1, 4, 0, 3, -1 };

    private static void DrawKeySignature(DrawingContext ctx, KeySignature ks, ClefType clef, double x, double y, double sp)
    {
        string symbol = ks.Fifths > 0 ? "♯" : "♭";
        int[] positions = (ks.Fifths > 0)
            ? (clef == ClefType.Bass ? SharpPositionsBass : SharpPositionsTreble)
            : (clef == ClefType.Bass ? FlatPositionsBass  : FlatPositionsTreble);

        int count = Math.Abs(ks.Fifths);
        for (int i = 0; i < count; i++)
        {
            int pos = i < positions.Length ? positions[i] : 5;
            double ny = y + NoteYFromPos(pos, sp);
            var ft = new FormattedText(symbol,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, MusicTypeface, sp * 1.3, AccBrush);
            ctx.DrawText(ft, new Point(x + i * 8 * sp / 10.0, ny - sp * 0.7));
        }
    }

    // ── Time Signature ────────────────────────────────────────────────

    private static void DrawTimeSignature(DrawingContext ctx, TimeSignature ts, double x, double y, double sp)
    {
        double fontSize = sp * 1.9;
        var ftNum = new FormattedText(ts.Numerator.ToString(),
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, BoldTypeface, fontSize, NoteBrush);
        var ftDen = new FormattedText(ts.Denominator.ToString(),
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, BoldTypeface, fontSize, NoteBrush);
        ctx.DrawText(ftNum, new Point(x, y));
        ctx.DrawText(ftDen, new Point(x, y + sp * 2));
    }

    // ── Barlines ─────────────────────────────────────────────────────

    private static void DrawBarline(DrawingContext ctx, BarlineType type,
        double x, double y, double h, double sp, bool isStart)
    {
        switch (type)
        {
            case BarlineType.Single:
                ctx.DrawLine(BarlinePen, new Point(x, y), new Point(x, y + h));
                break;
            case BarlineType.Double:
                ctx.DrawLine(BarlinePen, new Point(x - 2, y), new Point(x - 2, y + h));
                ctx.DrawLine(BarlinePen, new Point(x, y), new Point(x, y + h));
                break;
            case BarlineType.Final:
                ctx.DrawLine(BarlinePen, new Point(x - 3, y), new Point(x - 3, y + h));
                ctx.DrawLine(FinalBarPen, new Point(x, y), new Point(x, y + h));
                break;
            case BarlineType.RepeatEnd:
                ctx.DrawLine(BarlinePen, new Point(x - 3, y), new Point(x - 3, y + h));
                ctx.DrawLine(FinalBarPen, new Point(x, y), new Point(x, y + h));
                // Dots
                DrawRepeatDots(ctx, x - 7, y, h, sp, right: true);
                break;
            case BarlineType.RepeatStart:
                ctx.DrawLine(FinalBarPen, new Point(x, y), new Point(x, y + h));
                ctx.DrawLine(BarlinePen, new Point(x + 3, y), new Point(x + 3, y + h));
                DrawRepeatDots(ctx, x + 6, y, h, sp, right: false);
                break;
        }
    }

    private static void DrawRepeatDots(DrawingContext ctx, double x, double y, double h, double sp, bool right)
    {
        double r = sp * 0.2;
        ctx.DrawEllipse(NoteBrush, null, new Point(x, y + h * 0.35), r, r);
        ctx.DrawEllipse(NoteBrush, null, new Point(x, y + h * 0.65), r, r);
    }

    // ── Note Element ─────────────────────────────────────────────────

    private void DrawNoteElement(DrawingContext ctx, RenderedNoteElement elem,
        double staffTop, double staffHeight, double sp)
    {
        if (elem.IsRest)
        {
            DrawRest(ctx, elem, staffTop, sp);
            return;
        }

        double rx = sp * 0.55;
        double ry = sp * 0.40;
        double cx = elem.X;
        double cy = elem.Y;

        // Ledger lines
        if (elem.NeedsLedgerLines)
        {
            double lw = rx + sp * 0.4;
            for (int i = 0; i < elem.LedgerLineCount; i++)
            {
                double lyd = elem.LedgerLinesAbove
                    ? cy - i * (sp / 2.0)
                    : cy + i * (sp / 2.0);
                ctx.DrawLine(LedgerPen,
                    new Point(cx - lw, lyd),
                    new Point(cx + lw, lyd));
            }
        }

        // Accidental
        if (elem.ShowAccidental)
            DrawAccidental(ctx, elem.Accidental, cx - rx - 2, cy, sp);

        // Notehead fill
        bool isFilled = elem.NoteValue is not (NoteValue.Whole or NoteValue.Half or NoteValue.Breve);
        bool isSelected = elem.NoteId == SelectedNoteId;

        IBrush fill = isSelected ? SelectBrush
            : ShowHandColoring && elem.Hand != Hand.Unassigned
                ? (elem.Hand == Hand.Right ? RightHandBrush : LeftHandBrush)
                : (isFilled ? NoteBrush : Brushes.Transparent);

        ctx.DrawEllipse(fill, NoteOutline, new Point(cx, cy), rx, ry);

        // Open notehead inner hole (for half note — a slightly rotated oval)
        if (elem.NoteValue == NoteValue.Half && !isSelected)
        {
            ctx.DrawEllipse(PageBrush, null, new Point(cx, cy), rx * 0.55, ry * 0.42);
        }

        // Augmentation dots
        for (int d = 0; d < elem.Dots; d++)
        {
            double dotX = cx + rx + sp * 0.35 + d * sp * 0.4;
            double dotY = elem.StaffPosition % 2 == 0 ? cy - sp * 0.25 : cy;
            ctx.DrawEllipse(NoteBrush, null, new Point(dotX, dotY), sp * 0.13, sp * 0.13);
        }

        // Stem
        if (elem.Stem is StemDirection.Up or StemDirection.Down)
        {
            double stemX = elem.Stem == StemDirection.Up ? cx + rx : cx - rx;
            ctx.DrawLine(StemPen,
                new Point(stemX, cy),
                new Point(stemX, elem.StemEndY));

            // Flag (for unbeamed 8th/16th/32nd)
            if (elem.BeamGroup == 0 && elem.NoteValue is NoteValue.Eighth or NoteValue.Sixteenth or NoteValue.ThirtySecond)
                DrawFlag(ctx, stemX, elem.StemEndY, elem.Stem, elem.NoteValue, sp);
        }

        // Articulation
        if (elem.Articulation != Articulation.None)
            DrawArticulation(ctx, elem, staffTop, sp);

        // Lyrics
        double lyricY = staffTop + staffHeight + sp * 0.6;
        foreach (var (verse, text, syllable) in elem.Lyrics)
        {
            string lyricText = syllable switch
            {
                LyricSyllable.Begin  => text + "-",
                LyricSyllable.Middle => "-" + text + "-",
                LyricSyllable.End    => "-" + text,
                _                    => text
            };
            var ft = new FormattedText(lyricText,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, ItalicTypeface, sp * 1.0, LyricBrush);
            ctx.DrawText(ft, new Point(cx - ft.Width / 2, lyricY + (verse - 1) * sp * 1.4));
        }

        // Chord notes
        foreach (var (chordPos, showAcc, acc) in elem.ChordPositions)
        {
            double ccy = staffTop + NoteYFromPos(chordPos, sp);
            if (showAcc) DrawAccidental(ctx, acc, cx - rx - 2, ccy, sp);
            ctx.DrawEllipse(fill, NoteOutline, new Point(cx, ccy), rx, ry);
        }
    }

    // ── Rest ─────────────────────────────────────────────────────────

    private static void DrawRest(DrawingContext ctx, RenderedNoteElement elem,
        double staffTop, double sp)
    {
        double cx = elem.X;
        double midY = staffTop + sp * 2; // middle of staff

        switch (elem.NoteValue)
        {
            case NoteValue.Whole:
                // Whole rest: filled rectangle hanging from 4th line
                ctx.FillRectangle(RestBrush,
                    new Rect(cx - sp * 0.6, midY - sp * 0.5, sp * 1.2, sp * 0.5));
                break;
            case NoteValue.Half:
                // Half rest: filled rectangle sitting on 3rd line
                ctx.FillRectangle(RestBrush,
                    new Rect(cx - sp * 0.6, midY, sp * 1.2, sp * 0.5));
                break;
            case NoteValue.Quarter:
                // Quarter rest: draw approximate symbol
                DrawQuarterRest(ctx, cx, midY, sp);
                break;
            case NoteValue.Eighth:
                ctx.DrawEllipse(RestBrush, null, new Point(cx, midY + sp * 0.3), sp * 0.25, sp * 0.25);
                ctx.DrawLine(StemPen, new Point(cx, midY + sp * 0.3), new Point(cx + sp * 0.5, midY - sp * 1.5));
                break;
            case NoteValue.Sixteenth:
                ctx.DrawEllipse(RestBrush, null, new Point(cx, midY + sp * 0.3), sp * 0.25, sp * 0.25);
                ctx.DrawLine(StemPen, new Point(cx, midY + sp * 0.3), new Point(cx + sp * 0.5, midY - sp * 1.5));
                ctx.DrawEllipse(RestBrush, null, new Point(cx + sp * 0.2, midY - sp * 0.5), sp * 0.2, sp * 0.2);
                break;
            default:
                // Fallback: small filled rect
                ctx.FillRectangle(RestBrush, new Rect(cx - sp * 0.4, midY - sp * 0.3, sp * 0.8, sp * 0.6));
                break;
        }

        // Dots
        for (int d = 0; d < elem.Dots; d++)
            ctx.DrawEllipse(RestBrush, null,
                new Point(cx + sp * 0.7 + d * sp * 0.4, midY - sp * 0.1), sp * 0.12, sp * 0.12);
    }

    private static void DrawQuarterRest(DrawingContext ctx, double cx, double midY, double sp)
    {
        // Simplified quarter rest using a zig-zag path
        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            gc.BeginFigure(new Point(cx + sp * 0.2, midY - sp * 1.0), false);
            gc.LineTo(new Point(cx - sp * 0.1, midY - sp * 0.5));
            gc.LineTo(new Point(cx + sp * 0.3, midY));
            gc.LineTo(new Point(cx - sp * 0.2, midY + sp * 0.5));
            gc.LineTo(new Point(cx + sp * 0.1, midY + sp * 1.0));
        }
        ctx.DrawGeometry(null, StemPen, geo);
    }

    // ── Flag ─────────────────────────────────────────────────────────

    private static void DrawFlag(DrawingContext ctx, double stemX, double stemTipY,
        StemDirection dir, NoteValue value, double sp)
    {
        // Draw flag curve from stem tip
        int flagCount = value switch
        {
            NoteValue.Eighth       => 1,
            NoteValue.Sixteenth    => 2,
            NoteValue.ThirtySecond => 3,
            _                      => 1
        };

        for (int f = 0; f < flagCount; f++)
        {
            double fy = stemTipY + (dir == StemDirection.Up ? f * sp * 0.8 : -f * sp * 0.8);
            double cpX = stemX + sp * 1.0;
            double cpY = fy + (dir == StemDirection.Up ? sp * 1.2 : -sp * 1.2);
            double endX = stemX + sp * 0.3;
            double endY = fy + (dir == StemDirection.Up ? sp * 0.6 : -sp * 0.6);

            var geo = new StreamGeometry();
            using (var gc = geo.Open())
            {
                gc.BeginFigure(new Point(stemX, fy), false);
                gc.CubicBezierTo(new Point(cpX, fy), new Point(cpX, cpY), new Point(endX, endY));
            }
            ctx.DrawGeometry(null, StemPen, geo);
        }
    }

    // ── Accidental ───────────────────────────────────────────────────

    private static void DrawAccidental(DrawingContext ctx, Accidental acc,
        double x, double cy, double sp)
    {
        string sym = acc switch
        {
            Accidental.Sharp       => "♯",
            Accidental.Flat        => "♭",
            Accidental.Natural     => "♮",
            Accidental.DoubleSharp => "𝄪",
            Accidental.DoubleFlat  => "𝄫",
            _                      => ""
        };
        if (sym.Length == 0) return;
        var ft = new FormattedText(sym,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, MusicTypeface, sp * 1.4, AccBrush);
        ctx.DrawText(ft, new Point(x - ft.Width, cy - ft.Height * 0.5));
    }

    // ── Articulation ─────────────────────────────────────────────────

    private static void DrawArticulation(DrawingContext ctx, RenderedNoteElement elem,
        double staffTop, double sp)
    {
        bool above = elem.Stem == StemDirection.Down;
        double ay = above ? elem.Y - sp * 1.4 : elem.Y + sp * 1.2;

        switch (elem.Articulation)
        {
            case Articulation.Staccato:
                ctx.DrawEllipse(NoteBrush, null,
                    new Point(elem.X, ay), sp * 0.15, sp * 0.15);
                break;
            case Articulation.Accent:
                DrawAccentMark(ctx, elem.X, ay, sp, above);
                break;
            case Articulation.Tenuto:
                ctx.DrawLine(StemPen,
                    new Point(elem.X - sp * 0.5, ay),
                    new Point(elem.X + sp * 0.5, ay));
                break;
            case Articulation.Fermata:
                DrawFermata(ctx, elem.X, ay, sp, above);
                break;
            case Articulation.Marcato:
                DrawMarcato(ctx, elem.X, ay, sp, above);
                break;
        }
    }

    private static void DrawAccentMark(DrawingContext ctx, double cx, double y, double sp, bool above)
    {
        double dir = above ? -1 : 1;
        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            gc.BeginFigure(new Point(cx - sp * 0.5, y), false);
            gc.LineTo(new Point(cx, y + dir * sp * 0.4));
            gc.LineTo(new Point(cx + sp * 0.5, y));
        }
        ctx.DrawGeometry(null, StemPen, geo);
    }

    private static void DrawFermata(DrawingContext ctx, double cx, double y, double sp, bool above)
    {
        // Dot + arc
        ctx.DrawEllipse(NoteBrush, null, new Point(cx, y), sp * 0.12, sp * 0.12);
        var arc = new StreamGeometry();
        using (var gc = arc.Open())
        {
            gc.BeginFigure(new Point(cx - sp * 0.5, y + (above ? sp * 0.2 : -sp * 0.2)), false);
            gc.CubicBezierTo(
                new Point(cx - sp * 0.25, y + (above ? -sp * 0.5 : sp * 0.5)),
                new Point(cx + sp * 0.25, y + (above ? -sp * 0.5 : sp * 0.5)),
                new Point(cx + sp * 0.5,  y + (above ? sp * 0.2 : -sp * 0.2)));
        }
        ctx.DrawGeometry(null, SlurPen, arc);
    }

    private static void DrawMarcato(DrawingContext ctx, double cx, double y, double sp, bool above)
    {
        double dir = above ? -1 : 1;
        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            gc.BeginFigure(new Point(cx - sp * 0.4, y), false);
            gc.LineTo(new Point(cx, y + dir * sp * 0.7));
            gc.LineTo(new Point(cx + sp * 0.4, y));
        }
        ctx.DrawGeometry(null, StemPen, geo);
    }

    // ── Beam ─────────────────────────────────────────────────────────

    private static void DrawBeam(DrawingContext ctx, RenderedBeam beam, double sp)
    {
        double thickness = sp * 0.35;
        double yOffset = beam.BeamLevel * (thickness + sp * 0.15);

        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            gc.BeginFigure(new Point(beam.StartX, beam.StartY + yOffset), true);
            gc.LineTo(new Point(beam.EndX, beam.EndY + yOffset));
            gc.LineTo(new Point(beam.EndX, beam.EndY + yOffset + thickness));
            gc.LineTo(new Point(beam.StartX, beam.StartY + yOffset + thickness));
        }
        ctx.DrawGeometry(NoteBrush, null, geo);
    }

    // ── Dynamic ──────────────────────────────────────────────────────

    private static void DrawDynamic(DrawingContext ctx, RenderedDynamic dyn, double sp)
    {
        var ft = new FormattedText(dyn.Symbol,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, ItalicTypeface, sp * 1.4, DynBrush);
        ctx.DrawText(ft, new Point(dyn.X, dyn.Y));
    }

    // ── Hairpin ──────────────────────────────────────────────────────

    private static void DrawHairpin(DrawingContext ctx, RenderedHairpin hp, double sp)
    {
        double openW = sp * 0.6;
        if (hp.Type == HairpinType.Crescendo)
        {
            ctx.DrawLine(HairpinPen, new Point(hp.StartX, hp.Y), new Point(hp.EndX, hp.Y - openW));
            ctx.DrawLine(HairpinPen, new Point(hp.StartX, hp.Y), new Point(hp.EndX, hp.Y + openW));
        }
        else
        {
            ctx.DrawLine(HairpinPen, new Point(hp.StartX, hp.Y - openW), new Point(hp.EndX, hp.Y));
            ctx.DrawLine(HairpinPen, new Point(hp.StartX, hp.Y + openW), new Point(hp.EndX, hp.Y));
        }
    }

    // ── Slur ─────────────────────────────────────────────────────────

    private static void DrawSlur(DrawingContext ctx, RenderedSlur slur, double sp)
    {
        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            gc.BeginFigure(new Point(slur.StartX, slur.StartY), false);
            gc.CubicBezierTo(
                new Point(slur.Cp1X, slur.Cp1Y),
                new Point(slur.Cp2X, slur.Cp2Y),
                new Point(slur.EndX, slur.EndY));
        }
        ctx.DrawGeometry(null, SlurPen, geo);
    }

    // ── Tempo ─────────────────────────────────────────────────────────

    private static void DrawTempo(DrawingContext ctx, RenderedTempo tempo, double sp)
    {
        string text = tempo.BPM.HasValue ? $"{tempo.Text}  ♩ = {tempo.BPM}" : tempo.Text;
        var ft = new FormattedText(text,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, BoldTypeface, sp * 1.1, TempoBrush);
        ctx.DrawText(ft, new Point(tempo.X, tempo.Y));
    }

    // ── Input ─────────────────────────────────────────────────────────

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(this);
        var layout = LayoutResult;
        if (layout is null) return;

        foreach (var page in layout.Pages)
        foreach (var system in page.Systems)
        foreach (var staff in system.Staves)
        {
            double sp = staff.Height / 4.0;
            double hitR = sp * 0.7;

            foreach (var measure in staff.Measures)
            {
                // Note hit test
                foreach (var elem in measure.Elements)
                {
                    if (Math.Abs(pos.X - elem.X) < hitR && Math.Abs(pos.Y - elem.Y) < hitR * 0.7)
                    {
                        NoteClicked?.Invoke(this, new NoteClickedEventArgs(
                            elem.NoteId, measure.MeasureNumber, staff.StaffId));
                        SetValue(SelectedNoteIdProperty, elem.NoteId);
                        return;
                    }
                }

                // Staff click → note entry (use actual clef/key from this measure)
                if (pos.X >= measure.X && pos.X < measure.X + measure.Width
                    && pos.Y >= staff.Y && pos.Y <= staff.Y + staff.Height)
                {
                    int staffPos = YToStaffPosition(pos.Y, staff.Y, sp);
                    Clef clef = measure.ClefType switch
                    {
                        ClefType.Bass  => Clef.Bass,
                        ClefType.Alto  => Clef.Alto,
                        ClefType.Tenor => Clef.Tenor,
                        _              => Clef.Treble
                    };
                    StaffPositionClicked?.Invoke(this, new StaffPositionClickedEventArgs(
                        staff.StaffId, measure.MeasureNumber, staffPos,
                        clef, measure.KeySignature));
                    return;
                }
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static double NoteYFromPos(int pos, double sp) => (9 - pos) * (sp / 2.0);
    private static int YToStaffPosition(double y, double staffTop, double sp) =>
        9 - (int)Math.Round((y - staffTop) / (sp / 2.0));
}
