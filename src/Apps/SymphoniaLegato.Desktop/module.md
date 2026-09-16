# Module: SymphoniaLegato.Desktop

**Path:** `src/Apps/SymphoniaLegato.Desktop/`
**Type:** Application (Avalonia MVVM desktop app — Windows/Linux/macOS)

## Purpose
The primary, full-featured editor. Every Core engine is wired in here:
notation editing, MIDI/audio playback with mixer, metronome, MusicXML/MIDI/
`.enscore`/PDF/PNG/SVG import-export, cloud sync, per-score git version
history, plugin manager, and the AI assistant panel. `Program.cs` (top-level
statements) registers every service into a `ServiceCollection` and builds the
`ServiceProvider`; `App.axaml.cs` receives it and constructs `MainWindow` with
its resolved dependencies.

## Depends on
All ten Core libraries: `Core`, `Rendering`, `NotationEngine`,
`PlaybackEngine`, `LayoutEngine`, `ImportExport`, `PdfEngine`, `PluginEngine`,
`GitIntegration`, `AIEngine`.

## Depended on by
Nothing — this is a leaf application.

## Key files
| File | Purpose |
|------|---------|
| `Program.cs` | DI registration & app entry point |
| `Views/MainWindow.axaml(.cs)` | Main window layout + export/open/save wiring |
| `ViewModels/MainWindowViewModel.cs` | Top-level VM: menus, export requests, theme |
| `Themes/SymphoniaTheme.axaml` / `LightTheme.axaml` / `HighContrastTheme.axaml` | Dark/Light/High-Contrast theme tokens — `AppTheme` enum picks between them |
| `Services/AppSettingsService.cs` | Persisted preferences (theme, MIDI device, sync folder, recent files) — `%AppData%\SymphoniaLegato\settings.json` |
| `Views/ShortcutsWindow.axaml(.cs)` | Help ▸ Keyboard Shortcuts dialog — kept as the single source of truth for shortcuts (not duplicated in `docs/USER_MANUAL.md`) |

## Docs
`docs/operations_guide.html`, `docs/executive_overview.html` (this module —
the ones a Desktop-app end user or IT admin would actually reach for). Full
architecture, all 31 "Known pitfalls," and the domain model reference live in
the project root's `CLAUDE.md` and `docs/ARCHITECTURE.md`.
