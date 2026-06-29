# Changelog

**Symphonia Legato** by Jose Jorge Hernandez.
All notable changes are documented here.
Format: [Semantic Versioning](https://semver.org/).

---

## [Unreleased]

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
