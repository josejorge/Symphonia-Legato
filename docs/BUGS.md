# Bugs — Symphonia Legato

Authoritative, tagged log of every gotcha hit in this project — internal (our own
code) or external (surprising/undocumented behavior from a dependency, API,
library, OS, or platform). Format:

`[Internal|External] **[Short bug title]** — Symptom: [ ]. Root cause: [ ]. Fix (or workaround, if external and unfixable on our end): [ ]. File: [ ].`

Full narrative write-ups (root-cause investigation, before/after) for the more
involved entries live in `docs/BUGFIXES.md` — this file is the scannable index.
The most load-bearing entries are also mirrored as "Known pitfalls" in the root
`CLAUDE.md` so a fresh session doesn't regress them.

---

[Internal] **Dark-on-dark score canvas** — Symptom: the music sheet appeared
empty; staff barely visible. Root cause: `ScoreCanvas.PageBrush` (`#262626`)
was nearly identical to `EditorBackground` (`#252526`), so the "paper" was
invisible against the app chrome. Fix: repainted the canvas as white paper /
near-black ink, independent of the dark app theme (see `CLAUDE.md` pitfall
#20). File: `src/Core/SymphoniaLegato.Rendering/ScoreCanvas.cs`.

[Internal] **Play produced no sound** — Symptom: notes entered, Play pressed,
silence. Root cause: `PlaybackViewModel.LoadScoreAsync` was never called from
anywhere, so `MidiPlaybackEngine._midiFile` stayed null and `PlayAsync` was a
silent no-op. Fix: rebuild the MIDI from the live `Score` at the start of every
`PlayPauseAsync`. File: `src/Apps/SymphoniaLegato.Desktop/ViewModels/PlaybackViewModel.cs`.

[Internal] **Virtual piano silent until first Play** — Symptom: clicking piano
keys before pressing Play produced no sound. Root cause: the MIDI output
device was only opened inside `PlayAsync`, not on first key preview. Fix:
extracted a shared lazy `EnsureOutputDevice()` used by both `PlayAsync` and
`PreviewNoteAsync`. File: `src/Core/SymphoniaLegato.PlaybackEngine/MidiPlaybackEngine.cs`.

[Internal] **Mixer panel always empty** — Symptom: no per-staff channels shown.
Root cause: `MixerViewModel.LoadScore(score)` was never invoked. Fix: call it
alongside the AI assistant load in `MainWindowViewModel.LoadScore`. File:
`src/Apps/SymphoniaLegato.Desktop/ViewModels/MainWindowViewModel.cs`.

[Internal] **Zoom controls did nothing** — Symptom: toolbar/menu zoom changed
the percentage label but not the rendered score. Root cause: two disconnected
`Zoom` properties — `MainWindowViewModel.Zoom` (bound by the toolbar) vs.
`ScoreEditorViewModel.Zoom` (bound by `ScoreCanvas`, the one that actually
recomputes layout). Fix: window-level zoom commands now delegate to
`ScoreEditor.ZoomInCommand/…`. File: `src/Apps/SymphoniaLegato.Desktop/ViewModels/MainWindowViewModel.cs`.

[Internal] **Scroll area stale after zoom/adding measures** — Symptom: wrong
scrollbar extent, clipped content. Root cause: `ScoreCanvas` called
`InvalidateVisual()` but not `InvalidateMeasure()` on layout changes, so
`MeasureOverride` never re-reported size to the `ScrollViewer`. Fix: call both.
File: `src/Core/SymphoniaLegato.Rendering/ScoreCanvas.cs`.

[Internal] **Playback ignored score tempo** — Symptom: every score played at
120 BPM regardless of `InitialTempo`. Root cause: `ScoreToMidiConverter.BuildTrack`
never emitted a `SetTempoEvent`. Fix: emit it once, on the first track. File:
`src/Core/SymphoniaLegato.PlaybackEngine/ScoreToMidiConverter.cs`.

[Internal] **Notes overlapped past the bar line** — Symptom (user-reported):
"when the amount of notes overlaps the size of the line, it doesn't create a
new line — it overlaps the notes in one line." Root cause: `Measure.AddNote`
always appended at `UsedTicks` with no capacity check; a full measure kept
accepting notes past its capacity. Fix: `ScoreEditorViewModel.ResolveTargetMeasure`
auto-flows into new measures, plus defensive layout compression as a backstop.
File: `src/Apps/SymphoniaLegato.Desktop/ViewModels/ScoreEditorViewModel.cs`,
`src/Core/SymphoniaLegato.LayoutEngine/LayoutEngine.cs`.

[Internal] **Click-entered notes played the wrong pitch** — Symptom: a note
clicked on the treble bottom line (E4) drew correctly but played as F5.
Root cause: `StaffPositionCalculator.FromStaffPosition` was not the exact
inverse of `Calculate` (missing a `-1` / an octave-shift term). Fix: made it
the exact inverse; added a `Calculate`↔`FromStaffPosition` round-trip test.
File: `src/Core/SymphoniaLegato.NotationEngine/StaffPositionCalculator.cs`.

[Internal] **Duration toolbar didn't track keyboard duration entry** —
Symptom: pressing `1`–`6` changed the active duration but the toolbar's radio
buttons never highlighted the new selection. Root cause: only the Quarter
`RadioButton` had a hardcoded `IsChecked="True"`; none were bound back to
`SelectedDuration`. Fix (2026-09-15): added `Converters/EnumEqualsConverter.cs`,
bound each button's `IsChecked` to `SelectedDuration.Value`. File:
`src/Apps/SymphoniaLegato.Desktop/Views/NoteInputToolbarView.axaml`.

[Internal] **`File ▸ Export ▸ MIDI...` did nothing** — Symptom: menu item set
a status message pointing at "playback controls," which have no export path
either — no file was ever written. Root cause: `MainWindow.OnExportAsync`'s
`"midi"` case never called `ScoreToMidiConverter`. Fix (2026-09-15): injected
`ScoreToMidiConverter` into `MainWindow`, added `ExportMidiAsync` following the
same picker pattern as the other exporters. File:
`src/Apps/SymphoniaLegato.Desktop/Views/MainWindow.axaml.cs`.

[Internal] **`tests/SymphoniaLegato.PlaybackEngine.Tests` was an empty stub** —
Symptom: wired into the solution and counted toward the "111 tests" milestone
in `CLAUDE.md`, but contained zero test files — `dotnet test` silently
reported "No test is available" for it. Root cause: the project was scaffolded
in an earlier phase and never actually populated. Fix (2026-09-15): added 6
tests covering `ScoreToMidiConverter` (tick conversion, tempo, GM percussion
channel reservation, chord simultaneity, metronome clicks). `MidiPlaybackEngine`
itself still has no tests (needs a fake output device abstraction first — see
`docs/TODO.md`). File: `tests/SymphoniaLegato.PlaybackEngine.Tests/`.

[External] **Avalonia `KeyGesture.Parse` rejects `"Plus"` / `"Minus"` / `"0"`**
— Symptom: `ArgumentException: Requested value 'Plus' was not found` thrown
from XAML population at startup, crashing the app before any window appeared.
Root cause: Avalonia's `InputGesture` parser expects `OemPlus`/`OemMinus`/
`D0`–`D9`, not the plain key-cap characters. Workaround: use `OemPlus`,
`OemMinus`, `D0`–`D9` in every `InputGesture` string (see `CLAUDE.md` pitfall
#1). This bug predates commit `3d64da4`; the stale crash log it left behind
(`run_err.txt`) was mistaken for current state during a 2026-09-15 audit and
has since been deleted from the repo. File:
`src/Apps/SymphoniaLegato.Desktop/Views/MainWindow.axaml`.

[External] **`SkiaSharp.Views.Avalonia` / `Avalonia.SkiaSharp` don't exist on
NuGet** — Symptom: package restore fails for either name. Root cause: no such
packages are published; Avalonia's own `Avalonia.Media.DrawingContext` is
Skia-backed internally and is the intended rendering surface. Workaround: use
`DrawingContext` directly (`CubicBezierTo`, not `BezierTo`). File: n/a
(convention, see `CLAUDE.md` pitfall #2).

[Internal] **`.vscode/` `.gitignore` exceptions were silently non-functional** —
Symptom: `!.vscode/settings.json` and similar negation lines existed in
`.gitignore` but did nothing. Root cause: the blanket pattern was
`.vscode/` (trailing slash — a directory-level exclude); git cannot re-include
files below a directory excluded that way, only below one excluded with `/*`.
Fix (2026-09-15): changed to `.vscode/*` so the existing negation lines
actually take effect. File: `.gitignore`.

[Internal] **`Microsoft.Data.Sqlite` is a dead dependency** — Symptom: none
visible at runtime — found during a 2026-09-15 dependency audit. Root cause:
`SymphoniaLegato.Desktop.csproj` references the package, but no
`SqliteConnection` or related type appears anywhere in `src/` or `tests/` —
likely a leftover from an earlier, never-implemented design direction. Fix:
not applied (removing a dependency is a code change) — flagged in
`docs/DEPENDENCIES.md` for the next session to either wire up or remove.
File: `src/Apps/SymphoniaLegato.Desktop/SymphoniaLegato.Desktop.csproj`.

[Internal] **MIDI output device selection never actually took effect** — Symptom:
choosing a different device in MIDI/Audio Settings and clicking Apply did
nothing audible — playback always used the first device. Root cause:
`MidiPlaybackEngine.EnsureOutputDevice()` was hardcoded to
`OutputDevice.GetByIndex(0)`; `MidiSettingsViewModel.Apply()` updated
`SelectedDeviceName` but never told the engine about it. Fix (2026-09-15):
added `IPlaybackEngine.SetOutputDevice(string?)`; `Apply()` now calls it and
persists the choice via the new `AppSettingsService`. File:
`src/Core/SymphoniaLegato.PlaybackEngine/MidiPlaybackEngine.cs`,
`src/Apps/SymphoniaLegato.Desktop/ViewModels/MidiSettingsViewModel.cs`.

[External] **`Grid.RowSpacing` / `Grid.ColumnSpacing` don't exist in Avalonia 11**
— Symptom: XAML fails to parse / properties silently ignored. Root cause:
Avalonia 11's `Grid` has no spacing properties (unlike WPF/UWP). Workaround:
use `StackPanel` with `Spacing`, or per-child `Margin`. File: n/a (convention,
see `CLAUDE.md` pitfall #7).
