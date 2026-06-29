using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.NotationEngine;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class ScoreEditorViewModel : ViewModelBase
{
    private readonly ILayoutEngine _layout;
    private readonly IPlaybackEngine _playback;

    [ObservableProperty] private ScoreEditor? _editor;
    [ObservableProperty] private LayoutResult? _layoutResult;
    [ObservableProperty] private double _zoom = 1.0;

    /// <summary>Absolute domain tick of the playback cursor; &lt; 0 = hidden.</summary>
    [ObservableProperty] private double _playbackTick = -1.0;

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

    /// <summary>Raised when a note is selected, carrying its absolute tick so playback can start there.</summary>
    public event EventHandler<double>? PlaybackStartTickChanged;

    public ScoreEditorViewModel(ILayoutEngine layout, IPlaybackEngine playback)
    {
        _layout = layout;
        _playback = playback;
    }

    /// <summary>
    /// Selects a note and arms "play from here": shows the cursor at the note and
    /// reports its absolute tick so playback starts from that point.
    /// </summary>
    public void SelectNote(Guid staffId, int measureNumber, Guid noteId)
    {
        SelectedNoteId  = noteId;
        SelectedMeasure = measureNumber;
        SelectedStaffId = staffId;

        double tick = AbsoluteTickOf(staffId, measureNumber, noteId);
        if (tick < 0) return;
        PlaybackTick = tick;                              // stationary cursor at the note
        PlaybackStartTickChanged?.Invoke(this, tick);     // arm play-from-here
    }

    private double AbsoluteTickOf(Guid staffId, int measureNumber, Guid noteId)
    {
        var staff = Editor?.Score.Parts.SelectMany(p => p.Staves)
            .FirstOrDefault(s => s.Id == staffId);
        if (staff is null) return -1;
        double acc = 0;
        foreach (var m in staff.Measures.OrderBy(m => m.Number))
        {
            if (m.Number == measureNumber)
            {
                var note = m.Notes.FirstOrDefault(n => n.Id == noteId);
                return note is null ? acc : acc + note.TickOffset;
            }
            acc += m.TimeSignature.TicksPerMeasure;
        }
        return -1;
    }

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

        // Flow across measures: if the clicked measure can't fit this note,
        // advance to the next measure that has room (creating measures at the end
        // as needed). This is what stops notes from piling up and overlapping
        // past the bar line.
        int targetMeasure = ResolveTargetMeasure(staffId, measureNumber, note.Duration.Ticks);

        Editor.AddNote(staffId, targetMeasure, note);
        SelectedNoteId  = note.Id;
        SelectedMeasure = targetMeasure;
        SelectedStaffId = staffId;

        // Audible feedback when entering a pitched note.
        if (note.Pitch is { } enteredPitch)
            _ = _playback.PreviewNoteAsync(enteredPitch);
    }

    /// <summary>
    /// Returns the measure number that should receive a note of <paramref name="neededTicks"/>,
    /// starting from <paramref name="startMeasure"/>. Skips full measures and appends new
    /// measures (to every staff) when the music runs off the end.
    /// </summary>
    private int ResolveTargetMeasure(Guid staffId, int startMeasure, int neededTicks)
    {
        if (Editor is null) return startMeasure;
        var staff = Editor.Score.Parts.SelectMany(p => p.Staves)
            .FirstOrDefault(s => s.Id == staffId);
        if (staff is null) return startMeasure;

        int target = startMeasure;
        // Skip partially-filled measures that cannot hold the note, but stop at an
        // empty measure so an oversized note never loops forever.
        while (staff.GetMeasure(target) is { } m && m.UsedTicks > 0 && m.RemainingTicks < neededTicks)
            target++;

        if (staff.GetMeasure(target) is null)
        {
            int last = staff.Measures.Count == 0 ? 0 : staff.Measures.Max(m => m.Number);
            Editor.AddMeasures(last, Math.Max(1, target - last));
        }
        return target;
    }

    /// <summary>
    /// Keyboard note entry: enter a note by letter name. Chooses the octave
    /// nearest the previous note on the target staff, honours the key signature,
    /// and flows across measures exactly like click entry.
    /// </summary>
    public void EnterNoteByName(NoteName name)
    {
        if (Editor is null || !InputMode) return;

        var staff = (SelectedStaffId != Guid.Empty
            ? Editor.Score.Parts.SelectMany(p => p.Staves).FirstOrDefault(s => s.Id == SelectedStaffId)
            : null) ?? Editor.Score.Parts.SelectMany(p => p.Staves).FirstOrDefault();
        if (staff is null) return;

        int startMeasure = SelectedMeasure > 0 ? SelectedMeasure : 1;
        var measure = staff.GetMeasure(startMeasure);
        var clef = measure?.ClefChange ?? staff.DefaultClef;
        var key  = measure?.KeySignatureChange ?? Editor.Score.InitialKeySignature;

        // Octave: nearest to the last pitched note on this staff, else a clef default.
        var lastPitch = staff.Measures.OrderByDescending(m => m.Number)
            .SelectMany(m => m.Notes.OrderByDescending(n => n.TickOffset))
            .FirstOrDefault(n => n.Pitch is not null)?.Pitch;
        var natural = lastPitch is { } prev
            ? NearestPitch(name, prev)
            : new Pitch(name, Accidental.Natural, DefaultOctave(clef));

        int staffPos = StaffPositionCalculator.Calculate(natural, clef);

        var note = new Note
        {
            Duration      = SelectedDuration,
            StaffPosition = staffPos,
            Hand          = SelectedHand
        };
        if (!InputRest)
        {
            var pitch = StaffPositionCalculator.FromStaffPosition(staffPos, clef, key);
            if (AccidentalOverride.HasValue)
            {
                pitch = new Pitch(pitch.Name, AccidentalOverride.Value, pitch.Octave);
                AccidentalOverride = null;
            }
            note.Pitch = pitch;
        }

        int target = ResolveTargetMeasure(staff.Id, startMeasure, note.Duration.Ticks);
        Editor.AddNote(staff.Id, target, note);
        SelectedNoteId  = note.Id;
        SelectedMeasure = target;
        SelectedStaffId = staff.Id;

        if (note.Pitch is { } entered)
            _ = _playback.PreviewNoteAsync(entered);
    }

    /// <summary>Selects a duration by toolbar index (0 = whole … 5 = 32nd). Used by number-key shortcuts.</summary>
    public void SetDurationByIndex(int index)
    {
        var value = index switch
        {
            0 => NoteValue.Whole,
            1 => NoteValue.Half,
            2 => NoteValue.Quarter,
            3 => NoteValue.Eighth,
            4 => NoteValue.Sixteenth,
            5 => NoteValue.ThirtySecond,
            _ => NoteValue.Quarter
        };
        SelectedDuration = new Duration(value, Dotted ? 1 : 0);
    }

    private static int DefaultOctave(Clef clef) => clef.Type switch
    {
        ClefType.Bass  => 3,
        ClefType.Tenor => 3,
        _              => 4
    };

    private static Pitch NearestPitch(NoteName name, Pitch reference)
    {
        Pitch best = new(name, Accidental.Natural, reference.Octave);
        int bestDist = Math.Abs(best.MidiNumber - reference.MidiNumber);
        foreach (int oct in new[] { reference.Octave - 1, reference.Octave + 1 })
        {
            var cand = new Pitch(name, Accidental.Natural, oct);
            int d = Math.Abs(cand.MidiNumber - reference.MidiNumber);
            if (d < bestDist) { best = cand; bestDist = d; }
        }
        return best;
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
