# Changelog

All notable changes to Symphonia Legato are documented here.
Format: [Semantic Versioning](https://semver.org/).

---

## [Unreleased]

### Added
- Core domain model: Pitch, Duration, Note, Measure, Staff, Score, Part
- ScoreEditor with full undo/redo command stack
- MusicXML 4.0 import and export
- .enscore file format (ZIP + MusicXML + JSON)
- MIDI playback engine (DryWetMidi)
- Layout engine with automatic measure distribution
- Avalonia desktop app shell (main window, menus, toolbar)
- SkiaSharp score canvas
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
