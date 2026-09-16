# Module: SymphoniaLegato.Core

**Path:** `src/Core/SymphoniaLegato.Core/`
**Type:** Core library (domain layer — the base of the dependency graph)

## Purpose
Domain models and interfaces for the whole application: `Score`, `Part`,
`Staff`, `Measure`, `Note`, `Pitch`, `Duration`, `TimeSignature`,
`KeySignature`, `Clef`, `Instrument`, dynamics/hairpins/slurs/annotations, the
AI result types (`ChordLabel`, `FingeringResult`, etc.), and the interfaces
every engine implements (`ILayoutEngine`, `IPlaybackEngine`, `IAIEngine`,
`IScoreRepository`). Contains no logic beyond simple model invariants
(`Duration.Ticks`, `Pitch.MidiNumber`, `Measure.AddNote`) — everything
behavioural lives in the engine layers below.

## Depends on
Nothing in this repo. Only BCL/.NET.

## Depended on by
Every other module — `AIEngine`, `GitIntegration`, `ImportExport`,
`LayoutEngine`, `NotationEngine`, `PdfEngine`, `PlaybackEngine`,
`PluginEngine`, `Rendering`, `Desktop`, `Android`. Nothing may import from an
app (`Desktop`/`Android`) into `Core` — see `CLAUDE.md`'s "Dependency rule".

## Key files
| File | Purpose |
|------|---------|
| `Models/` | All domain model classes |
| `Interfaces/ILayoutEngine.cs` | Layout result types |
| `Models/AIModels.cs` | AI result types (`ChordLabel`, `FingeringResult`, `AIAnalysisResult`, …) |

## Docs
Domain model quick reference and full architecture live in the project root's
`CLAUDE.md` and `docs/ARCHITECTURE.md`. This module's own operations/executive
docs: `docs/operations_guide.html`, `docs/executive_overview.html`.
