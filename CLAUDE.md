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

# Run all tests (104 passing)
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
    SymphoniaLegato.AIEngine          # ChordDetector, FingeringAdvisor, ClaudeAIEngine (Phase 5)
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
| `src/Core/SymphoniaLegato.Rendering/ScoreCanvas.cs` | Score rendering (DrawingContext) — shared library |
| `src/Apps/SymphoniaLegato.Desktop/Controls/PianoKeyboardControl.cs` | Piano keyboard rendering |
| `src/Apps/SymphoniaLegato.Desktop/Services/ScorePngExporter.cs` | PNG export (Avalonia RenderTargetBitmap) |
| `src/Apps/SymphoniaLegato.Desktop/Views/MainWindow.axaml` | Main window layout |
| `src/Apps/SymphoniaLegato.Desktop/Views/AboutWindow.axaml` | About dialog |
| `src/Apps/SymphoniaLegato.Desktop/Program.cs` | DI registration & app entry point |
| `src/Apps/SymphoniaLegato.Desktop/Themes/SymphoniaTheme.axaml` | Dark theme tokens |
| `src/Apps/SymphoniaLegato.Desktop/Themes/HighContrastTheme.axaml` | High-contrast theme |
| `src/Apps/SymphoniaLegato.Desktop/ViewModels/MetronomeViewModel.cs` | Metronome sidebar VM |
| `src/Apps/SymphoniaLegato.Desktop/ViewModels/SyncSettingsViewModel.cs` | Cloud sync settings VM |
| `src/Apps/SymphoniaLegato.Desktop/ViewModels/AIAssistantViewModel.cs` | AI assistant sidebar VM |
| `src/Apps/SymphoniaLegato.Desktop/Views/AIAssistantPanel.axaml` | AI assistant sidebar panel |
| `src/Core/SymphoniaLegato.AIEngine/ClaudeAIEngine.cs` | `IAIEngine` implementation (Claude API + offline) |
| `src/Core/SymphoniaLegato.AIEngine/ChordDetector.cs` | Algorithmic chord detection |
| `src/Core/SymphoniaLegato.AIEngine/FingeringAdvisor.cs` | Algorithmic fingering suggestions |
| `src/Core/SymphoniaLegato.Core/Models/AIModels.cs` | AI result types (ChordLabel, FingeringResult, etc.) |
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

15. **Raw interpolated string literals with JSON examples (CS9006)** — In `$"""..."""`, any `{`
    in content starts an interpolation; you cannot have literal `{`. Use `$$"""..."""` instead:
    single `{`/`}` in content becomes literal, and `{{expr}}` is interpolation. See `ClaudeAIEngine.cs`.

16. **`IAIEngine` AI models live in `Core.Models`** — `ChordLabel`, `FingeringResult`, `AIAnalysisResult`,
    `PracticeRecommendation`, `HarmonyMeasure` are in `SymphoniaLegato.Core.Models.AIModels.cs` (not
    in `AIEngine`). This avoids the circular-dependency that would arise if `Core.Interfaces.IAIEngine`
    imported from `AIEngine`.

17. **`RenderedMeasure.ClefType` is the enum, not the struct** — When converting a click position to a
    note, `measure.ClefType` is `ClefType` (enum) but `StaffPositionClickedEventArgs` expects `Clef`
    (struct). Convert with a switch: `Clef.Bass`, `Clef.Alto`, `Clef.Tenor`, default `Clef.Treble`.

18. **`ScoreCanvas` must override `MeasureOverride`** — Without it the control reports zero size to the
    `ScrollViewer` and the score never scrolls. Return `new Size(maxPageWidth, totalPagesHeight)` from
    the `LayoutResult`. The `Render` override alone is not enough to size the control.

19. **Chord notes in `Note.ChordNotes` store `Pitch`, not `Note`** — They have no `StaffPosition`
    property. The layout engine must compute staff positions via `PitchToStaffPosition(pitch, clef)`
    using diatonic distance from the clef's `BottomLineMidi` reference note.

20. **The score page is white paper / black ink — by design.** `ScoreCanvas` deliberately renders
    the sheet as off-white paper (`RGB 252,251,248`) with near-black notation, *independent* of the
    dark app chrome. Do **not** "theme" it dark to match the menus — that is exactly the bug that
    made the whole sheet invisible (the page colour matched `EditorBackground` `#252526`). See
    `docs/BUGFIXES.md` #1.

21. **Playback must (re)load the current score before Play.** The MIDI engine's `_midiFile` is built
    only by `LoadScoreAsync`. `PlaybackViewModel` reloads from `ScoreProvider` (wired in
    `MainWindowViewModel` to the live `ScoreEditor.Editor.Score`) at the start of playback, so newly
    entered notes are heard. Never assume the engine already has the current score.

22. **MIDI output device is opened lazily via `MidiPlaybackEngine.EnsureOutputDevice()`** — shared by
    `PlayAsync` *and* `PreviewNoteAsync` (the piano keyboard). Don't open the device only in `PlayAsync`
    or key preview goes silent until the first Play.

23. **`ScoreCanvas` property changes must `InvalidateMeasure()` too**, not just `InvalidateVisual()`.
    `MeasureOverride` feeds the `ScrollViewer` extent; without re-measuring, the scroll area is stale
    after zoom or adding measures (pairs with pitfall #18).

24. **There are two `Zoom` properties.** `ScoreCanvas` binds to `ScoreEditorViewModel.Zoom` (recomputes
    layout); the toolbar/menu live on `MainWindowViewModel`. Route window-level zoom commands through
    `ScoreEditor.ZoomInCommand/…` and mirror the value back for the percentage label — don't set only
    the window's local `Zoom`.

25. **Note entry flows across measures.** `ScoreEditorViewModel.ResolveTargetMeasure` advances past full
    measures and appends new ones (on every staff) so notes never overfill a single measure. `Measure.AddNote`
    sets `TickOffset = UsedTicks`, so adding past capacity used to overlap past the bar line. The layout also
    defensively scales by `max(capacity, contentSpan)`. Don't reintroduce single-measure overfill.

26. **Playback cursor uses absolute domain ticks.** `ScoreCanvas.PlaybackTick` is the absolute tick
    (1 quarter = 1024); the canvas walks measures advancing by each measure's `TicksPerMeasure` capacity —
    this must match how `ScoreToMidiConverter` advances time. `MainWindowViewModel` computes ticks from the
    engine `Position` and the score's `InitialTempo`, and resets `PlaybackTick = -1` when playback stops.
    `RenderedMeasure.NotesStartX/NotesEndX` and `RenderedNoteElement.TickOffset/DurationTicks` exist for this.

27. **The engine reports position on a 50 ms `System.Timers.Timer`** (background thread). Any ViewModel
    handling `PositionChanged` must marshal with `Dispatcher.UIThread.Post` before touching observable
    properties (same rule as the metronome, pitfall #14).

28. **Play-from-here uses a pending seek.** `MidiPlaybackEngine.SeekAsync` stores the position in
    `_pendingSeek` and applies it via `Playback.MoveToTime` when `PlayAsync` (re)builds the `Playback`
    (the object doesn't exist until then). `PlaybackViewModel.StartTick` (set when a note is clicked)
    is converted to a time and seeked before play; `Stop`/`Rewind` reset it to 0.

29. **Audible metronome = GM percussion clicks** (channel 9 / MIDI ch 10), not the `MetronomeEngine`
    (which is silent / events only). The per-beat click is **baked into the MIDI** by
    `ScoreToMidiConverter.BuildMetronomeTrack` when `Convert(score, includeMetronome: true)` — so it is
    sample-accurate. Count-in is a short pre-roll using `MidiPlaybackEngine.SendClick`. The converter
    deliberately skips channel 9 for instruments so it stays free for clicks.

30. **`StaffPositionCalculator.FromStaffPosition` MUST stay the exact inverse of `Calculate`.** A
    round-trip test enforces it. The old version was off by a diatonic step + an octave, so
    click-entered notes sounded wrong (they drew correctly). Keyboard entry (`EnterNoteByName`) and
    click entry both depend on this round-trip.

31. **Keyboard note entry lives in `MainWindow.OnKeyDown`** → `ScoreEditorViewModel.EnterNoteByName`.
    It only fires with no Ctrl/Alt modifier and when focus isn't a `TextBox`. Octave is picked nearest
    the previous note on the staff (`NearestPitch`), or a clef default for the first note.

---

## Phase status

| Phase | Status | Summary |
|-------|--------|---------|
| 1 — MVP | ✅ Done | Domain model, notation engine, layout, MIDI playback, save/load, 38 tests |
| 2 — Advanced notation | ✅ Done | Noteheads, stems, beams, accidentals, rests, clefs, dynamics, hairpins, slurs, articulations, lyrics, hand coloring, MIDI import, 55 tests |
| 3 — Professional | ✅ Done | PDF/PNG/SVG export, Score Properties dialog, Plugin Manager, Git History panel, MIDI/Audio settings, High Contrast theme, accessibility labels, file pickers, 74 tests |
| 4 — Android | ✅ Done | MetronomeEngine + Desktop panel, ScoreSyncService + Desktop dialog, ScoreAnnotation model, full Android Avalonia app in `SymphoniaLegato.Android.sln`, 88 tests |
| 5 — AI | ✅ Done | `SymphoniaLegato.AIEngine`: chord detection + fingering (offline), harmonisation + score analysis + practice plan (Claude API), Desktop AI Assistant panel, 104 tests |
| Bugfix | ✅ Done | Score editing: correct clef/key on click, chord note positions, canvas scroll; toolbar: accidentals, slur, tie, mode toggle |
| Bugfix 2 | ✅ Done | Rendering & playback pass (2026-06-28): white-paper/black-ink canvas (was invisible dark-on-dark), Play now loads the live score (sound), piano-preview device, mixer populated, zoom actually zooms, canvas re-measures on zoom, MIDI tempo honoured. See `docs/BUGFIXES.md`. |
| Bugfix 3 | ✅ Done | Note flow & playback cursor (2026-06-29): notes flow across measures/lines instead of overlapping; moving playback indicator (cursor line + active-measure band + sounding-note highlight) with auto-scroll; live transport timer; audible note preview on entry; File ▸ Load Demo Score. See `docs/BUGFIXES.md`. |
| Features | ✅ Done | Play-from-here (click a note → playback starts there), audible per-beat metronome during playback, one-bar count-in. Transport toggles in `PlaybackControlsView`. |
| Features 2 | ✅ Done | Keyboard note entry (A–G + duration/rest/dot/transport keys), metronome baked into MIDI (sample-accurate), fixed `FromStaffPosition` pitch bug. `docs/TODO.md` backlog added. 111 tests. |

---

## Conventions

- **XML doc comments** (`///`) are expected on all `public` members in Core libraries
  (`GenerateDocumentationFile=true`). App code (Desktop/Android) has no doc comments.
- **No inline comments** unless the *why* is non-obvious to a reader unfamiliar with the code.
- All score mutations via `ScoreEditor.Execute(IScoreCommand)` — never mutate `Score` directly.
- ViewModels must not import Avalonia UI types; use events to ask the View to open dialogs.
- Tests use `xUnit` + `FluentAssertions`. Naming: `Method_StateUnderTest_Expected`.
- Conventional Commits for messages: `feat:`, `fix:`, `test:`, `docs:`, `refactor:`.
