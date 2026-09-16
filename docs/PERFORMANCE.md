# Performance

**No formal benchmarks exist yet.** This file documents where performance
matters and the current approach — not measured numbers, since none have
been captured. Treat any specific-sounding claim elsewhere as design intent,
not a verified benchmark, until this file says otherwise.

## Where it matters

| Area | Current approach | Risk as scores grow |
|---|---|---|
| Layout (`LayoutEngine.ComputeLayout`) | Recomputes the full `LayoutResult` tree on every score change (proportional spacing) | Recomputing the *entire* score on every single-note edit could get slow for long pieces — no incremental/partial re-layout exists today |
| Rendering (`ScoreCanvas.Render`) | Repaints from the current `LayoutResult` via Avalonia's `DrawingContext` on every invalidation | No virtualization — very long scores render every page's content even if off-screen |
| MIDI conversion (`ScoreToMidiConverter.Convert`) | Walks every measure/note in the score on every `Convert()` call, which happens at the start of every `Play` | Cheap relative to layout, but also has no caching — repeated Play/Stop cycles reconvert the whole score each time |
| PNG/PDF export | Renders every page at the requested DPI, synchronously | Large scores at high DPI could be slow/memory-heavy — not measured |

## If you're chasing a real performance problem

1. Confirm it's reproducible with the demo score (File ▸ Load Demo Score) vs.
   only with a much larger user score — that tells you whether it's a
   fixed cost or scales with score size.
2. Profile rather than guess — there's no existing profiling setup in this
   repo to reuse; standard `dotnet-trace`/Visual Studio profiler tooling
   applies.
3. Record what you find here, with real numbers, replacing the relevant row
   above — don't leave stale "no benchmarks" language once one exists.
