using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.NotationEngine;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class ScoreEditorViewModel : ViewModelBase
{
    private readonly ILayoutEngine _layout;

    [ObservableProperty] private ScoreEditor? _editor;
    [ObservableProperty] private LayoutResult? _layoutResult;
    [ObservableProperty] private double _zoom = 1.0;

    // Current input state
    [ObservableProperty] private Duration _selectedDuration = Duration.Quarter;
    [ObservableProperty] private bool _inputRest;
    [ObservableProperty] private bool _dotted;
    [ObservableProperty] private Hand _selectedHand = Hand.Unassigned;
    [ObservableProperty] private bool _showHandColoring;

    // Selection
    [ObservableProperty] private Guid? _selectedNoteId;
    [ObservableProperty] private int _selectedMeasure = 1;
    [ObservableProperty] private Guid _selectedStaffId;

    public ScoreEditorViewModel(ILayoutEngine layout) => _layout = layout;

    public void Initialize(ScoreEditor editor)
    {
        Editor = editor;
        Editor.ScoreChanged += (_, _) => RefreshLayout();
        RefreshLayout();
    }

    private void RefreshLayout()
    {
        if (Editor is null) return;
        var opts = new LayoutOptions { Zoom = Zoom };
        LayoutResult = _layout.ComputeLayout(Editor.Score, opts);
    }

    [RelayCommand]
    private void SelectDuration(NoteValue value) =>
        SelectedDuration = new Duration(value, Dotted ? 1 : 0);

    [RelayCommand]
    private void ToggleDot()
    {
        Dotted = !Dotted;
        SelectedDuration = new Duration(SelectedDuration.Value, Dotted ? 1 : 0);
    }

    [RelayCommand]
    private void ToggleRest() => InputRest = !InputRest;

    /// <summary>Called when the user clicks a staff position to enter a note.</summary>
    public void EnterNoteAtPosition(Guid staffId, int measureNumber, int staffPosition, Clef clef, KeySignature keySig)
    {
        if (Editor is null) return;

        var note = new Note
        {
            Duration = SelectedDuration,
            StaffPosition = staffPosition,
            Hand = SelectedHand
        };

        if (!InputRest)
            note.Pitch = StaffPositionCalculator.FromStaffPosition(staffPosition, clef, keySig);

        Editor.AddNote(staffId, measureNumber, note);
        SelectedNoteId = note.Id;
    }

    [RelayCommand]
    private void DeleteSelectedNote()
    {
        if (Editor is null || SelectedNoteId is null) return;
        Editor.DeleteNote(SelectedStaffId, SelectedMeasure, SelectedNoteId.Value);
        SelectedNoteId = null;
    }
}
