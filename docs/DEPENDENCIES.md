# Dependencies

Every NuGet package referenced anywhere in the solution, as of 2026-09-15.

| Package | Version | Used by | Why |
|---|---|---|---|
| `Avalonia` + `Avalonia.Desktop`/`Avalonia.Android`/`Avalonia.Diagnostics`/`Avalonia.Fonts.Inter`/`Avalonia.Themes.Fluent` | 11.2.3 | Desktop, Android, Rendering | Cross-platform UI framework — the whole app's UI toolkit |
| `CommunityToolkit.Mvvm` | 8.3.2 | Desktop, Android | `[ObservableProperty]`/`[RelayCommand]` source generators for the MVVM ViewModels |
| `Melanchall.DryWetMidi` | 8.0.0 | PlaybackEngine, ImportExport | MIDI file read/write and playback — see `CLAUDE.md` pitfall #4 for its `Note`/`TimeSignature` name collision with the domain model |
| `QuestPDF` | 2024.10.4 | PdfEngine | PDF generation for score export |
| `LibGit2Sharp` | 0.30.0 | GitIntegration | Per-score version history (`ScoreVersionControl`) |
| `Microsoft.Extensions.DependencyInjection`(`.Abstractions`) | 9.0.0 | Desktop, Core libraries | DI container (`Program.cs`'s `ServiceCollection`) |
| `Microsoft.Extensions.Logging`(`.Abstractions`, `.Debug`) | 9.0.0 | Everywhere | `ILogger<T>` throughout; `AddDebug()` is the only sink configured (see `docs/operations_guide.html` — no persistent log file) |
| `System.Text.Json` | 9.0.0 | ImportExport | `.enscore` JSON metadata serialization |
| `System.IO.Compression` | 4.3.0 | ImportExport | `.enscore` ZIP container read/write |
| `Microsoft.Data.Sqlite` | 9.0.0 | Desktop *(referenced, unused)* | **Dead dependency** — see note below |
| `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector` | 2.9.2 / 2.8.2 / 17.11.1 / 6.0.2 | All test projects | Test framework/runner/coverage |
| `FluentAssertions` | 6.12.1 | All test projects | Assertion syntax (`.Should()`) |
| `Moq` | 4.20.72 | All test projects | Mocking (referenced solution-wide; not every test project currently uses a mock) |

## Known issue: dead dependency

`SymphoniaLegato.Desktop.csproj` references `Microsoft.Data.Sqlite`, but no
`SqliteConnection` or any related type appears anywhere in `src/` or `tests/`
— nothing in the app actually touches SQLite. Likely a leftover from an
earlier design direction that was never implemented, or never cleaned up.
Either wire up real usage or remove the reference; leaving it costs a bit of
restore time and NuGet surface for no benefit. Not fixed as part of the
2026-09-15 docs audit — flagged here rather than silently removed, since
removing a dependency is a code change, not a documentation one.

## Updating this file

Touch this file whenever a `PackageReference` is added, removed, or
meaningfully upgraded — the same trigger as `CHANGELOG.md` (see root
`CLAUDE.md`'s "Documentation upkeep").
