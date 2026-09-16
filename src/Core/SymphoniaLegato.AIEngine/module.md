# Module: SymphoniaLegato.AIEngine

**Path:** `src/Core/SymphoniaLegato.AIEngine/`
**Type:** Core library (engine layer)

## Purpose
AI assistance, split offline/online: `ChordDetector` and `FingeringAdvisor`
are algorithmic and work with no network access; `ClaudeAIEngine` (the
`IAIEngine` implementation) adds harmonisation, score analysis, and practice
recommendations via the Claude API when the user supplies an API key in the
Desktop app's AI Assistant panel. AI result types live in
`SymphoniaLegato.Core.Models.AIModels.cs`, not in this project, specifically
to avoid a circular dependency (`Core.Interfaces.IAIEngine` would otherwise
need to import from `AIEngine` — see `CLAUDE.md` pitfall #16).

## Depends on
`SymphoniaLegato.Core`.

## Depended on by
`Desktop`.

## Key files
| File | Purpose |
|------|---------|
| `ClaudeAIEngine.cs` | `IAIEngine` implementation (Claude API + offline) |
| `ChordDetector.cs` | Algorithmic chord detection |
| `FingeringAdvisor.cs` | Algorithmic fingering suggestions |
| (in `Core`) `Models/AIModels.cs` | AI result types (`ChordLabel`, `FingeringResult`, …) |

## Docs
`docs/operations_guide.html`, `docs/executive_overview.html` (this module).
Full architecture: root `CLAUDE.md` / `docs/ARCHITECTURE.md`.
