# Data Model

The in-memory domain model every module shares, defined entirely in
`SymphoniaLegato.Core` (see its `module.md`). Nothing here is persisted to a
database (see `docs/DATABASE.md`) — it's serialized to `.enscore` (ZIP +
embedded MusicXML + JSON) or MusicXML/MIDI on demand.

## Hierarchy

```
Score → Part[] → Staff[]
  Staff → Measure[]
    Measure → Note[], Dynamic[], Hairpin[], Slur[], TempoMarking[], TextAnnotation[]
      Note → Pitch?, Duration, ChordNotes[], Lyric[], Articulation, Hand, Stem, BeamGroup
  Staff → Instrument, Clef, Volume, Pan, IsMuted, IsSolo
Score → InitialTimeSignature, InitialKeySignature, InitialTempo, PageSize
Score → Annotations[]  (per-page freehand strokes)
```

## Key conventions

| Concept | Rule |
|---|---|
| Tick unit | 1 quarter note = 1024 ticks. All durations are expressed in these ticks (`Duration.Ticks`). |
| Staff positions | 1 = bottom line of staff, 9 = top line, <1 = ledger lines below, >9 = above (`StaffPositionCalculator`). |
| Pitch | `Pitch.MidiNumber` — middle C (C4) = 60. |
| Rest | A `Note` with `Pitch == null` (`Note.IsRest`). |
| Chord | Extra pitches on `Note.ChordNotes` — these store `Pitch`, not `Note` (no independent `StaffPosition`; computed via `PitchToStaffPosition`). |

## Where types live

| File | Contains |
|---|---|
| `Models/Score.cs` | `Score`, `Part` |
| `Models/Staff.cs` | `Staff`, `Instrument`, `InstrumentFamily` |
| `Models/Measure.cs` | `Measure`, `BarlineType`, `TextAnnotation` |
| `Models/Note.cs` | `Note`, `Articulation`, `Hand` |
| `Models/Pitch.cs` | `Pitch`, `NoteName`, `Accidental` |
| `Models/Duration.cs` | `Duration`, `NoteValue` |
| `Models/TimeSignature.cs` | `TimeSignature` |
| `Models/KeySignature.cs` | `KeySignature`, `Mode` |
| `Models/Clef.cs` | `ClefType` |
| `Models/Dynamics.cs` | `Dynamic`, `DynamicLevel`, `TempoMarking` |
| `Models/Hairpin.cs` | `Hairpin`, `HairpinType` |
| `Models/SlurTie.cs` | `Slur`, `Tie`, `CurveDirection` |
| `Models/Lyric.cs` | `Lyric`, `LyricSyllable` |
| `Models/ScoreAnnotation.cs` | `ScoreAnnotation`, `AnnotationStroke` |
| `Models/Volta.cs` | `VoltaBracket` |
| `Models/AIModels.cs` | `ChordLabel`, `FingeringResult`, `AIAnalysisResult`, `PracticeRecommendation`, `HarmonyMeasure` |
| `Interfaces/ILayoutEngine.cs` | The rendered-output tree (`LayoutResult` → `RenderedPage` → … → `RenderedNoteElement`) |

See `docs/ARCHITECTURE.md` for how the model flows through the engine layers,
and each module's own `module.md` for which module owns which behavior.
