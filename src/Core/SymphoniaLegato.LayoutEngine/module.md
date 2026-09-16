# Module: SymphoniaLegato.LayoutEngine

**Path:** `src/Core/SymphoniaLegato.LayoutEngine/`
**Type:** Core library (engine layer)

## Purpose
Proportional layout computation: `LayoutEngine.ComputeLayout(Score, LayoutOptions)`
returns a `LayoutResult` tree (`RenderedPage → RenderedSystem → RenderedStaff →
RenderedMeasure → RenderedNoteElement`). `ScoreCanvas` (in `Rendering`) reads
this tree directly to paint — there's no intermediate format. Also
defensively scales note positions by `max(timeSignatureCapacity,
actualContentSpan)` so an over-full measure compresses instead of spilling
past the bar line (see `CLAUDE.md` pitfall #25 / `docs/BUGS.md`).

## Depends on
`SymphoniaLegato.Core`.

## Depended on by
`PdfEngine`, `Rendering`, `Desktop`, `Android`.

## Key files
| File | Purpose |
|------|---------|
| `LayoutEngine.cs` | Full layout computation |
| (in `Core`) `Interfaces/ILayoutEngine.cs` | Layout result types |

## Docs
`docs/operations_guide.html`, `docs/executive_overview.html` (this module).
Full architecture: root `CLAUDE.md` / `docs/ARCHITECTURE.md`.
