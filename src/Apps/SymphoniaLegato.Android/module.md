# Module: SymphoniaLegato.Android

**Path:** `src/Apps/SymphoniaLegato.Android/`
**Type:** Application (Avalonia Android companion app)

## Purpose
A companion app, currently **viewer-focused** rather than full editing parity
with Desktop (see `docs/TODO.md` "Long-term"): score viewing, playback,
metronome, and annotations, sharing `Rendering`'s `ScoreCanvas` so it renders
identically to Desktop. `App.axaml.cs` does its own DI wiring (separate from
`Desktop`'s `Program.cs`); `Views/MainShellView.axaml` is the bottom-nav shell.
Built via the separate `SymphoniaLegato.Android.sln` (excluded from the main
`SymphoniaLegato.sln` so Desktop always builds cleanly without the Android
workload — see `CLAUDE.md` pitfall #13).

## Depends on
`Core`, `NotationEngine`, `PlaybackEngine`, `LayoutEngine`, `ImportExport`,
`Rendering`.

## Depended on by
Nothing — this is a leaf application.

## Key files
| File | Purpose |
|------|---------|
| `App.axaml.cs` | Android DI wiring & app entry |
| `Views/MainShellView.axaml` | Bottom-nav shell |

## Docs
`docs/operations_guide.html`, `docs/executive_overview.html` (this module).
Build/deploy details: root `docs/ANDROID.md`. Full architecture: root
`CLAUDE.md` / `docs/ARCHITECTURE.md`.
