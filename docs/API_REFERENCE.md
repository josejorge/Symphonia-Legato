# API Reference

## SymphoniaLegato.Core

### Models

#### `Pitch`
```csharp
readonly record struct Pitch(NoteName Name, Accidental Accidental, int Octave)
```
- `MidiNumber` — MIDI note number (0–127). Middle C = 60.
- `FromMidi(int midi)` — construct from raw MIDI number.
- `ToString()` — e.g. `"F#4"`, `"Bb3"`.

#### `Duration`
```csharp
readonly record struct Duration(NoteValue Value, int Dots = 0)
```
- `Ticks` — total ticks (1 quarter = 1024).
- Static presets: `Whole`, `Half`, `Quarter`, `Eighth`, `Sixteenth`, etc.

#### `Note`
- `Pitch?` — null means rest.
- `Duration` — value + dots.
- `ChordNotes` — additional pitches forming a chord.
- `Articulation`, `Hand`, `Velocity`, `Fingering`.
- `IsRest`, `IsTiedFrom`, `IsTiedTo`.

#### `Measure`
- `Notes`, `Dynamics`, `TempoMarkings`, `Lyrics`, `Annotations`.
- `AddNote(Note)` — appends note, sets tick offset.
- `UsedTicks`, `RemainingTicks`, `IsFull`.

#### `Staff`
- `Instrument`, `DefaultClef`, `Measures`.
- `Volume` (0–127), `Pan` (0–127), `IsMuted`, `IsSolo`.
- `GetOrAddMeasure(int number, TimeSignature)`.

#### `Score`
- `Parts`, `InitialTimeSignature`, `InitialKeySignature`, `InitialTempo`.
- `CreatePianoScore(string title)` — factory for a linked grand staff.

---

## SymphoniaLegato.NotationEngine

### `ScoreEditor`
```csharp
ScoreEditor(Score score, ILogger<ScoreEditor> logger)
```
- `Execute(IScoreCommand)` — runs command + pushes undo.
- `Undo()`, `Redo()`.
- `CanUndo`, `CanRedo`, `IsDirty`.
- `AddNote(Guid staffId, int measureNumber, Note note)`.
- `DeleteNote(Guid staffId, int measureNumber, Guid noteId)`.
- `ChangeTimeSignature(int measureNumber, TimeSignature ts)`.
- `ChangeKeySignature(int measureNumber, KeySignature ks)`.
- `AddMeasures(int afterMeasure, int count)`.
- `DeleteMeasure(int measureNumber)`.

### `StaffPositionCalculator`
- `Calculate(Pitch pitch, Clef clef) → int` — returns staff position (1 = bottom line).
- `GetLedgerLines(int staffPosition) → (int count, bool above)`.
- `FromStaffPosition(int pos, Clef clef, KeySignature key) → Pitch`.

---

## SymphoniaLegato.PlaybackEngine

### `IPlaybackEngine`
- `LoadScoreAsync(Score score)`.
- `PlayAsync()`, `PauseAsync()`, `StopAsync()`, `SeekAsync(TimeSpan)`.
- `State`, `Position`, `Duration`, `TempoMultiplier`, `IsLooping`.
- `SetStaffVolume/Pan/Muted/Solo(Guid staffId, ...)`.
- `PreviewNoteAsync(Pitch pitch, int velocity, int durationMs)`.

---

## SymphoniaLegato.LayoutEngine

### `ILayoutEngine`
- `ComputeLayout(Score score, LayoutOptions options) → LayoutResult`.
- `ComputePageLayout(Score score, LayoutOptions options, int pageNumber) → LayoutResult`.

### `LayoutOptions`
- `Zoom` (0.25–8.0), `DPI`, `StaffSpacingPx`, `SystemSpacingPx`, `PageMarginPx`.
- `MeasuresPerSystemHint` (0 = auto), `ForPrinting`.

---

## SymphoniaLegato.ImportExport

### `IScoreImporter`
- `ImportAsync(string filePath) → Task<Score>`.
- Implementations: `MusicXmlImporter`.

### `IScoreExporter`
- `ExportAsync(Score score, string filePath, ExportOptions?)`.
- Implementations: `MusicXmlExporter`.

### `IScoreRepository` (.enscore)
- `LoadAsync(string filePath) → Task<Score?>`.
- `SaveAsync(Score score, string filePath)`.
- `GetRecentAsync(int count) → Task<IReadOnlyList<ScoreMetadata>>`.
