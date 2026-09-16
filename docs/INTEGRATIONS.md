# Integrations

Every external system Symphonia Legato talks to, and where that code lives.

| Integration | Module | What it does | Auth |
|---|---|---|---|
| **Claude API** (Anthropic) | `SymphoniaLegato.AIEngine` (`ClaudeAIEngine`) | Harmonisation, score analysis, practice recommendations | User-supplied API key, session-only — see `docs/AUTHENTICATION.md` |
| **Git** (via LibGit2Sharp) | `SymphoniaLegato.GitIntegration` (`ScoreVersionControl`) | Per-score commit/restore version history — a *local* repo per score, unrelated to this project's own repo | None (local, no remote) |
| **Cloud storage** (any provider) | `SymphoniaLegato.ImportExport` (`ScoreSyncService`) | Folder-based push/pull sync — works with whatever the user's OS-level cloud-drive client already syncs (OneDrive, Dropbox, etc.) | None — piggybacks on the OS-level client |
| **MIDI output devices** | `SymphoniaLegato.PlaybackEngine` (`MidiPlaybackEngine`, via DryWetMidi) | Audible playback through the system's MIDI subsystem | None |
| **Plugin assemblies** | `SymphoniaLegato.PluginEngine` (`PluginHost`) | Third-party instruments/exporters loaded from local assemblies implementing `IPlugin` — see `docs/PLUGIN_SDK.md` | None (sandboxed, local) |

## Not integrated (by design)

- No telemetry/analytics service.
- No crash-reporting service.
- No update-check/auto-update service.
- No webhooks — see `docs/WEBHOOKS.md`.

If a new integration is added, add a row here in the same pass — this table
is meant to stay a complete, current index, the same way `docs/DEPENDENCIES.md`
is for NuGet packages.
