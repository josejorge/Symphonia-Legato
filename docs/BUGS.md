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

[Internal] **Measure width computed from only the first staff, cramming a
busier second staff** — Symptom (user-reported, with screenshot): on a grand
staff, a measure whose bass clef had more/shorter notes than the treble clef
rendered with all the bass notes squeezed together, while other measures
looked normally spaced. Root cause:
`LayoutEngine.EstimateMeasureWidth` read
`score.Parts.SelectMany(p => p.Staves).FirstOrDefault()` — the treble staff
only — to size each measure's shared column width, completely ignoring every
other staff. A reproduction test confirmed the exact inversion: a measure
with a busy bass staff came out *narrower* (256px) than a simple neighboring
measure (418px), because only the sparse treble was ever consulted. Fix
(2026-09-15): width is now the max of every staff's note content at that
measure, not just the first one's. File:
`src/Core/SymphoniaLegato.LayoutEngine/LayoutEngine.cs`. Covered by
`tests/SymphoniaLegato.Integration.Tests/LayoutEngineTests.cs` (verified to
fail against the pre-fix code before confirming the fix).

[Internal] **MusicXML import silently dropped dotted-note dots** — Symptom:
exporting a dotted note to MusicXML and reading it back lost the dot — a
dotted eighth came back as a plain note. Root cause:
`MusicXmlExporter.BuildNoteElement` correctly writes `<dot/>` elements, but
`MusicXmlImporter` never read them (or the `<type>` element) — it
reverse-engineered `NoteValue` purely by finding the closest plain duration
to the raw `<duration>` tick count, a lossy guess that has no concept of
dots at all. Found and proven by a new round-trip test
(`RoundTrip_PreservesNotePitchDurationAndDots`) before being fixed. Fix
(2026-09-15): read `<type>` and count `<dot>` elements directly instead of
guessing from ticks. File:
`src/Core/SymphoniaLegato.ImportExport/MusicXmlImporter.cs`.

[Internal] **MusicXML export silently drops every staff but the first** —
Symptom: exporting a piano (grand staff) score to MusicXML keeps only the
treble clef — the entire bass clef / left hand vanishes with no warning.
Root cause: `MusicXmlExporter.BuildDocument` reads
`part.Staves.FirstOrDefault()` — the importer has the same shape (`part.
Staves.Add(staff)`, always exactly one). MusicXML represents multiple staves
per part via `<staff>` markers inside each `<note>`, which neither side
reads or writes. Not fixed (2026-09-15) — this is real feature work (proper
multi-staff MusicXML support), scoped out of a "write round-trip tests"
pass; documented here plus a permanent characterization test
(`KnownGap_ExportingAGrandStaff_SilentlyDropsTheSecondStaff` in
`tests/SymphoniaLegato.Integration.Tests/MusicXmlRoundTripTests.cs`) so it
doesn't get rediscovered from scratch, and so the test itself starts failing
(on purpose) the moment someone fixes it — a signal to replace it with real
multi-staff coverage. This is a data-loss bug, not a stylistic gap — treat
as higher priority than the missing-markup items below. File:
`src/Core/SymphoniaLegato.ImportExport/MusicXmlExporter.cs`,
`src/Core/SymphoniaLegato.ImportExport/MusicXmlImporter.cs`.

[Internal] **MusicXML export writes no notation markup at all** — Symptom:
slurs, hairpins, dynamics, lyrics, articulations, and ties all silently
disappear on export — `MusicXmlExporter` only ever writes pitch/rest,
duration, dots, and chord notes. Not fixed (2026-09-15) — real feature work,
scoped out of a "write round-trip tests" pass; documented with a permanent
characterization test
(`KnownGap_SlursHairpinsDynamicsLyricsAndTies_AreNotExported`) so it's an
explicit, tracked gap rather than a silent one. Lower priority than the
missing-second-staff bug above (this is "notation looks plainer than
intended," not "half the music vanishes"). File:
`src/Core/SymphoniaLegato.ImportExport/MusicXmlExporter.cs`.

[Internal] **The "Loop" menu item showed `Ctrl+L` but nothing was bound to
it at all** — Symptom: pressing Ctrl+L did nothing; the menu item had no
`Command`, and `MainWindow.OnKeyDown` doesn't handle Ctrl-modified keys in
the first place (it returns early whenever Control/Alt/Meta is held, so a
`Ctrl+L` case couldn't have worked there either). Looping itself worked fine
via the toolbar's Loop toggle button (`PlaybackViewModel.IsLooping`) — only
the menu item and its advertised shortcut were dead. Found while building
the shortcut cheat-sheet (2026-09-15) and cross-checking every advertised
`InputGesture` against actual command bindings. Fix: added
`PlaybackViewModel.ToggleLoopCommand`, bound it to the menu item, and changed
the shortcut to bare `L` (consistent with Space/Escape, which are also
unmodified transport keys handled directly in `OnKeyDown` — `Ctrl+L` was
never reachable there to begin with). File:
`src/Apps/SymphoniaLegato.Desktop/ViewModels/PlaybackViewModel.cs`,
`src/Apps/SymphoniaLegato.Desktop/Views/MainWindow.axaml(.cs)`.

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

[External] **`claude_memory/` leaked private cross-project data into this public
repo** — Symptom: user spotted references to an unrelated client project
("Jireh") and machine paths while reviewing docs. Root cause: the global
Claude Code memory-mirror hook (`sync-claude-memory.ps1`) mirrors this
machine's *entire* memory folder for its `H:\DEV` working-directory grouping —
which spans every project under that root, not just this one — into any
git-enabled repo it touches. `claude_memory/` was never gitignored, so it was
committed and pushed to the public GitHub remote (commit `2c1c79b`), exposing
another client's production SSH host/username/deploy paths and other unrelated
project notes. Fix (2026-09-16): purged `claude_memory/` from every commit via
`git-filter-repo` and force-pushed; added `claude_memory/` to `.gitignore`
(the local folder still exists for this machine's own use, just untracked).
See `CLAUDE.md`'s "Compliance" section for the standing deviation from the
global rule. File: `.gitignore`.
