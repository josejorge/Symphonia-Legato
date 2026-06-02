# Architecture

## Overview

Symphonia Legato follows a clean layered architecture with strict dependency rules:
- **Core** libraries have no UI or platform dependencies.
- **Apps** depend on Core; Core never depends on Apps.
- **DI** wires everything together at startup.

```
┌─────────────────────────────────────┐
│         SymphoniaLegato.Desktop      │   Avalonia UI, ViewModels, Controls
├─────────────────────────────────────┤
│         SymphoniaLegato.PdfEngine    │   QuestPDF rendering
│         SymphoniaLegato.PluginEngine │   Plugin host, sandbox
│         SymphoniaLegato.GitInteg.   │   LibGit2Sharp wrapper
├─────────────────────────────────────┤
│         SymphoniaLegato.ImportExport │   MusicXML, MIDI, .enscore
│         SymphoniaLegato.LayoutEngine │   SkiaSharp layout computation
│         SymphoniaLegato.PlaybackEng  │   DryWetMidi playback
│         SymphoniaLegato.NotationEng  │   Score editing commands
├─────────────────────────────────────┤
│         SymphoniaLegato.Core         │   Domain models, interfaces only
└─────────────────────────────────────┘
```

---

## Domain Model

```
Score
└── Part[]
    └── Staff[]
        ├── Instrument
        ├── Clef (default)
        └── Measure[]
            ├── TimeSignature
            ├── KeySignature?
            ├── Clef? (change)
            ├── Note[]
            │   ├── Pitch? (null = rest)
            │   ├── Duration
            │   ├── ChordNotes[]
            │   ├── Articulation
            │   └── Hand (right/left/unassigned)
            ├── Dynamic[]
            ├── TempoMarking[]
            └── TextAnnotation[]
```

---

## Score Editor (Command Pattern)

All mutations go through `ScoreEditor.Execute(IScoreCommand)`.

- **Execute** → runs the command, pushes to undo stack, clears redo stack, fires `ScoreChanged`
- **Undo** → pops undo stack, runs `Undo()`, pushes to redo stack
- **Redo** → pops redo stack, re-executes

Commands implemented:
- `AddNoteCommand` / `DeleteNoteCommand`
- `ChangeTimeSignatureCommand`
- `ChangeKeySignatureCommand`
- `AddMeasuresCommand` / `DeleteMeasureCommand`

---

## Layout Engine

`LayoutEngine.ComputeLayout(Score, LayoutOptions)` returns a `LayoutResult` — a tree of:

```
LayoutResult
└── RenderedPage[]
    └── RenderedSystem[]      ← one row of staves
        └── RenderedStaff[]
            └── RenderedMeasure[]
                └── RenderedNoteElement[]
```

All coordinates are in **pixels at the requested DPI and zoom**. The `ScoreCanvas` control reads this tree directly to paint with SkiaSharp — no intermediate representation.

---

## Playback Architecture

```
ScoreEditor (domain)
     │
     ▼
ScoreToMidiConverter ──► MidiFile (DryWetMidi)
                                │
                                ▼
                          Playback (DryWetMidi)
                                │
                                ▼
                         OutputDevice (system MIDI out)
```

`MidiPlaybackEngine` exposes `IPlaybackEngine` — the VM layer never touches DryWetMidi directly.

---

## .enscore File Format

| Entry | Format | Purpose |
|---|---|---|
| `score.xml` | MusicXML 4.0 | Score content, git-diffable |
| `project.json` | JSON | Metadata, settings |
| `audio/` | directory | Audio file references |
| `assets/` | directory | Embedded images |

---

## Plugin System

Plugins are .NET assemblies that implement `IPlugin`. They are loaded from the `plugins/` directory at startup. Plugins receive an `IPluginContext` (sandboxed API) and may:

- Register custom instruments (General MIDI or SoundFont-based)
- Register custom exporters
- Add notation symbols
- Hook into the AI extension point (Phase 5)

---

## AI Extension Points (Phase 5)

Reserved service interfaces (not yet implemented):

- `IHarmonizationService` — suggest chord progressions
- `IChordDetectionService` — detect chords from selected notes
- `IFingeringService` — compute optimal fingering
- `IScoreAnalysisService` — analyse structure, form, difficulty
- `IPracticeRecommendationService` — recommend practice sections

All AI features are **disabled by default** and only activated via plugins or explicit user settings.

---

## Threading Model

- All UI work runs on the Avalonia UI thread.
- `IPlaybackEngine` methods that perform I/O (`PlayAsync`, `StopAsync`) are `async Task`.
- `LayoutEngine.ComputeLayout` is synchronous but fast (< 50ms for typical scores).
- File I/O (load/save) is always `async`.

---

## Dependency Injection

DI is configured in `Program.cs` via `Microsoft.Extensions.DependencyInjection`.

- Singletons: `IPlaybackEngine`, `ILayoutEngine`, `IScoreRepository`, converters
- Transients: all ViewModels
- Scoped: not used (no web scope)
