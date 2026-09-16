# Database

**Not applicable — there is no database.** Symphonia Legato persists data as
local files, not database rows:

- **Scores** are saved as `.enscore` files — a ZIP container with embedded
  MusicXML, JSON metadata, and annotations (`EnScoreFormat.cs` /
  `EnScoreRepository`, in `SymphoniaLegato.ImportExport`).
- **Per-score version history** uses a hidden git repository created next to
  the saved score (`ScoreVersionControl`, in `SymphoniaLegato.GitIntegration`)
  — see `module.md` in that project for detail. This is unrelated to this
  repository's own git history.
- **Cloud sync** is folder-based (any locally-synced folder, e.g. OneDrive/
  Dropbox) — `ScoreSyncService` compares file timestamps, newer wins. There is
  no sync server or account system.

If the project ever needs structured/queryable storage (e.g. a searchable
score library, a user account system), this file should be the place that
decision and its schema get documented. Until then, treat it as current and
accurate, not a placeholder waiting to be filled in.

**Note:** `SymphoniaLegato.Desktop.csproj` references `Microsoft.Data.Sqlite`
9.0.0, but nothing in the codebase actually uses it (no `SqliteConnection` or
related type appears anywhere in `src/` or `tests/`) — it's a dead dependency,
not a sign that SQLite is secretly in use. See `docs/DEPENDENCIES.md`.

See also: `docs/DATA_MODEL.md` for the in-memory domain model (which is real
and substantial, even though nothing here is persisted to a database).
