# Testing Guide

## Test Projects

| Project | Scope |
|---|---|
| `SymphoniaLegato.Core.Tests` | Domain models — Pitch, Duration, Note, Score |
| `SymphoniaLegato.NotationEngine.Tests` | ScoreEditor, undo/redo, StaffPositionCalculator |
| `SymphoniaLegato.PlaybackEngine.Tests` | ScoreToMidiConverter, playback state machine |
| `SymphoniaLegato.Integration.Tests` | End-to-end: import → edit → export → reimport |

## Running Tests

```bash
# All tests
dotnet test

# Single project
dotnet test tests/SymphoniaLegato.Core.Tests --verbosity normal

# With coverage (Coverlet)
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults

# HTML report (requires reportgenerator global tool)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:"TestResults/Report" \
  -reporttypes:Html
```

## Coverage Target

80% line coverage across all Core libraries.

## Test Naming Convention

```
MethodName_StateUnderTest_ExpectedBehaviour
```

Example: `MidiNumber_ComputesCorrectly`, `Undo_RemovesAddedNote`

## Integration Tests

Integration tests exercise full import→edit→export round-trips using real files in `tests/fixtures/`. They should run without UI and without a MIDI device (use the null playback engine).

## UI Tests

UI tests use Avalonia's headless mode (Phase 2+). They render the score canvas to a bitmap and compare against golden images.

## Performance Tests

Performance benchmarks (BenchmarkDotNet) live in `tools/Benchmarks/`. Run:

```bash
dotnet run --project tools/Benchmarks -c Release
```

Key benchmarks:
- `LayoutBenchmark.ComputeLayout` — target < 50ms for 100-measure score
- `MusicXmlImportBenchmark` — target < 200ms for typical file
- `MidiConvertBenchmark` — target < 10ms for 100-measure score
