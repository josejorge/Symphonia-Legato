# Configuration

Most settings persist across restarts via `AppSettingsService`, a JSON file
at `%AppData%\SymphoniaLegato\settings.json` (added 2026-09-15 — see
`docs/TODO.md`). Two things are deliberately excluded from it:

| Parameter | Where it's set | Persisted? |
|---|---|---|
| Claude API key | AI Assistant sidebar (typed directly) | **No — deliberately.** Kept session-only rather than written to disk in plaintext; see `docs/AUTHENTICATION.md`. |
| MIDI output device | MIDI/Audio Settings dialog | Yes. Selecting a device and clicking Apply also makes it take effect immediately — it didn't before 2026-09-15 (see `docs/BUGS.md`). |
| SoundFont path | MIDI/Audio Settings dialog | **No — deliberately.** Not wired to an actual synth yet (`docs/TODO.md`), so persisting it would imply a working feature that doesn't exist. |
| Cloud sync folder | Sync Settings dialog | Yes. |
| Theme (Dark / High Contrast) | View menu | Yes. |
| Recent files (last 10) | File ▸ Recent Files | Yes. Stale (deleted/moved) entries are pruned on load. |

## Build-time configuration

| File | Controls |
|---|---|
| `global.json` | Pinned .NET SDK version (`9.0.0`, `rollForward: latestMajor`) |
| `Directory.Build.props` / `Directory.Build.targets` | Shared MSBuild settings across every project in the solution |
| Each project's `.csproj` | `<GenerateDocumentationFile>` (`true` for Core libraries, `false` for Desktop/Android — CS1591 suppressed globally so missing doc comments never block the build) |

## Secrets

No secrets are stored in any tracked file. The Claude API key lives only in
memory for the session it's typed in. See the root `CLAUDE.md`'s
"Conventions" section for the project's secrets policy.
