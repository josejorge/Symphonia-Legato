# Symphonia Legato

**Modern open-source music notation software for 2026.**

Created by **Jose Jorge Hernandez**

Symphonia Legato is a cross-platform, piano-first music notation editor inspired by the classic Encore application. It is built with C# (.NET 9), Avalonia UI, and a clean MVVM architecture — lightweight, fast, and fully git-friendly.

---

## Features

- **Grand Staff piano editor** — Treble + Bass clef linked as one unit
- **Full notation support** — Notes, chords, rests, ties, slurs, tuplets, dynamics, lyrics, text annotations
- **All standard clefs** — Treble, Bass, Alto, Tenor
- **All key & time signatures** — Including custom
- **MIDI playback** — Play, pause, stop, loop, tempo 25–400%
- **Virtual piano keyboard** — Mouse, touch, and MIDI input
- **Mixer** — Per-staff volume, pan, mute, solo
- **MusicXML import/export** — Industry-standard interchange
- **MIDI import/export**
- **.enscore format** — ZIP container with embedded MusicXML + JSON metadata
- **Plugin system** — Extensible instruments, exporters, notation symbols
- **Git integration** — Version control built in
- **PDF/PNG/SVG export**
- **Dark, light, and high-contrast themes**
- **Dockable panels and customisable workspace**

---

## Technology Stack

| Layer | Technology |
|---|---|
| Language | C# 13 (.NET 9) |
| UI Framework | Avalonia 11 |
| MVVM | CommunityToolkit.Mvvm |
| DI | Microsoft.Extensions.DependencyInjection |
| MIDI | Melanchall.DryWetMidi |
| Rendering | SkiaSharp |
| PDF | QuestPDF |
| Format | MusicXML 4.0 |
| Database | SQLite (settings/history) |
| Version Control | LibGit2Sharp |
| Testing | xUnit + FluentAssertions |

---

## Project Structure

```
SymphoniaLegato.sln
├── src/
│   ├── Core/
│   │   ├── SymphoniaLegato.Core           # Domain models & interfaces
│   │   ├── SymphoniaLegato.NotationEngine  # Score editing, commands
│   │   ├── SymphoniaLegato.PlaybackEngine  # MIDI playback
│   │   ├── SymphoniaLegato.LayoutEngine    # Score layout algorithm
│   │   ├── SymphoniaLegato.ImportExport    # MusicXML, MIDI, .enscore
│   │   ├── SymphoniaLegato.PdfEngine       # PDF/PNG/SVG export
│   │   ├── SymphoniaLegato.PluginEngine    # Plugin host & sandbox
│   │   └── SymphoniaLegato.GitIntegration  # Git version control
│   └── Apps/
│       ├── SymphoniaLegato.Desktop         # Avalonia cross-platform app
│       └── SymphoniaLegato.Android         # Android companion app
├── tests/
│   ├── SymphoniaLegato.Core.Tests
│   ├── SymphoniaLegato.NotationEngine.Tests
│   ├── SymphoniaLegato.PlaybackEngine.Tests
│   └── SymphoniaLegato.Integration.Tests
├── docs/
├── assets/
└── examples/
```

---

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A SoundFont file (`GeneralUser.sf2` or similar) in `assets/soundfonts/`

### Build & Run

```bash
git clone https://github.com/yourorg/symphonia-legato
cd symphonia-legato
dotnet restore
dotnet build
dotnet run --project src/Apps/SymphoniaLegato.Desktop
```

### Run Tests

```bash
dotnet test
```

---

## Documentation

| File | Contents |
|---|---|
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | System design & module responsibilities |
| [BUILD.md](docs/BUILD.md) | Full build & packaging instructions |
| [TESTING.md](docs/TESTING.md) | Testing strategy & running tests |
| [USER_MANUAL.md](docs/USER_MANUAL.md) | End-user guide |
| [PLUGIN_SDK.md](docs/PLUGIN_SDK.md) | Writing plugins |
| [API_REFERENCE.md](docs/API_REFERENCE.md) | Public API reference |
| [ROADMAP.md](docs/ROADMAP.md) | Development phases |
| [CONTRIBUTING.md](CONTRIBUTING.md) | How to contribute |
| [CHANGELOG.md](CHANGELOG.md) | Version history |

---

## File Format: .enscore

`.enscore` files are standard ZIP archives:

```
my-piece.enscore
├── score.xml       # MusicXML 4.0 (human-readable, git-diffable)
├── project.json    # Metadata: title, tempo, page size, etc.
├── audio/          # Audio clip references (optional)
└── assets/         # Embedded images (optional)
```

---

## Roadmap

- **Phase 1 (MVP):** Staff rendering, note entry, basic playback, save/load ✓
- **Phase 2:** Advanced notation — dynamics, lyrics, slurs, tuplets
- **Phase 3:** Professional — PDF export, plugins, Git history UI
- **Phase 4:** Android companion app — view, playback, annotation
- **Phase 5:** AI extensions — harmonisation, chord detection, fingering

---

## License

MIT — see [LICENSE.md](LICENSE.md)

---

## Author

**Jose Jorge Hernandez** — creator and lead developer of Symphonia Legato.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). PRs and issues welcome!
