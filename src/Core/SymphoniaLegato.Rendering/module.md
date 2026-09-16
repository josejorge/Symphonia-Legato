# Module: SymphoniaLegato.Rendering

**Path:** `src/Core/SymphoniaLegato.Rendering/`
**Type:** Core library (shared UI-adjacent layer — the one Core library that
renders, so it sits above `NotationEngine`/`LayoutEngine` rather than beside
them)

## Purpose
`ScoreCanvas` — the shared score-rendering control extended from Avalonia's
`Control`, overriding `Render(DrawingContext ctx)` directly (no SkiaSharp
packages — Avalonia's `DrawingContext` is Skia-backed internally). Renders the
page as white paper / near-black ink, independent of the dark app chrome, by
design (see `CLAUDE.md` pitfall #20 / `docs/BUGS.md`). Shared between
`Desktop` and `Android` so both apps render identically.

## Depends on
`SymphoniaLegato.Core`, `SymphoniaLegato.NotationEngine`, `SymphoniaLegato.LayoutEngine`.

## Depended on by
`Desktop`, `Android`.

## Key files
| File | Purpose |
|------|---------|
| `ScoreCanvas.cs` | Score rendering (`DrawingContext`); must override `MeasureOverride` (pitfall #18) and call `InvalidateMeasure()` alongside `InvalidateVisual()` (pitfall #23) |

## Docs
`docs/operations_guide.html`, `docs/executive_overview.html` (this module).
Full architecture: root `CLAUDE.md` / `docs/ARCHITECTURE.md`.
