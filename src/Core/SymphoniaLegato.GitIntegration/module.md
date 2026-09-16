# Module: SymphoniaLegato.GitIntegration

**Path:** `src/Core/SymphoniaLegato.GitIntegration/`
**Type:** Core library (engine layer)

## Purpose
`ScoreVersionControl` — a LibGit2Sharp wrapper that gives each score its own
version history (commit/restore score versions), surfaced in the Desktop
app's Git History panel. Unrelated to the repo's own git history; this is
per-score version control for the end user's `.enscore` files.

## Depends on
`SymphoniaLegato.Core`.

## Depended on by
`Desktop`.

## Key files
| File | Purpose |
|------|---------|
| `ScoreVersionControl.cs` | LibGit2Sharp wrapper — commit/restore score versions |

## Docs
`docs/operations_guide.html`, `docs/executive_overview.html` (this module).
Full architecture: root `CLAUDE.md` / `docs/ARCHITECTURE.md`.
