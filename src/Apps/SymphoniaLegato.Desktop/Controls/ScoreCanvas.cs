using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Desktop.Controls;

/// <summary>Event args for when user clicks on an existing note.</summary>
public sealed class NoteClickedEventArgs(Guid noteId, int measureNumber, Guid staffId) : EventArgs
{
    public Guid NoteId { get; } = noteId;
    public int MeasureNumber { get; } = measureNumber;
    public Guid StaffId { get; } = staffId;
}

/// <summary>Event args for when user clicks empty staff space to enter a note.</summary>
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
/// Custom Avalonia control that renders a music score using Avalonia's DrawingContext.
/// Handles mouse/touch input for note entry and selection.
/// </summary>
public sealed class ScoreCanvas : Control
{
    // ── Avalonia Properties ────────────────────────────────────────────

    public static readonly StyledProperty<LayoutResult?> LayoutResultProperty =
        AvaloniaProperty.Register<ScoreCanvas, LayoutResult?>(nameof(LayoutResult));

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<ScoreCanvas, double>(nameof(Zoom), 1.0);

    public static readonly StyledProperty<Guid?> SelectedNoteIdProperty =
        AvaloniaProperty.Register<ScoreCanvas, Guid?>(nameof(SelectedNoteId));

    public static readonly StyledProperty<bool> ShowHandColoringProperty =
        AvaloniaProperty.Register<ScoreCanvas, bool>(nameof(ShowHandColoring));

    public LayoutResult? LayoutResult
    {
        get => GetValue(LayoutResultProperty);
        set => SetValue(LayoutResultProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public Guid? SelectedNoteId
    {
        get => GetValue(SelectedNoteIdProperty);
        set => SetValue(SelectedNoteIdProperty, value);
    }

    public bool ShowHandColoring
    {
        get => GetValue(ShowHandColoringProperty);
        set => SetValue(ShowHandColoringProperty, value);
    }

    // ── Events ────────────────────────────────────────────────────────

    public event EventHandler<NoteClickedEventArgs>? NoteClicked;
    public event EventHandler<StaffPositionClickedEventArgs>? StaffPositionClicked;

    // ── Pens & Brushes ────────────────────────────────────────────────

    private static readonly Pen StaffPen    = new(Brushes.Gray, 0.8);
    private static readonly Pen BarlinePen  = new(Brushes.DarkGray, 1.5);
    private static readonly Pen LedgerPen   = new(Brushes.Gray, 0.8);
    private static readonly Pen NoteOutline = new(Brushes.LightGray, 1.5);

    private static readonly IBrush NoteWhite    = new SolidColorBrush(Color.FromRgb(230, 230, 230));
    private static readonly IBrush RightHand    = new SolidColorBrush(Color.FromRgb(100, 149, 237));
    private static readonly IBrush LeftHand     = new SolidColorBrush(Color.FromRgb(205, 92, 92));
    private static readonly IBrush SelectionBrush = new SolidColorBrush(Color.FromArgb(160, 100, 149, 237));
    private static readonly IBrush PageBrush    = new SolidColorBrush(Color.FromRgb(38, 38, 38));
    private static readonly IBrush MeasureNumBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));

    private static readonly Typeface MeasureNumFont = new(FontFamily.Default);

    // ── Constructor ───────────────────────────────────────────────────

    public ScoreCanvas()
    {
        LayoutResultProperty.Changed.AddClassHandler<ScoreCanvas>((s, _) => s.InvalidateVisual());
        ZoomProperty.Changed.AddClassHandler<ScoreCanvas>((s, _) => s.InvalidateVisual());
        SelectedNoteIdProperty.Changed.AddClassHandler<ScoreCanvas>((s, _) => s.InvalidateVisual());
        ShowHandColoringProperty.Changed.AddClassHandler<ScoreCanvas>((s, _) => s.InvalidateVisual());

        this.AddHandler(PointerPressedEvent, OnPointerPressed);
    }

    // ── Rendering ─────────────────────────────────────────────────────

    public override void Render(DrawingContext ctx)
    {
        var layout = LayoutResult;
        if (layout is null)
        {
            ctx.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
            return;
        }

        foreach (var page in layout.Pages)
            DrawPage(ctx, page);
    }

    private void DrawPage(DrawingContext ctx, RenderedPage page)
    {
        double px = page.WidthPx * 0.05;
        double py = 20;
        double pw = page.WidthPx * 0.90;
        double ph = page.HeightPx - 40;
        ctx.FillRectangle(PageBrush, new Rect(px, py, pw, ph));

        foreach (var system in page.Systems)
            DrawSystem(ctx, system);
    }

    private void DrawSystem(DrawingContext ctx, RenderedSystem system)
    {
        foreach (var staff in system.Staves)
            DrawStaff(ctx, staff);
    }

    private void DrawStaff(DrawingContext ctx, RenderedStaff staff)
    {
        if (staff.Measures.Count == 0) return;

        double startX = staff.Measures[0].X;
        double endX   = staff.Measures[^1].X + staff.Measures[^1].Width;
        double lineSpacing = staff.Height / 4.0;

        // 5 staff lines
        for (int line = 0; line < 5; line++)
        {
            double y = staff.Y + line * lineSpacing;
            ctx.DrawLine(StaffPen, new Point(startX, y), new Point(endX, y));
        }

        foreach (var measure in staff.Measures)
            DrawMeasure(ctx, measure, staff, lineSpacing);
    }

    private void DrawMeasure(DrawingContext ctx, RenderedMeasure measure, RenderedStaff staff, double lineSpacing)
    {
        double x = measure.X;
        double y = staff.Y;
        double h = staff.Height;

        // Barline
        ctx.DrawLine(BarlinePen, new Point(x + measure.Width, y), new Point(x + measure.Width, y + h));

        // Measure number
        var ft = new FormattedText(
            measure.MeasureNumber.ToString(),
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            MeasureNumFont, 10, MeasureNumBrush);
        ctx.DrawText(ft, new Point(x + 2, y - 14));

        foreach (var elem in measure.Elements)
            DrawNote(ctx, elem, lineSpacing);
    }

    private void DrawNote(DrawingContext ctx, RenderedNoteElement elem, double lineSpacing)
    {
        double rx = lineSpacing * 0.55;
        double ry = lineSpacing * 0.40;
        double cx = elem.X;
        double cy = elem.Y;

        // Ledger lines
        if (elem.NeedsLedgerLines)
        {
            double lw = rx + 4;
            for (int l = 0; l < elem.LedgerLineCount; l++)
            {
                double ly = elem.StaffPosition < 1
                    ? cy + l * (lineSpacing / 2)
                    : cy - l * (lineSpacing / 2);
                ctx.DrawLine(LedgerPen, new Point(cx - lw, ly), new Point(cx + lw, ly));
            }
        }

        // Note head
        bool isSelected = elem.NoteId == SelectedNoteId;
        var fill = isSelected ? SelectionBrush : NoteWhite;

        ctx.DrawEllipse(fill, NoteOutline, new Point(cx, cy), rx, ry);
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
            double lineSpacing = staff.Height / 4.0;

            foreach (var measure in staff.Measures)
            foreach (var elem in measure.Elements)
            {
                double dx = Math.Abs(pos.X - elem.X);
                double dy = Math.Abs(pos.Y - elem.Y);
                if (dx < lineSpacing && dy < lineSpacing * 0.6)
                {
                    NoteClicked?.Invoke(this, new NoteClickedEventArgs(
                        elem.NoteId, measure.MeasureNumber, staff.StaffId));
                    SetValue(SelectedNoteIdProperty, elem.NoteId);
                    return;
                }
            }

            // Click on empty staff area → note entry
            if (pos.X >= system.X && pos.X <= system.X + system.Width
                && pos.Y >= staff.Y && pos.Y <= staff.Y + staff.Height)
            {
                int mNum = HitTestMeasure(staff, pos.X);
                int sPos = YToStaffPosition(pos.Y, staff.Y, lineSpacing);
                StaffPositionClicked?.Invoke(this, new StaffPositionClickedEventArgs(
                    staff.StaffId, mNum, sPos, Clef.Treble, KeySignature.CMajor));
                return;
            }
        }
    }

    private static int HitTestMeasure(RenderedStaff staff, double x)
    {
        foreach (var m in staff.Measures)
            if (x >= m.X && x < m.X + m.Width)
                return m.MeasureNumber;
        return 1;
    }

    private static int YToStaffPosition(double y, double staffTop, double lineSpacing) =>
        9 - (int)Math.Round((y - staffTop) / (lineSpacing / 2.0));
}
