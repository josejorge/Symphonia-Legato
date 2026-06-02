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
        vm.SelectedNoteId = e.NoteId;
        vm.SelectedMeasure = e.MeasureNumber;
        vm.SelectedStaffId = e.StaffId;
    }

    private void OnStaffPositionClicked(object? sender, StaffPositionClickedEventArgs e)
    {
        if (DataContext is not ScoreEditorViewModel vm) return;
        vm.EnterNoteAtPosition(e.StaffId, e.MeasureNumber, e.StaffPosition, e.Clef, e.KeySignature);
    }
}
