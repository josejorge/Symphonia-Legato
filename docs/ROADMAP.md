# Roadmap

## Phase 1 — Minimal Viable Product ✅

**Goal:** A usable piano score editor with playback.

- [x] Core domain models (Pitch, Duration, Note, Measure, Staff, Score)
- [x] ScoreEditor with full undo/redo
- [x] StaffPositionCalculator, BeamCalculator, AccidentalProcessor
- [x] MusicXML import/export
- [x] .enscore file format (ZIP)
- [x] MIDI playback engine (DryWetMidi)
- [x] Proportional layout engine
- [x] Avalonia Desktop app shell (DI, MVVM, ViewLocator)
- [x] Virtual piano keyboard
- [x] Mixer panel
- [x] Dark theme
- [x] 38 passing tests

## Phase 2 — Advanced Notation ✅

- [x] Notehead rendering (open/filled/half-hole)
- [x] Stems, beams, flags (Bézier curves)
- [x] Accidentals (♯ ♭ ♮) with key-sig awareness
- [x] Rest rendering (whole/half/quarter/eighth/16th)
- [x] Clef, time-sig, key-sig symbols
- [x] Dynamics (pp, p, mp, mf, f, ff, sfz)
- [x] Hairpins (crescendo / decrescendo)
- [x] Slurs (cubic Bézier)
- [x] Articulations (staccato, accent, tenuto, fermata, marcato)
- [x] Lyrics (italic, syllable hyphens)
- [x] Tempo markings
- [x] Hand coloring (right = blue, left = red)
- [x] MIDI file import
- [x] About dialog (credited to Jose Jorge Hernandez)
- [x] 55 passing tests (all Phase 1 tests still pass)

## Phase 3 — Professional Features ✅

- [x] PDF export — QuestPDF with embedded PNG pages; title/composer header block
- [x] PNG export — Avalonia off-screen RenderTargetBitmap; multi-page support
- [x] SVG export — full notation drawing from LayoutResult; all pages
- [x] Score Properties dialog — title, subtitle, composer, lyricist, arranger,
       copyright, notes, tempo, time signature, page size
- [x] Plugin Manager dialog — load/unload plugins; pluginDir configuration
- [x] Git History panel — commit list, create commit, restore version (LibGit2Sharp)
- [x] MIDI / Audio Settings dialog — device picker, SoundFont file picker
- [x] High Contrast theme (WCAG 2.1 AA+ palette, dynamically swappable)
- [x] Accessibility — AutomationProperties.Name on interactive controls,
       LiveSetting on status bar
- [x] File picker dialogs (Open, Save As) wired to MainWindow
- [x] Export menu items fully wired (MusicXML, MIDI, PDF, PNG, SVG)
- [x] 74 passing tests (19 new Phase 3 integration tests)

## Phase 4 — Android Companion ⏳ Planned

- [ ] Android app skeleton
- [ ] Score viewing (read-only render)
- [ ] Playback
- [ ] Metronome
- [ ] Loop sections
- [ ] Pencil annotations
- [ ] Cloud sync with desktop

## Phase 5 — AI Extensions ⏳ Planned

- [ ] Harmonisation suggestions
- [ ] Chord detection
- [ ] Fingering suggestions
- [ ] Automatic accompaniment
- [ ] Score analysis
- [ ] Practice recommendations
