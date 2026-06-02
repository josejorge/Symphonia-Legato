# Roadmap

## Phase 1 — Minimal Viable Product

**Goal:** A usable piano score editor with playback.

- [x] Core domain models (Pitch, Duration, Note, Measure, Staff, Score)
- [x] ScoreEditor with full undo/redo
- [x] StaffPositionCalculator
- [x] MusicXML import/export
- [x] .enscore file format
- [x] MIDI playback engine
- [x] Layout engine (basic)
- [x] Avalonia Desktop app shell
- [x] Score canvas (SkiaSharp)
- [x] Virtual piano keyboard
- [x] Mixer panel
- [x] Dark theme
- [ ] Clef rendering (SMuFL glyphs)
- [ ] Note head rendering (open/closed, stems, beams)
- [ ] Accidental rendering
- [ ] Rest rendering
- [ ] Basic MIDI file import

## Phase 2 — Advanced Notation

- [ ] Dynamics (hairpins, sfz, fp)
- [ ] Lyrics
- [ ] Slurs and ties (curved paths)
- [ ] Articulations (staccato, accent, tenuto, fermata)
- [ ] Tuplets (triplets, quintuplets, …)
- [ ] Text annotations
- [ ] Tempo markings
- [ ] Repeats and volta brackets
- [ ] Automatic collision detection
- [ ] Measure balancing across systems
- [ ] Hand coloring (right = blue, left = red)

## Phase 3 — Professional Features

- [ ] PDF export (multi-page, professional quality)
- [ ] PNG export
- [ ] SVG export
- [ ] SoundFont playback (sf2/sf3)
- [ ] Plugin system (instruments, exporters)
- [ ] Git history UI (visual diff, restore)
- [ ] Cloud sync module (GitHub, Nextcloud, Dropbox)
- [ ] Score properties dialog
- [ ] Accessibility (keyboard navigation, screen readers)
- [ ] High contrast theme

## Phase 4 — Android Companion

- [ ] Android app skeleton
- [ ] Score viewing (read-only render)
- [ ] Playback
- [ ] Metronome
- [ ] Loop sections
- [ ] Pencil annotations
- [ ] Cloud sync with desktop

## Phase 5 — AI Extensions

- [ ] Harmonisation suggestions
- [ ] Chord detection
- [ ] Fingering suggestions
- [ ] Automatic accompaniment
- [ ] Score analysis
- [ ] Practice recommendations
