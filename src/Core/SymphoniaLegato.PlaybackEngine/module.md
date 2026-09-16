# Module: SymphoniaLegato.PlaybackEngine

**Path:** `src/Core/SymphoniaLegato.PlaybackEngine/`
**Type:** Core library (engine layer)

## Purpose
Audible playback: `MidiPlaybackEngine` (DryWetMidi-based, implements
`IPlaybackEngine`), `ScoreToMidiConverter` (domain `Score` → `MidiFile`, ticks,
tempo, GM percussion click track), and `MetronomeEngine` (cross-platform
timer-based metronome events, distinct from the audible per-beat click baked
into the MIDI — see `CLAUDE.md` pitfall #29).

## Depends on
`SymphoniaLegato.Core`.

## Depended on by
`Desktop`, `Android`.

## Key files
| File | Purpose |
|------|---------|
| `MidiPlaybackEngine.cs` | `IPlaybackEngine` implementation; opens the MIDI output device lazily |
| `ScoreToMidiConverter.cs` | Domain → MIDI conversion, tempo, percussion click track |
| `MetronomeEngine.cs` | Timer-based metronome events (`Beat`) |

## Docs
`docs/operations_guide.html`, `docs/executive_overview.html` (this module).
Full architecture: root `CLAUDE.md` / `docs/ARCHITECTURE.md`. Test coverage
for `ScoreToMidiConverter`: `tests/SymphoniaLegato.PlaybackEngine.Tests/`.
