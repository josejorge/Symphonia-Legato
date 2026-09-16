# Debugging

## Logging

`Program.cs` configures `AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug))`
— the only active sink is `AddDebug()`, which writes to whatever debugger is
attached (Visual Studio's Output pane, `dotnet run` under a debugger, etc.).
There is no file log — see `docs/operations_guide.html`. If you need to
capture logs without a debugger attached, that's a real gap (tracked in
`docs/TODO.md`), not a missed setting.

## Useful breakpoint locations

| Symptom you're chasing | Start here |
|---|---|
| A score edit didn't apply / undo is wrong | `ScoreEditor.Execute` (`NotationEngine`) |
| A note drew in the wrong place | `LayoutEngine.ComputeLayout` → `StaffPositionCalculator` |
| A note played the wrong pitch | `StaffPositionCalculator.FromStaffPosition` / `ScoreToMidiConverter.BuildTrack` |
| Playback didn't start / was silent | `MidiPlaybackEngine.PlayAsync` / `EnsureOutputDevice()` |
| An export produced a blank/wrong file | `ScorePngExporter` → `ScorePdfExporter`/`ScoreSvgExporter`/`MusicXmlExporter` |
| A ViewModel property didn't update the UI | Check the `[ObservableProperty]` is actually bound in the matching `.axaml` — see `CLAUDE.md` pitfall entries for the duration-toolbar bug as a worked example |

## Reproducing a bug headlessly

Most engine-layer bugs (layout, MIDI conversion, notation commands) can be
reproduced in a unit test without launching the UI — see
`tests/SymphoniaLegato.PlaybackEngine.Tests/ScoreToMidiConverterTests.cs` for
the pattern: build a minimal `Score` by hand, run it through the engine, and
assert on the result. This is faster to iterate on than reproducing through
the UI and is the preferred first step before reaching for the debugger.

## Common Avalonia-specific gotchas

See `CLAUDE.md`'s "Known pitfalls" section (31 entries) and `docs/BUGS.md` —
most non-obvious Desktop bugs trace back to one of those.
