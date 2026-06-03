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

## Phase 4 — Android Companion ✅

- [x] Desktop: Metronome panel (BPM, subdivision, tap tempo, beat display) — sidebar in Desktop app
- [x] Desktop: Cloud Sync dialog (push/pull .enscore to OneDrive / Dropbox / Nextcloud folder)
- [x] Core: MetronomeEngine (cross-platform, `System.Timers.Timer`, configurable BPM + subdivision)
- [x] Core: ScoreAnnotation model (freehand pencil strokes per page, normalised coords)
- [x] Core: ScoreSyncService (folder-based bidirectional sync, newer-wins strategy)
- [x] Android app scaffold — complete Avalonia Android project in `SymphoniaLegato.Android.sln`
  - [x] MainActivity + MainApplication (Avalonia Android entry points)
  - [x] Bottom-navigation shell (Library / Score / Play / Metronome tabs)
  - [x] Library browser (scan folder for .enscore, open scores)
  - [x] Score Viewer (ScoreCanvas read-only, page nav, zoom, pinch-to-zoom)
  - [x] Playback screen (transport controls + loop section with measure bounds)
  - [x] Metronome screen (tap tempo, subdivision, beat display)
  - [x] Annotation ViewModel (pencil strokes, colour picker, clear page/all)
  - [x] Mobile theme (touch-optimised: 48px tap targets, larger fonts)
- [x] `docs/ANDROID.md` — full build + deploy guide
- [x] 88 passing tests (14 new Phase 4 integration tests)
- ⚠️  **Android build requires:** `dotnet workload install android` — see `docs/ANDROID.md`

## Phase 5 — AI Extensions ✅

- [x] Chord detection (algorithmic — offline, no API key needed)
- [x] Fingering suggestions (algorithmic — offline, no API key needed)
- [x] Harmonisation suggestions (Claude AI — requires API key)
- [x] Score analysis: key, form, style, difficulty, challenges (Claude AI)
- [x] Practice recommendations: steps, focus areas, tempo plan (Claude AI)
- [x] `SymphoniaLegato.AIEngine` — new Core library with `ClaudeAIEngine`, `ChordDetector`, `FingeringAdvisor`
- [x] `IAIEngine` interface in Core.Interfaces
- [x] Desktop: AI Assistant panel with API key field, offline + AI buttons
- [x] Desktop: AI menu (AI → Detect Chords, Suggest Fingering, Harmonise, Analyse, Practice Plan)
- [x] Desktop: AI toolbar toggle button (✨)
- [x] 16 new Phase 5 tests — 104 total passing
