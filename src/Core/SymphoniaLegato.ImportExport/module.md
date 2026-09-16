# Module: SymphoniaLegato.ImportExport

**Path:** `src/Core/SymphoniaLegato.ImportExport/`
**Type:** Core library (engine layer)

## Purpose
File format bridges: MusicXML 4.0 import/export, MIDI import, the native
`.enscore` format (a ZIP container with embedded MusicXML + JSON metadata +
annotations), `ScoreSvgExporter` (a separate text-generation path, not shared
with the PNG/PDF pipeline), and `ScoreSyncService` (folder-based cloud sync,
newer-wins).

## Depends on
`SymphoniaLegato.Core`.

## Depended on by
`Desktop`, `Android`.

## Key files
| File | Purpose |
|------|---------|
| `EnScoreFormat.cs` | `.enscore` save/load |
| `ScoreSvgExporter.cs` | SVG export (from `LayoutResult`) |
| `ScoreSyncService.cs` | Cloud sync (folder-based, newer-wins) |

## Docs
`docs/operations_guide.html`, `docs/executive_overview.html` (this module).
Full architecture: root `CLAUDE.md` / `docs/ARCHITECTURE.md`.
