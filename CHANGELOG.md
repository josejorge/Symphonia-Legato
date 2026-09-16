# Changelog

**Symphonia Legato** by Jose Jorge Hernandez.
All notable changes are documented here.
Format: [Semantic Versioning](https://semver.org/).

---

## [Unreleased]

### Security (2026-09-16 — private data purged from public history)
- **`claude_memory/` leaked private cross-project data**, already committed
  and pushed to the public GitHub remote — see `docs/BUGS.md` for the
  full root cause. Purged from every commit via `git-filter-repo`, force-
  pushed, and added `claude_memory/` to `.gitignore` so it can't be
  re-tracked by accident. Documented the standing deviation from the
  user's global CLAUDE.md memory-mirror rule in this project's own
  `CLAUDE.md`.
- **5 files still had `Company: Parlee Conseiller, Inc.`** in their branding
  headers instead of this project's agreed `N/A (personal open-source
  project, MIT licensed)` — missed during Tier 3 work. Fixed:
  `App.axaml.cs`, `Converters/EnumEqualsConverter.cs`, `MainWindow.axaml.cs`,
  `ScoreToMidiConverterTests.cs`, `technical_memory/technical_memory.ipynb`.

### Fixed (2026-09-15 — documentation audit after Tier 3)
- **Project-wide version was stuck at 3.0.0** despite Tier 0 through Tier 3
  all adding new files (no new folders) — per the versioning policy each of
  those should have bumped it. Caught up in one bump: every `.csproj` and
  `README.md` now read **3.1.0**; `CLAUDE.md`'s own "Project version" line
  was separately stale at 2.0.0 (predating even the 3.0.0 bump) — fixed too.
- **Stale test counts** in `README.md` (still said "104 tests", the
  pre-session count) and `docs/operations_guide.html` (said "117") — both
  now say 139. Also fixed `docs/operations_guide.html`'s and the Desktop
  module's "Theme (Dark / High Contrast)" configuration rows to include
  Light, and `Themes/SymphoniaTheme.axaml` / `HighContrastTheme.axaml`
  references in `src/Apps/SymphoniaLegato.Desktop/module.md` and
  `src/Core/SymphoniaLegato.PlaybackEngine/module.md` /
  `docs/operations_guide.html` to mention `LightTheme.axaml` and the new
  count-in behaviour respectively.
- **README's feature list overclaimed tuplets/ties as fully supported** —
  corrected to match `docs/KNOWN_ISSUES.md` (ties are modelled but
  entry/playback support is partial; tuplets aren't wired up at all).
- **`docs/CONFIGURATION.md`'s Theme row** still said "Dark / High Contrast",
  missed by the earlier pass above — now says "Dark / Light / High Contrast".
- **`docs/API_REFERENCE.md` was missing two Tier 3 API additions**:
  `ScoreEditor.Transpose(int semitones)` and `ScoreToMidiConverter`'s
  `includeCountIn` parameter / `ComputeCountInDuration(Score)` — both added.

### Added (2026-09-15 — TODO.md Tier 3: theme, shortcuts, transpose, count-in, MusicXML tests)
- **Light theme** — `Themes/LightTheme.axaml`, plus a proper `AppTheme` enum
  (Dark/Light/HighContrast) replacing the old boolean high-contrast toggle.
  `View ▸ Theme` submenu for direct selection; the toolbar button now cycles
  through all three. Persists via `AppSettingsService`.
- **Keyboard Shortcuts dialog** (`Help ▸ Keyboard Shortcuts`) — every entry
  cross-checked against actual `Command` bindings and `MainWindow.OnKeyDown`
  rather than copied from `InputGesture` labels, which caught a real bug (see
  Fixed, below). `docs/USER_MANUAL.md`'s hand-maintained shortcut table was
  replaced with a pointer to this dialog so the two can't drift apart.
- **Transpose** (`Score ▸ Transpose`) — shifts the whole score up/down a
  semitone or an octave (chromatic). Undo restores each note's exact original
  spelling from a snapshot, not by re-deriving it from the MIDI number (which
  would have silently turned flats into sharps).
- **Sample-accurate count-in** — `ScoreToMidiConverter.Convert(includeCountIn:
  true)` now bakes a one-bar click prefix directly into the MIDI file (every
  other track shifted later by exactly one bar) instead of firing clicks from
  a `Task.Delay` loop subject to OS timer jitter. `MidiPlaybackEngine`
  compensates a "play from here" seek target by the same amount so it still
  lands on the right note.
- **MusicXML round-trip fidelity tests** — 9 new tests
  (`tests/SymphoniaLegato.Integration.Tests/MusicXmlRoundTripTests.cs`)
  covering what survives a round trip today (title/composer, time/key
  signature, pitch/duration/dots/rests/chords) plus two permanent
  characterization tests documenting real gaps found while writing them (see
  Fixed and Known limitations, below).

### Fixed (2026-09-15 — found while building Tier 3)
- **MusicXML import silently dropped dotted-note dots.** The exporter wrote
  `<dot/>` correctly; the importer never read it (or `<type>`), reconstructing
  duration purely by guessing the closest plain value from the raw tick
  count. Now reads `<type>`/`<dot>` directly instead of guessing.
- **The "Loop" menu item showed `Ctrl+L` with nothing bound to it at all** —
  not even reachable from `MainWindow.OnKeyDown`, which returns early on any
  Ctrl-modified key. Looping itself worked fine via the toolbar's Loop
  toggle. Added `PlaybackViewModel.ToggleLoopCommand`, changed the shortcut
  to bare `L` (consistent with Space/Escape, the other unmodified transport
  keys).

### Known limitations found this pass (real feature work, not fixed here)
- **MusicXML export drops every staff but the first** — exporting a piano
  score keeps only the treble clef; the entire bass clef vanishes with no
  warning. Data loss, not a stylistic gap — see `docs/BUGS.md` and
  `docs/TODO.md` for why this was scoped out of a "write tests" pass.
- **MusicXML export writes no notation markup** — slurs, hairpins, dynamics,
  lyrics, articulations, and ties are silently dropped. Lower priority than
  the missing-staff bug above.

### Fixed (2026-09-15 — grand-staff measure width bug, user-reported with screenshot)
- **A measure's width was computed from only the treble staff**, ignoring
  every other staff. On a grand staff, a measure where the bass clef had
  more/shorter notes than the treble rendered with the bass notes squeezed
  together while other measures looked fine — the shared column width was
  sized for the sparser staff. `LayoutEngine.EstimateMeasureWidth` now takes
  the max note-content width across every staff at that measure. Added
  `tests/SymphoniaLegato.Integration.Tests/LayoutEngineTests.cs`, confirmed
  to fail against the pre-fix code (busy measure came out narrower than a
  simple one) before verifying the fix inverts it correctly.

### Added (2026-09-15 — TODO.md Tier 2: mixer, settings persistence, chord entry)
- **Mixer → playback actually connected.** Volume/Pan/Mute/Solo changes in the
  mixer panel now write straight back into the `Staff` (so the next Play
  reflects them, even if the change was made while stopped) via a new
  `ScoreEditor.MarkDirty()` — deliberately outside the undo/redo command
  system, the same treatment `Zoom` already gets, since these are continuous
  mixing controls rather than discrete notation edits. `ScoreToMidiConverter`
  now also honours `IsSolo` (previously never read at all): soloing any staff
  mutes every other staff for that playback.
- **Settings actually persist** — new `AppSettingsService`
  (`%AppData%\SymphoniaLegato\settings.json`): theme, MIDI output device,
  cloud sync folder, and the last 10 recent files all survive a restart now.
  Deliberately excludes the Claude API key (stays session-only — see
  `docs/AUTHENTICATION.md`) and the SoundFont path (not wired to a real synth
  yet, so persisting it would imply a working feature that doesn't exist).
- **`File ▸ Recent Files` menu** — capped at 10, newest first, stale
  (deleted/moved) entries pruned automatically.
- **Chord entry** — Shift+A…G stacks a pitch onto the currently selected note
  instead of creating a new one (`AddChordPitchCommand`), with undo/redo
  support. Octave chosen nearest the note's own root pitch. No-op on a rest
  or an exact-duplicate pitch.

### Fixed (2026-09-15 — found while wiring settings persistence)
- **MIDI output device selection never actually worked.** Choosing a
  different device in MIDI/Audio Settings and clicking Apply did nothing —
  playback was hardcoded to the first device on the system
  (`OutputDevice.GetByIndex(0)`). Added `IPlaybackEngine.SetOutputDevice
  (string?)`; the Settings dialog now actually changes what plays, and the
  choice persists.

### Added (2026-09-15 — full standards compliance: headers, scaffold, docs)
- **Branding headers on all 140 pre-existing source files** (116 `.cs` + 24
  `.axaml`) — File/Description/Author/Company/Date/Last edit date/Version,
  with `Date` pulled from each file's actual first-commit date via
  `git log --follow`, and `Description` drawn from each type's existing XML
  doc summary where one existed.
- **Baseline folder scaffold** — `.github/`, `.vscode/`, `.devcontainer/`,
  `scripts/`, `tools/`, `config/`, `database/`, `infrastructure/`,
  `integrations/`, `assets/`, `public/`, `artifacts/`, `templates/`,
  `samples/`, `localization/`, `security/`, `licenses/`, `tmp/` — each with a
  `.gitkeep` placeholder.
- **Project version bumped to 3.0.0** (README.md + every `.csproj`) — new
  folders added again this pass. Also found `SymphoniaLegato.AIEngine.csproj`
  had no `<Version>` element at all (every sibling project has one) — added it.
- **19 previously-missing standard docs**: `API.md`, `WEBHOOKS.md`,
  `INTEGRATIONS.md`, `AUTHENTICATION.md`, `DEBUGGING.md`,
  `TROUBLESHOOTING.md`, `OPERATIONS.md`, `RUNBOOK.md`, `DEPENDENCIES.md`,
  `PERFORMANCE.md`, `DATABASE.md`, `DATA_MODEL.md`, `KNOWN_ISSUES.md`,
  `FAQ.md`, `CREDITS.md`, `AUTHORS.md`, `STYLE_GUIDE.md`,
  `CONFIGURATION.md`, `DEVELOPMENT.md`. `docs/` now has all 27 standard
  files plus the project's own extras (`ANDROID.md`, `API_REFERENCE.md`,
  `PLUGIN_SDK.md`, `UI_GUIDELINES.md`, `USER_MANUAL.md`, `BUGFIXES.md`).

### Fixed (2026-09-15 — repo hygiene found while adding the scaffold)
- **`.gitignore`'s `.vscode/` exceptions were silently non-functional** — the
  blanket pattern used a trailing slash (directory-level exclude), which
  prevents git from ever re-including files below it, so the existing
  `!.vscode/settings.json`-style lines never worked. Changed to `.vscode/*`.
- **`artifacts/` was blanket-gitignored** as generic "build output" boilerplate,
  which would have silently swallowed the tracked Jupyter-notebook artifacts
  directory required by `CLAUDE.md`. Removed that line (real build output
  already goes to `bin/`/`obj/`, which stay ignored).
- **Dead dependency found**: `Microsoft.Data.Sqlite` is referenced in
  `SymphoniaLegato.Desktop.csproj` but never used anywhere in the codebase —
  flagged in `docs/DEPENDENCIES.md` and `docs/BUGS.md`, not removed (a code
  change, out of scope for a docs pass).

### Added (2026-09-15 — repo-standards compliance pass)
- **`docs/BUGS.md`** — the authoritative `[Internal]/[External]`-tagged bug log;
  `docs/BUGFIXES.md` stays as the fuller narrative write-ups.
- **`.assetignore`** at the repo root, alongside `.gitignore`.
- **`module.md`** in all 12 module directories (`src/Core/*`, `src/Apps/*`),
  each documenting purpose, dependency position, and key files.
- **`docs/operations_guide.html` + `docs/executive_overview.html`** — at the
  project root and inside every one of the 12 modules (26 HTML files total),
  offline-friendly, print/PDF/Word-exportable, no external dependencies.
- **Project version bumped to 2.0.0** (README.md + every `.csproj`) — new
  folders were added across the repo this pass (12 module `docs/` folders,
  `technical_memory/`, `claude_memory/`), which is a major bump per the
  project's versioning policy.

### Added (2026-09-15 — TODO.md Tier 0/1 triage)
- **MIDI export actually writes a `.mid` file.** `File ▸ Export ▸ MIDI...` was
  wired to a "use playback controls" status-message stub with nothing behind it;
  it now converts the current score via `ScoreToMidiConverter` and writes it
  through the save-file picker, same pattern as PNG/PDF/SVG/MusicXML export.
- **Real tests for `SymphoniaLegato.PlaybackEngine.Tests`.** The project was wired
  into the solution but contained zero test files — it silently contributed 0 of
  the "111 tests" milestone claimed in `CLAUDE.md`. Added 6 tests covering
  `ScoreToMidiConverter`: tick conversion, tempo, GM percussion channel
  reservation, chord-note simultaneity, and metronome click generation.

### Fixed (2026-09-15)
- **Duration toolbar radio buttons didn't reflect keyboard duration changes.**
  Pressing `1`–`6` updated `SelectedDuration` but the toolbar's radio-button
  highlight never moved because `IsChecked` wasn't bound to it. Added
  `EnumEqualsConverter` and bound each duration `RadioButton.IsChecked` to
  `SelectedDuration.Value` so the toolbar now tracks both mouse and keyboard entry.

### Repo hygiene (2026-09-15)
- Removed `msbuild.binlog` and the stale `run_err*.txt` / `run_out*.txt` debug
  artifacts from git (untracked + deleted); added `*.binlog`, `run_out*.txt`,
  `run_err*.txt` to `.gitignore`. `run_err.txt` was a startup-crash log from
  before commit `3d64da4` (invalid `InputGesture="Plus"`) that `docs/BUGFIXES.md`
  already flagged as stale — the app builds, tests, and launches cleanly today.

### Added (2026-06-29 — playback cursor, note flow & extras)
- **Playback indicator** — a moving cursor sweeps the sheet during playback: a
  vertical line at the current beat, a translucent band over the active measure,
  and a highlight on the note(s) currently sounding. The view auto-scrolls to
  follow it.
- **Live transport** — the engine now reports position on a timer, so the time
  display and measure/beat counters update during playback.
- **Audible note preview** — entering a note (clicking the staff) now plays it.
- **Load Demo Score** (File menu) — loads "Ode to Joy" across 8 bars so you can
  immediately hear playback, see the cursor, and see multi-line wrapping.
- **Play from a note** — click any note to start playback from that point.
- **Audible metronome** — a per-beat click during playback (🥁 toggle) plus an
  optional **one-bar count-in** (⏱ toggle) before playback starts. The per-beat
  click is **baked into the MIDI** (sample-accurate, no timer jitter).
- **Keyboard note entry** — type `A`–`G` to place notes (octave nearest the
  previous note, key-signature aware, flowing across bars); `1`–`6` durations,
  `R` rest, `.` dot, `Delete` remove, `Space` play/pause, `Esc` stop.
- Added a forward-looking backlog at **`docs/TODO.md`**.

### Fixed (2026-06-29 — pitch round-trip)
- **Click-entered notes played the wrong pitch.** `StaffPositionCalculator.FromStaffPosition`
  was not the inverse of `Calculate` (off by a diatonic step and an octave), so a note
  clicked on the treble bottom line stored as F5 instead of E4. Fixed and covered by a
  round-trip test. (Notes drew correctly but sounded wrong.)

### Fixed (2026-06-29 — note flow)
- **Notes no longer overflow/overlap past the bar line.** Note entry now flows
  across measures: when a measure is full the note goes to the next one, creating
  new measures (and therefore new systems/lines) as needed. The layout also
  defensively compresses any over-full measure so notes can never spill past the
  closing bar line.

### Fixed (2026-06-28 — rendering & playback pass; see `docs/BUGFIXES.md`)
- **Score canvas was invisible** — the page rendered dark-grey on a dark editor
  background. The sheet is now white paper with black ink (Encore-style), with a
  page border and drop shadow.
- **No sound on Play** — the score was never loaded into the MIDI engine. Play
  now rebuilds MIDI from the live score, so entered notes are heard.
- **Piano keyboard preview was silent** until the first Play — the output device
  is now opened lazily and shared with note preview.
- **Mixer panel was empty** — it is now populated from the loaded score.
- **Zoom buttons/menu did nothing** — they now drive the score's zoom (layout
  recompute), and the canvas re-measures so scrollbars update.
- **Tempo was ignored** — playback now emits a `SetTempoEvent` from the score's
  `InitialTempo`.

### Added
- Core domain model: Pitch, Duration, Note, Measure, Staff, Score, Part
- ScoreEditor with full undo/redo command stack
- MusicXML 4.0 import and export
- .enscore file format (ZIP + MusicXML + JSON)
- MIDI playback engine (DryWetMidi)
- Layout engine with automatic measure distribution
- Avalonia desktop app shell (main window, menus, toolbar)
- Score canvas rendered directly with Avalonia `DrawingContext` (Skia-backed; no SkiaSharp package)
- Virtual piano keyboard control
- Mixer panel (per-staff volume, pan, mute, solo)
- Note input toolbar (duration, dot, rest)
- Symphonia dark theme
- Plugin host skeleton
- Git integration (LibGit2Sharp)
- xUnit test suite (PitchTests, DurationTests, ScoreTests, ScoreEditorTests)

---

## [0.1.0] — 2026-06-01

Initial project scaffold.
