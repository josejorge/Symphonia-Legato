# Style Guide

This is the canonical, standalone version of the conventions also summarized
in the root `CLAUDE.md` — keep both in sync if either changes.

## Comments

- **XML doc comments** (`///`) are expected on all `public` members in the
  Core libraries (`GenerateDocumentationFile=true` for those projects).
- App code (Desktop/Android) has **no** doc comments by convention.
- **No inline comments** unless the *why* is non-obvious to a reader
  unfamiliar with the code — never restate what the code already says.

## Architecture rules

- **All score mutations go through `ScoreEditor.Execute(IScoreCommand)`** —
  never mutate a `Score` object directly. Every command implements
  `Execute` + `Undo`.
- **ViewModels must not import Avalonia UI types** — use events to ask the
  View to open dialogs, so ViewModels stay UI-framework-agnostic and testable.
- **Dependency rule**: Core ← Engine layers ← Desktop/Android. Core never
  imports from Apps. See each module's `module.md` for its exact position.

## Testing

- **xUnit + FluentAssertions.** Mocking via Moq where needed.
- **Naming**: `Method_StateUnderTest_Expected` (e.g.
  `SharpNote_AlreadyInKey_NoAccidental`).
- Prefer a headless unit test over reproducing through the UI when the bug is
  in an engine layer — see `docs/DEBUGGING.md`.

## Commit messages

**Conventional Commits**: `feat:`, `fix:`, `test:`, `docs:`, `refactor:`.

## Naming/formatting a fresh session should follow

- `sealed` on classes not designed for inheritance (the default posture in
  this codebase).
- File-scoped namespaces (`namespace X;`, not `namespace X { }`).
- `readonly record struct` for small immutable value types (`Pitch`,
  `Duration`).

## Branding headers

Every file in this project (as of the 2026-09-15 standards-compliance pass)
opens with a `File`/`Description`/`Author`/`Company`/`Date`/`Last edit date`/
`Version` header block — see the root `CLAUDE.md`'s "Compliance" section for
the two deliberate deviations from the global default (no `Company` value;
mixed old/new `Author` spelling). New files should follow the same format.
