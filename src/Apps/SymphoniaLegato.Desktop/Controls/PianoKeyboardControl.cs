using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SymphoniaLegato.Desktop.ViewModels;

namespace SymphoniaLegato.Desktop.Controls;

public sealed class PianoKeyPressedEventArgs(PianoKey key) : EventArgs
{
    public PianoKey Key { get; } = key;
}

/// <summary>
/// Custom piano keyboard rendered with Avalonia's DrawingContext.
/// Draws white + black keys with hover and press highlights.
/// </summary>
public sealed class PianoKeyboardControl : Control
{
    public static readonly StyledProperty<System.Collections.ObjectModel.ObservableCollection<PianoKey>?> KeysProperty =
        AvaloniaProperty.Register<PianoKeyboardControl, System.Collections.ObjectModel.ObservableCollection<PianoKey>?>(nameof(Keys));

    public System.Collections.ObjectModel.ObservableCollection<PianoKey>? Keys
    {
        get => GetValue(KeysProperty);
        set => SetValue(KeysProperty, value);
    }

    public event EventHandler<PianoKeyPressedEventArgs>? KeyPressed;

    private PianoKey? _hoveredKey;
    private PianoKey? _pressedKey;

    private static readonly IBrush WhiteKeyBrush   = new SolidColorBrush(Color.FromRgb(238, 238, 234));
    private static readonly IBrush BlackKeyBrush   = new SolidColorBrush(Color.FromRgb(25, 25, 25));
    private static readonly IBrush PressedWhite    = new SolidColorBrush(Color.FromRgb(100, 149, 237));
    private static readonly IBrush PressedBlack    = new SolidColorBrush(Color.FromRgb(60, 100, 180));
    private static readonly IBrush HoverBrush      = new SolidColorBrush(Color.FromArgb(80, 200, 220, 255));
    private static readonly IBrush HighlightBrush  = new SolidColorBrush(Color.FromArgb(200, 100, 200, 100));
    private static readonly Pen   KeyBorderPen     = new(Brushes.Gray, 0.8);

    public PianoKeyboardControl()
    {
        KeysProperty.Changed.AddClassHandler<PianoKeyboardControl>((s, _) => s.InvalidateVisual());
        this.AddHandler(PointerMovedEvent,    OnPointerMoved);
        this.AddHandler(PointerPressedEvent,  OnPointerPressed);
        this.AddHandler(PointerReleasedEvent, OnPointerReleased);
        this.AddHandler(PointerExitedEvent,   OnPointerExited);
    }

    public override void Render(DrawingContext ctx)
    {
        var keys = Keys;
        if (keys is null || keys.Count == 0) return;

        double w = Bounds.Width;
        double h = Bounds.Height;

        var whites = keys.Where(k => !k.IsBlack).ToList();
        if (whites.Count == 0) return;

        double wKeyW = w / whites.Count;
        double bKeyW = wKeyW * 0.6;
        double bKeyH = h * 0.62;

        // White keys
        for (int i = 0; i < whites.Count; i++)
        {
            var key = whites[i];
            var rect = new Rect(i * wKeyW + 0.5, 0, wKeyW - 1, h - 0.5);
            IBrush fill = key.IsPressed     ? PressedWhite
                        : key.IsHighlighted ? HighlightBrush
                        : WhiteKeyBrush;
            ctx.FillRectangle(fill, rect);
            ctx.DrawRectangle(KeyBorderPen, rect);
            if (_hoveredKey == key)
                ctx.FillRectangle(HoverBrush, rect);
        }

        // Black keys
        for (int i = 0; i < whites.Count - 1; i++)
        {
            var white = whites[i];
            var blackKey = keys.FirstOrDefault(k =>
                k.IsBlack && k.Pitch.MidiNumber == white.Pitch.MidiNumber + 1);
            if (blackKey is null) continue;

            double bx = (i + 0.65) * wKeyW;
            var bRect = new Rect(bx, 0, bKeyW, bKeyH);
            IBrush fill = blackKey.IsPressed ? PressedBlack : BlackKeyBrush;
            ctx.FillRectangle(fill, bRect);
            if (_hoveredKey == blackKey)
                ctx.FillRectangle(HoverBrush, bRect);
        }
    }

    private PianoKey? HitTest(Point pos)
    {
        var keys = Keys;
        if (keys is null) return null;

        var whites = keys.Where(k => !k.IsBlack).ToList();
        if (whites.Count == 0) return null;

        double w = Bounds.Width;
        double h = Bounds.Height;
        double wKeyW = w / whites.Count;
        double bKeyW = wKeyW * 0.6;
        double bKeyH = h * 0.62;

        if (pos.Y < bKeyH)
        {
            for (int i = 0; i < whites.Count - 1; i++)
            {
                double bx = (i + 0.65) * wKeyW;
                if (pos.X >= bx && pos.X <= bx + bKeyW)
                {
                    var white = whites[i];
                    return keys.FirstOrDefault(k =>
                        k.IsBlack && k.Pitch.MidiNumber == white.Pitch.MidiNumber + 1);
                }
            }
        }

        int idx = (int)(pos.X / wKeyW);
        return idx >= 0 && idx < whites.Count ? whites[idx] : null;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        _hoveredKey = HitTest(e.GetPosition(this));
        InvalidateVisual();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _pressedKey = HitTest(e.GetPosition(this));
        if (_pressedKey is not null)
        {
            _pressedKey.IsPressed = true;
            KeyPressed?.Invoke(this, new PianoKeyPressedEventArgs(_pressedKey));
            InvalidateVisual();
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_pressedKey is not null)
        {
            _pressedKey.IsPressed = false;
            _pressedKey = null;
            InvalidateVisual();
        }
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        _hoveredKey = null;
        InvalidateVisual();
    }
}
