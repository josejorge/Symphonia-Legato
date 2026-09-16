# Module: SymphoniaLegato.NotationEngine

**Path:** `src/Core/SymphoniaLegato.NotationEngine/`
**Type:** Core library (engine layer)

## Purpose
Score editing engine: `ScoreEditor` executes/undoes `IScoreCommand`s, and
`PostProcess()` recomputes beam groups, stem directions, and accidentals after
every command. Also home to `BeamCalculator`, `AccidentalProcessor`, and
`StaffPositionCalculator` (click position ↔ pitch — see `CLAUDE.md` pitfall
#30, the round-trip invariant it's covered by a dedicated test for).

## Depends on
`SymphoniaLegato.Core`.

## Depended on by
`Rendering`, `Desktop`, `Android`.

## Key files
| File | Purpose |
|------|---------|
| `ScoreEditor.cs` | Command execution + undo/redo |
| `Commands/` | All `IScoreCommand` implementations |
| `StaffPositionCalculator.cs` | Click position ↔ pitch (round-trip tested) |

## Docs
`docs/operations_guide.html`, `docs/executive_overview.html` (this module).
Full architecture: root `CLAUDE.md` / `docs/ARCHITECTURE.md`.
