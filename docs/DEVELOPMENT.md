# Development

## Getting set up

1. Install the .NET SDK (9.0+; `global.json` pins `9.0.0` with
   `rollForward: latestMajor`).
2. `dotnet build SymphoniaLegato.sln -c Debug` — should be 0 warnings, 0
   errors. If not, see `docs/TROUBLESHOOTING.md`.
3. `dotnet run --project src\Apps\SymphoniaLegato.Desktop --no-build` to
   launch the editor.
4. (Optional, Android) `dotnet workload install android`, then build
   `SymphoniaLegato.Android.sln` — see `docs/ANDROID.md`.

## Project layout

```
SymphoniaLegato.sln          Desktop + all tests (always buildable)
SymphoniaLegato.Android.sln  Android + shared Core (needs the android workload)
src/Core/                    10 domain/engine libraries — see each module.md
src/Apps/                    Desktop and Android — see each module.md
tests/                       xUnit test projects, one per Core library (mostly)
docs/                        This documentation suite
technical_memory/            Running log of *why* technical decisions were made
artifacts/                   Jupyter-notebook analysis/reports (generated output)
examples/                    Sample files (e.g. simple-piano.musicxml)
```

Full architecture and the "Known pitfalls" list (31 entries covering
Avalonia/DryWetMidi/QuestPDF gotchas) live in the root `CLAUDE.md` — read it
before making non-trivial changes; it front-loads a lot of hard-won context.

## Everyday commands

```powershell
dotnet build SymphoniaLegato.sln -c Debug
dotnet test SymphoniaLegato.sln --no-build
dotnet run --project src\Apps\SymphoniaLegato.Desktop --no-build
```

## Where to look for more

| Need | Doc |
|---|---|
| How to build/run/verify status | `docs/operations_guide.html` (root and per-module) |
| Debugging a specific problem | `docs/DEBUGGING.md` |
| A symptom you're hitting | `docs/TROUBLESHOOTING.md` |
| Coding conventions | `docs/STYLE_GUIDE.md` |
| Domain model | `docs/DATA_MODEL.md` |
| A specific module's purpose | `<module>/module.md` |
| What's not built yet | `docs/TODO.md` / `docs/KNOWN_ISSUES.md` |
| A past bug and its fix | `docs/BUGS.md` |
| How to contribute a change | `CONTRIBUTING.md` |
