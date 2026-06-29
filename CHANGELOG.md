# Changelog

**Symphonia Legato** by Jose Jorge Hernandez.
All notable changes are documented here.
Format: [Semantic Versioning](https://semver.org/).

---

## [Unreleased]

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
