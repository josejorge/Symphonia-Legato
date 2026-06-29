using Avalonia.Controls;
using SymphoniaLegato.Rendering;
using SymphoniaLegato.Desktop.ViewModels;

namespace SymphoniaLegato.Desktop.Views;

public sealed partial class ScoreEditorView : UserControl
{
    public ScoreEditorView() => InitializeComponent();

    private void OnNoteClicked(object? sender, NoteClickedEventArgs e)
    {
        if (DataContext is not ScoreEditorViewModel vm) return;
        vm.SelectNote(e.StaffId, e.MeasureNumber, e.NoteId);
    }

    private void OnStaffPositionClicked(object? sender, StaffPositionClickedEventArgs e)
    {
        if (DataContext is not ScoreEditorViewModel vm) return;
        vm.EnterNoteAtPosition(e.StaffId, e.MeasureNumber, e.StaffPosition, e.Clef, e.KeySignature);
    }

    // Auto-scroll vertically so the playback cursor stays comfortably in view.
    private void OnPlaybackCursorMoved(object? sender, double cursorCenterY)
    {
        if (Scroller is null) return;
        double viewTop    = Scroller.Offset.Y;
        double viewHeight  = Scroller.Viewport.Height;
        double margin      = viewHeight * 0.2;

        double target = viewTop;
        if (cursorCenterY < viewTop + margin)
            target = cursorCenterY - margin;
        else if (cursorCenterY > viewTop + viewHeight - margin)
            target = cursorCenterY - viewHeight + margin;

        double maxY = Math.Max(0, Scroller.Extent.Height - viewHeight);
        target = Math.Clamp(target, 0, maxY);
        if (Math.Abs(target - viewTop) > 1)
            Scroller.Offset = Scroller.Offset.WithY(target);
    }
}
