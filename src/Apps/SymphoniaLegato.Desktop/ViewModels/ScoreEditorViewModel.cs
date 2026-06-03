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

    // Input state
    [ObservableProperty] private Duration _selectedDuration = Duration.Quarter;
    [ObservableProperty] private bool _inputRest;
    [ObservableProperty] private bool _dotted;
    [ObservableProperty] private bool _inputMode = true;   // true = note entry, false = selection
    [ObservableProperty] private bool _showHandColoring;
    [ObservableProperty] private int _selectedHandIndex;   // 0=Both,1=Right,2=Left

    // Accidental override (applied to the next entered note, then cleared)
    [ObservableProperty] private Accidental? _accidentalOverride;

    // Slur/tie entry state
    [ObservableProperty] private bool _enteringSlur;
    [ObservableProperty] private bool _enteringTie;

    // Selection
    [ObservableProperty] private Guid? _selectedNoteId;
    [ObservableProperty] private int _selectedMeasure = 1;
    [ObservableProperty] private Guid _selectedStaffId;

    // Current effective key/clef for note entry
    private KeySignature _activeKeySig = KeySignature.CMajor;
    private Clef _activeClef = Clef.Treble;

    public Hand SelectedHand => SelectedHandIndex switch
    {
        1 => Hand.Right,
        2 => Hand.Left,
        _ => Hand.Unassigned
    };

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

    // ── Mode ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleInputMode() => InputMode = !InputMode;

    // ── Duration ──────────────────────────────────────────────────────

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

    [RelayCommand]
    private void SetAccidentalOverride(Accidental acc)
    {
        // Toggle off if already selected
        AccidentalOverride = AccidentalOverride == acc ? null : acc;
    }

    [RelayCommand]
    private void ToggleSlur() => EnteringSlur = !EnteringSlur;

    [RelayCommand]
    private void ToggleTie() => EnteringTie = !EnteringTie;

    // ── Note entry ────────────────────────────────────────────────────

    public void EnterNoteAtPosition(Guid staffId, int measureNumber,
        int staffPosition, Clef clef, KeySignature keySig)
    {
        if (Editor is null || !InputMode) return;
        _activeClef   = clef;
        _activeKeySig = keySig;

        var note = new Note
        {
            Duration      = SelectedDuration,
            StaffPosition = staffPosition,
            Hand          = SelectedHand
        };

        if (!InputRest)
        {
            var pitch = StaffPositionCalculator.FromStaffPosition(staffPosition, clef, keySig);
            // Apply accidental override if set, then clear it
            if (AccidentalOverride.HasValue)
            {
                pitch = new Pitch(pitch.Name, AccidentalOverride.Value, pitch.Octave);
                AccidentalOverride = null;
            }
            note.Pitch = pitch;
        }

        Editor.AddNote(staffId, measureNumber, note);
        SelectedNoteId  = note.Id;
        SelectedMeasure = measureNumber;
        SelectedStaffId = staffId;
    }

    [RelayCommand]
    private void DeleteSelectedNote()
    {
        if (Editor is null || SelectedNoteId is null) return;
        Editor.DeleteNote(SelectedStaffId, SelectedMeasure, SelectedNoteId.Value);
        SelectedNoteId = null;
    }

    // ── Articulation ─────────────────────────────────────────────────

    [RelayCommand]
    private void SetArticulation(Articulation art)
    {
        if (Editor is null || SelectedNoteId is null) return;
        Editor.SetArticulation(SelectedStaffId, SelectedMeasure, SelectedNoteId.Value, art);
    }

    // ── Dynamics ─────────────────────────────────────────────────────

    [RelayCommand]
    private void AddDynamic(DynamicLevel level)
    {
        if (Editor is null) return;
        var dyn = new Dynamic { Level = level, TickOffset = 0 };
        Editor.AddDynamic(SelectedStaffId, SelectedMeasure, dyn);
    }

    // ── Zoom ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void ZoomIn()  { Zoom = Math.Min(8.0, Zoom * 1.25); RefreshLayout(); }
    [RelayCommand]
    private void ZoomOut() { Zoom = Math.Max(0.25, Zoom / 1.25); RefreshLayout(); }
    [RelayCommand]
    private void ZoomReset() { Zoom = 1.0; RefreshLayout(); }

    partial void OnZoomChanged(double value) { } // RefreshLayout called in commands
}
