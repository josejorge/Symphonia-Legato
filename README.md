# Symphonia Legato

**Modern open-source music notation software for 2026.**

Created by **Jose Jorge Hernandez**

Symphonia Legato is a cross-platform, piano-first music notation editor inspired by the classic Encore application. Built with C# (.NET 9), Avalonia UI, and a clean MVVM architecture — lightweight, fast, and fully git-friendly.

---

## Features

- **Grand Staff piano editor** — Treble + Bass clef linked as one unit
- **Full notation support** — Notes, chords, rests, ties, slurs, tuplets, dynamics, lyrics, tempo markings
- **All standard clefs** — Treble, Bass, Alto, Tenor
- **All key & time signatures** — C major through all sharps/flats, custom
- **MIDI playback** — Play, pause, stop, loop, 25–400% tempo
- **Virtual piano keyboard** — Mouse and touch input
- **Mixer** — Per-staff volume, pan, mute, solo
- **Metronome** — Configurable BPM, subdivision, tap tempo
- **MusicXML 4.0 import/export** — Industry-standard interchange format
- **MIDI import/export**
- **.enscore format** — ZIP container with embedded MusicXML + JSON metadata + annotations
- **PDF export** — Multi-page, print-ready (QuestPDF)
- **PNG export** — Full-resolution per-page images
- **SVG export** — Vector graphics from layout engine
- **Score Properties dialog** — Title, composer, tempo, time signature, page size
- **Plugin system** — Extensible instruments and exporters
- **Git history panel** — Version control built in, commit/restore score versions
- **Cloud sync** — Push/pull .enscore files via any cloud-synced local folder
- **Dark and High Contrast themes** — WCAG 2.1 AA+ high contrast
- **Accessibility** — AutomationProperties on all interactive controls
- **Android companion app** — Score viewer, playback, metronome, annotations
- **AI Assistant** — Chord detection, fingering suggestions (offline); harmonisation, score analysis, practice recommendations (Claude API, API key required)

---

## Technology Stack

| Layer | Technology |
|---|---|
| Language | C# 13 (.NET 9) |
| UI Framework | Avalonia 11 |
| MVVM | CommunityToolkit.Mvvm |
| DI | Microsoft.Extensions.DependencyInjection |
| MIDI | Melanchall.DryWetMidi |
| Rendering | Avalonia DrawingContext (Skia-backed, no SkiaSharp package required) |
| PDF | QuestPDF 2024 |
| Format | MusicXML 4.0 |
| Version Control | LibGit2Sharp |
| Testing | xUnit + FluentAssertions (104 tests) |

---

## Project Structure

```
SymphoniaLegato.sln            ← Desktop + all tests
SymphoniaLegato.Android.sln   ← Android + shared Core (needs android workload)
├── src/
│   ├── Core/
│   │   ├── SymphoniaLegato.Core           # Domain models & interfaces
│   │   ├── SymphoniaLegato.NotationEngine  # Score editing, commands, undo/redo
│   │   ├── SymphoniaLegato.PlaybackEngine  # MIDI playback + MetronomeEngine
│   │   ├── SymphoniaLegato.LayoutEngine    # Score layout algorithm
│   │   ├── SymphoniaLegato.ImportExport    # MusicXML, MIDI, .enscore, SVG, cloud sync
│   │   ├── SymphoniaLegato.Rendering       # ScoreCanvas (shared between Desktop + Android)
│   │   ├── SymphoniaLegato.PdfEngine       # PDF export
│   │   ├── SymphoniaLegato.PluginEngine    # Plugin host & sandbox
│   │   ├── SymphoniaLegato.GitIntegration  # Git version control
│   │   └── SymphoniaLegato.AIEngine        # Chord detection, fingering, Claude AI integration
│   └── Apps/
│       ├── SymphoniaLegato.Desktop         # Avalonia desktop app (Windows/Linux/macOS)
│       └── SymphoniaLegato.Android         # Avalonia Android companion app
├── tests/
│   ├── SymphoniaLegato.Core.Tests
│   ├── SymphoniaLegato.NotationEngine.Tests
│   ├── SymphoniaLegato.PlaybackEngine.Tests
│   └── SymphoniaLegato.Integration.Tests
├── docs/
│   ├── ROADMAP.md                          # Phase-by-phase feature plan
│   └── ANDROID.md                          # Android build & deploy guide
├── examples/
│   └── simple-piano.musicxml
└── publish/
    ├── windows/                            # Self-contained EXE
    └── android/                            # APK (after android publish)
```

---

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows 10/11, Linux, or macOS

### Build & Run (Desktop)

```powershell
git clone https://github.com/josejorgehz/symphonia-legato
cd symphonia-legato
dotnet build SymphoniaLegato.sln
dotnet run --project src/Apps/SymphoniaLegato.Desktop
```

### Publish Self-Contained EXE (Windows)

```powershell
dotnet publish src/Apps/SymphoniaLegato.Desktop -c Release -r win-x64 `
  --self-contained -p:PublishSingleFile=true -o publish/windows
```

### Build Android APK

```powershell
# First-time setup
dotnet workload install android

# Build debug APK
dotnet build SymphoniaLegato.Android.sln -c Debug

# Publish release APK
dotnet publish SymphoniaLegato.Android.sln -c Release -r android-arm64 `
  --self-contained -o publish/android
```

See [docs/ANDROID.md](docs/ANDROID.md) for full Android setup and deployment guide.

### Run Tests

```powershell
dotnet test SymphoniaLegato.sln   # 104 tests, all passing
```

---

## File Format: .enscore

`.enscore` files are standard ZIP archives — fully readable by any ZIP tool:

```
my-piece.enscore
├── score.xml       # MusicXML 4.0 (human-readable, git-diffable)
├── project.json    # Metadata: title, tempo, page size, etc.
├── audio/          # Audio clip references (optional)
└── assets/         # Embedded images (optional)
```

---

## Roadmap

| Phase | Status | Summary |
|---|---|---|
| 1 — MVP | ✅ | Staff rendering, note entry, basic playback, save/load |
| 2 — Advanced notation | ✅ | Dynamics, lyrics, slurs, articulations, hand coloring |
| 3 — Professional | ✅ | PDF/PNG/SVG, score properties, plugins, git history, high contrast |
| 4 — Android | ✅ | Metronome, cloud sync, annotations, Android companion app |
| 5 — AI | ✅ | Chord detection, fingering (offline) + harmonisation, analysis, practice plan (Claude API) |

---

## License

MIT — see [LICENSE.md](LICENSE.md)

---

## Author

**Jose Jorge Hernandez** — creator and lead developer of Symphonia Legato.

---

## Contributing

PRs and issues welcome. Please read [CLAUDE.md](CLAUDE.md) for architecture conventions and known pitfalls before contributing.
