# Module: SymphoniaLegato.PdfEngine

**Path:** `src/Core/SymphoniaLegato.PdfEngine/`
**Type:** Core library (engine layer)

## Purpose
PDF export via QuestPDF. Does **not** render notation itself — it embeds
pre-rendered PNG page images (produced by `Desktop`'s `ScorePngExporter`) into
a print-ready, multi-page PDF (`.Image(bytes).FitArea()`, QuestPDF 2024.x
API — see `CLAUDE.md` pitfall #10).

## Depends on
`SymphoniaLegato.Core`, `SymphoniaLegato.LayoutEngine`.

## Depended on by
`Desktop`.

## Key files
| File | Purpose |
|------|---------|
| `ScorePdfExporter.cs` | `GenerateFromImages()` — embeds PNG bytes into a PDF |

## Docs
`docs/operations_guide.html`, `docs/executive_overview.html` (this module).
Full architecture: root `CLAUDE.md` / `docs/ARCHITECTURE.md`.
