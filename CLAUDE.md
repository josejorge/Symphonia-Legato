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

# Run all tests (88 passing)
dotnet test SymphoniaLegato.sln --no-build

# Publish self-contained Windows EXE
dotnet publish src\Apps\SymphoniaLegato.Desktop -c Release -r win-x64 `
  --self-contained -p:PublishSingleFile=true -o publish\windows

# Build Android APK (requires: dotnet workload install android)
dotnet build SymphoniaLegato.Android.sln -c Debug
dotnet publish SymphoniaLegato.Android.sln -c Release -r android-arm64 `
  --self-contained -o publish\android
```

---

## Solution structure

```
SymphoniaLegato.sln          ← Desktop + all tests (always buildable)
SymphoniaLegato.Android.sln  ← Android + shared Core (needs android workload)

src/
  Core/
    SymphoniaLegato.Core              # Domain models & interfaces
    SymphoniaLegato.NotationEngine    # ScoreEditor, undo/redo, BeamCalculator, AccidentalProcessor
    SymphoniaLegato.PlaybackEngine    # MidiPlaybackEngine, ScoreToMidiConverter, MetronomeEngine
    SymphoniaLegato.LayoutEngine      # Proportional layout → LayoutResult tree
    SymphoniaLegato.ImportExport      # MusicXML, MIDI, .enscore (ZIP), ScoreSvgExporter, ScoreSyncService
    SymphoniaLegato.PdfEngine         # PDF export (QuestPDF, embeds PNG pages)
    SymphoniaLegato.PluginEngine      # Plugin host & sandbox
    SymphoniaLegato.GitIntegration    # LibGit2Sharp wrapper
  Apps/
    SymphoniaLegato.Desktop           # Avalonia MVVM desktop app (Windows/Linux/macOS)
    SymphoniaLegato.Android           # Avalonia Android companion app
tests/
  SymphoniaLegato.Core.Tests
  SymphoniaLegato.NotationEngine.Tests
  SymphoniaLegato.PlaybackEngine.Tests
  SymphoniaLegato.Integration.Tests
docs/
  ROADMAP.md                          # Phase-by-phase feature plan
  ANDROID.md                          # Android build + deploy guide
publish/
  windows/                            # Self-contained EXE output
  android/                            # APK output (after android publish)
```

---

## Architecture

**Dependency rule:** Core ← Engine layers ← Desktop/Android. Core never imports from Apps.

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

**Export pipeline:** `ScorePngExporter` (Desktop, Avalonia off-screen render) → PNG bytes →
`ScorePdfExporter` (PdfEngine, QuestPDF) embeds those bytes into a PDF. SVG uses a separate
text-generation path from `ScoreSvgExporter` in ImportExport.

---

## Domain model quick reference

```
Score → Part[] → Staff[]
  Staff → Measure[]
    Measure → Note[], Dynamic[], Hairpin[], Slur[], TempoMarking[], TextAnnotation[]
      Note → Pitch?, Duration, ChordNotes[], Lyric[], Articulation, Hand, Stem, BeamGroup
  Staff → Instrument, Clef, Volume, Pan, IsMuted, IsSolo
Score → InitialTimeSignature, InitialKeySignature, InitialTempo, PageSize
Score → Annotations[]  ← Phase 4: per-page freehand strokes
```

**Tick unit:** 1 quarter note = 1024 ticks. All durations are expressed in these ticks.
**Staff positions:** 1 = bottom line of staff, 9 = top line, <1 = ledger lines below, >9 = above.
**Pitch.MidiNumber:** Middle C (C4) = 60.

---

## Key files

| File | Purpose |
|------|---------|
| `src/Core/SymphoniaLegato.Core/Models/` | All domain model classes |
| `src/Core/SymphoniaLegato.Core/Interfaces/ILayoutEngine.cs` | Layout result types |
| `src/Core/SymphoniaLegato.NotationEngine/ScoreEditor.cs` | Command execution + undo/redo |
| `src/Core/SymphoniaLegato.NotationEngine/Commands/` | All IScoreCommand implementations |
| `src/Core/SymphoniaLegato.LayoutEngine/LayoutEngine.cs` | Full layout computation |
| `src/Core/SymphoniaLegato.ImportExport/EnScoreFormat.cs` | .enscore save/load |
| `src/Core/SymphoniaLegato.ImportExport/ScoreSvgExporter.cs` | SVG export (from LayoutResult) |
| `src/Core/SymphoniaLegato.ImportExport/ScoreSyncService.cs` | Cloud sync (folder-based, newer-wins) |
| `src/Core/SymphoniaLegato.PlaybackEngine/MetronomeEngine.cs` | Cross-platform timer-based metronome |
| `src/Core/SymphoniaLegato.PdfEngine/ScorePdfExporter.cs` | PDF export (QuestPDF; embeds PNG pages) |
| `src/Apps/SymphoniaLegato.Desktop/Controls/ScoreCanvas.cs` | Score rendering (DrawingContext) |
| `src/Apps/SymphoniaLegato.Desktop/Controls/PianoKeyboardControl.cs` | Piano keyboard rendering |
| `src/Apps/SymphoniaLegato.Desktop/Services/ScorePngExporter.cs` | PNG export (Avalonia RenderTargetBitmap) |
| `src/Apps/SymphoniaLegato.Desktop/Views/MainWindow.axaml` | Main window layout |
| `src/Apps/SymphoniaLegato.Desktop/Views/AboutWindow.axaml` | About dialog |
| `src/Apps/SymphoniaLegato.Desktop/Program.cs` | DI registration & app entry point |
| `src/Apps/SymphoniaLegato.Desktop/Themes/SymphoniaTheme.axaml` | Dark theme tokens |
| `src/Apps/SymphoniaLegato.Desktop/Themes/HighContrastTheme.axaml` | High-contrast theme |
| `src/Apps/SymphoniaLegato.Desktop/ViewModels/MetronomeViewModel.cs` | Metronome sidebar VM |
| `src/Apps/SymphoniaLegato.Desktop/ViewModels/SyncSettingsViewModel.cs` | Cloud sync settings VM |
| `src/Apps/SymphoniaLegato.Android/App.axaml.cs` | Android DI wiring & app entry |
| `src/Apps/SymphoniaLegato.Android/Views/MainShellView.axaml` | Android bottom-nav shell |
| `docs/ROADMAP.md` | Phase-by-phase feature plan |
| `docs/ANDROID.md` | Android build + deploy + architecture guide |

---

## Known pitfalls — read before touching these areas

1. **Avalonia `InputGesture`** — use `OemPlus`, `OemMinus`, `D0`–`D9` for +/-/digit keys.
   `Plus` / `Minus` / `0` throw `ArgumentException` at startup and crash the app.

2. **SkiaSharp packages** — `SkiaSharp.Views.Avalonia` and `Avalonia.SkiaSharp` do not exist
   on NuGet. All rendering uses `Avalonia.Media.DrawingContext`. Use `CubicBezierTo`, not `BezierTo`.

3. **Cross-platform TargetFramework** — use `net9.0` for Desktop, `net9.0-android` for Android.
   Supply `-r linux-x64` / `-r osx-arm64` / `-r android-arm64` at publish time.

4. **DryWetMidi name collisions** — `Melanchall.DryWetMidi.Interaction` exports `Note` and
   `TimeSignature` which clash with domain types. Use `using` aliases in any file that imports both.

5. **GenerateDocumentationFile** — the Core libraries set it to `true` (public API docs).
   The Desktop/Android apps inherit `false` from `Directory.Build.props`. CS1591 is suppressed
   globally via `<NoWarn>$(NoWarn);1591</NoWarn>` so missing doc comments never block the build.

6. **`IScoreRepository` double-registration** — only register the concrete `EnScoreRepository`
   once; the interface binding points to it via a lambda. A second `AddSingleton<IScoreRepository>`
   would shadow the first.

7. **`Grid.RowSpacing` / `Grid.ColumnSpacing`** do not exist in Avalonia 11. Use `StackPanel`
   with `Spacing`, or add `Margin` to individual children.

8. **`AutomationProperties.HeadingLevel` / `.Label`** — not available in Avalonia 11. Use
   `AutomationProperties.Name` for accessible labels on controls.

9. **Theme switching** — call `App.SetHighContrast(bool)` which adds/removes `HighContrastTheme.axaml`
   from `Application.Styles`. High-contrast styles override dark theme resources because they
   are appended later in the styles list.

10. **PDF export** — `ScorePdfExporter.GenerateFromImages()` takes pre-rendered PNG bytes (from
    `ScorePngExporter`). The PDF layer does not render notation itself; it embeds images into pages.
    Use QuestPDF 2024.x API: `.Image(bytes).FitArea()` (not the deprecated `ImageScaling` enum).

11. **New score starts blank** — `Score.CreatePianoScore()` creates staves with no measures.
    Always call `editor.AddMeasures(0, 4)` after creating a new score in the VM so ScoreCanvas
    has content to render.

12. **`ExtendClientAreaToDecorationsHint`** — do NOT set this on MainWindow; it removes the
    native title bar and breaks window dragging. The window is draggable via the OS title bar.

13. **Android workload** — `net9.0-android` requires `dotnet workload install android` (one-time,
    ~1 GB). The main `SymphoniaLegato.sln` excludes the Android project so Desktop always builds
    cleanly without the workload. Use `SymphoniaLegato.Android.sln` to build the APK.

14. **MetronomeEngine fires on a background thread** — `Beat` events come from `System.Timers.Timer`.
    ViewModels must marshal to the UI thread via `Dispatcher.UIThread.Post(...)` before touching
    observable properties.

---

## Phase status

| Phase | Status | Summary |
|-------|--------|---------|
| 1 — MVP | ✅ Done | Domain model, notation engine, layout, MIDI playback, save/load, 38 tests |
| 2 — Advanced notation | ✅ Done | Noteheads, stems, beams, accidentals, rests, clefs, dynamics, hairpins, slurs, articulations, lyrics, hand coloring, MIDI import, 55 tests |
| 3 — Professional | ✅ Done | PDF/PNG/SVG export, Score Properties dialog, Plugin Manager, Git History panel, MIDI/Audio settings, High Contrast theme, accessibility labels, file pickers, 74 tests |
| 4 — Android | ✅ Done | MetronomeEngine + Desktop panel, ScoreSyncService + Desktop dialog, ScoreAnnotation model, full Android Avalonia app in `SymphoniaLegato.Android.sln`, 88 tests |
| 5 — AI | ⏳ Planned | Harmonisation, chord detection, fingering, analysis |

---

## Conventions

- **XML doc comments** (`///`) are expected on all `public` members in Core libraries
  (`GenerateDocumentationFile=true`). App code (Desktop/Android) has no doc comments.
- **No inline comments** unless the *why* is non-obvious to a reader unfamiliar with the code.
- All score mutations via `ScoreEditor.Execute(IScoreCommand)` — never mutate `Score` directly.
- ViewModels must not import Avalonia UI types; use events to ask the View to open dialogs.
- Tests use `xUnit` + `FluentAssertions`. Naming: `Method_StateUnderTest_Expected`.
- Conventional Commits for messages: `feat:`, `fix:`, `test:`, `docs:`, `refactor:`.
