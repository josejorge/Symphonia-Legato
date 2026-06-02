# CLAUDE.md — Symphonia Legato

> This file is loaded automatically by Claude Code at the start of every session.
> It contains everything needed to continue development without re-explaining context.

---

## Project identity

**Symphonia Legato** — modern open-source music notation software inspired by Encore.
**Author / owner:** Jose Jorge Hernandez (credit in all docs, About dialog, and LICENSE).
**License:** MIT
**Stack:** C# (.NET 9) · Avalonia 11 · MVVM · MusicXML 4.0

---

## How to build and run

```powershell
# Restore + build (0 warnings, 0 errors expected)
dotnet build SymphoniaLegato.sln -c Debug

# Run the desktop app
dotnet run --project src\Apps\SymphoniaLegato.Desktop --no-build

# Run all tests (55 passing)
dotnet test SymphoniaLegato.sln --no-build

# Publish self-contained Windows EXE
dotnet publish src\Apps\SymphoniaLegato.Desktop -c Release -r win-x64 `
  --self-contained -p:PublishSingleFile=true -o publish\windows
```

---

## Solution structure

```
SymphoniaLegato.sln
src/
  Core/
    SymphoniaLegato.Core              # Domain models & interfaces
    SymphoniaLegato.NotationEngine    # ScoreEditor, undo/redo, BeamCalculator, AccidentalProcessor
    SymphoniaLegato.PlaybackEngine    # MidiPlaybackEngine, ScoreToMidiConverter
    SymphoniaLegato.LayoutEngine      # Proportional layout → LayoutResult tree
    SymphoniaLegato.ImportExport      # MusicXML, MIDI, .enscore (ZIP format)
    SymphoniaLegato.PdfEngine         # PDF export (skeleton, Phase 3)
    SymphoniaLegato.PluginEngine      # Plugin host & sandbox
    SymphoniaLegato.GitIntegration    # LibGit2Sharp wrapper
  Apps/
    SymphoniaLegato.Desktop           # Avalonia MVVM desktop app
    SymphoniaLegato.Android           # (Phase 4, empty scaffold)
tests/
  SymphoniaLegato.Core.Tests
  SymphoniaLegato.NotationEngine.Tests
  SymphoniaLegato.PlaybackEngine.Tests
  SymphoniaLegato.Integration.Tests
docs/                                 # All documentation markdown
publish/windows/                      # Self-contained EXE output
```

---

## Architecture

**Dependency rule:** Core ← Engine layers ← Desktop. Core never imports from Apps.

**DI wiring:** `Program.cs` (top-level statements) registers all services into
`Microsoft.Extensions.DependencyInjection.ServiceCollection` and builds a `ServiceProvider`.
`App.axaml.cs` receives the provider and injects `MainWindowViewModel`.

**Rendering:** Custom controls (`ScoreCanvas`, `PianoKeyboardControl`) extend `Control` and
override `Render(DrawingContext ctx)`. **No SkiaSharp packages** — Avalonia's `DrawingContext`
is used directly (it is backed by Skia internally).

**Score editing:** All mutations go through `ScoreEditor.Execute(IScoreCommand)`.
Every command implements `Execute` + `Undo`. `PostProcess()` runs after every command
to recompute beam groups, stem directions, and accidentals.

**Layout:** `LayoutEngine.ComputeLayout(Score, LayoutOptions)` returns a `LayoutResult`
(tree of `RenderedPage → RenderedSystem → RenderedStaff → RenderedMeasure → RenderedNoteElement`).
`ScoreCanvas` reads this tree directly to paint — no intermediate format.

---

## Domain model quick reference

```
Score → Part[] → Staff[]
  Staff → Measure[]
    Measure → Note[], Dynamic[], Hairpin[], Slur[], TempoMarking[], TextAnnotation[]
      Note → Pitch?, Duration, ChordNotes[], Lyric[], Articulation, Hand, Stem, BeamGroup
  Staff → Instrument, Clef, Volume, Pan, IsMuted, IsSolo
Score → InitialTimeSignature, InitialKeySignature, InitialTempo, PageSize
```

**Tick unit:** 1 quarter note = 1024 ticks. All durations are expressed in these ticks.
**Staff positions:** 1 = bottom line of staff, 9 = top line, <1 = ledger lines below, >9 = above.
**Pitch.MidiNumber:** Middle C (C4) = 60.

---

## Key files

| File | Purpose |
|------|---------|
| `src/Core/SymphoniaLegato.Core/Models/` | All domain model classes |
| `src/Core/SymphoniaLegato.Core/Interfaces/ILayoutEngine.cs` | Layout result types (RenderedPage, RenderedMeasure, etc.) |
| `src/Core/SymphoniaLegato.NotationEngine/ScoreEditor.cs` | Command execution + undo/redo |
| `src/Core/SymphoniaLegato.NotationEngine/Commands/` | All IScoreCommand implementations |
| `src/Core/SymphoniaLegato.LayoutEngine/LayoutEngine.cs` | Full layout computation |
| `src/Core/SymphoniaLegato.ImportExport/EnScoreFormat.cs` | .enscore save/load |
| `src/Apps/SymphoniaLegato.Desktop/Controls/ScoreCanvas.cs` | Score rendering (DrawingContext) |
| `src/Apps/SymphoniaLegato.Desktop/Controls/PianoKeyboardControl.cs` | Piano keyboard rendering |
| `src/Apps/SymphoniaLegato.Desktop/Views/MainWindow.axaml` | Main window layout |
| `src/Apps/SymphoniaLegato.Desktop/Views/AboutWindow.axaml` | About dialog |
| `src/Apps/SymphoniaLegato.Desktop/Program.cs` | DI registration & app entry point |
| `src/Apps/SymphoniaLegato.Desktop/Themes/SymphoniaTheme.axaml` | Dark theme tokens |
| `docs/ROADMAP.md` | Phase-by-phase feature plan |

---

## Known pitfalls — read before touching these areas

1. **Avalonia `InputGesture`** — use `OemPlus`, `OemMinus`, `D0`–`D9` for +/-/digit keys.
   `Plus` / `Minus` / `0` throw `ArgumentException` at startup and crash the app.

2. **SkiaSharp packages** — `SkiaSharp.Views.Avalonia` and `Avalonia.SkiaSharp` do not exist
   on NuGet. All rendering uses `Avalonia.Media.DrawingContext`. Use `CubicBezierTo`, not `BezierTo`.

3. **Cross-platform TargetFramework** — use `net9.0`, not `net9.0-linux` / `net9.0-macos`.
   Supply `-r linux-x64` / `-r osx-arm64` at publish time.

4. **DryWetMidi name collisions** — `Melanchall.DryWetMidi.Interaction` exports `Note` and
   `TimeSignature` which clash with domain types. Use `using` aliases in any file that imports both.

5. **GenerateDocumentationFile** — set to `false` in `Directory.Build.props`. Turning it on
   produces ~80 CS1591 warnings that hide real errors.

6. **`IScoreRepository` double-registration** — only register the concrete `EnScoreRepository`
   once; the interface binding points to it via a lambda. A second `AddSingleton<IScoreRepository>`
   would shadow the first.

---

## Phase status

| Phase | Status | Summary |
|-------|--------|---------|
| 1 — MVP | ✅ Done | Domain model, notation engine, layout, MIDI playback, save/load, 38 tests |
| 2 — Advanced notation | ✅ Done | Noteheads, stems, beams, accidentals, rests, clefs, dynamics, hairpins, slurs, articulations, lyrics, hand coloring, MIDI import, 55 tests |
| 3 — Professional | 🔜 Next | PDF export, PNG/SVG, SoundFont playback, plugin UI, Git history UI, cloud sync, score properties, accessibility |
| 4 — Android | ⏳ Planned | Companion app, score viewer, playback, annotations |
| 5 — AI | ⏳ Planned | Harmonisation, chord detection, fingering, analysis |

---

## Conventions

- **No XML doc comments** on app code (`GenerateDocumentationFile=false`).
- **No comments** unless the *why* is non-obvious.
- All score mutations via `ScoreEditor.Execute(IScoreCommand)` — never mutate `Score` directly.
- ViewModels must not import Avalonia UI types; use events to ask the View to open dialogs.
- Tests use `xUnit` + `FluentAssertions`. Naming: `Method_StateUnderTest_Expected`.
- Conventional Commits for messages: `feat:`, `fix:`, `test:`, `docs:`, `refactor:`.
