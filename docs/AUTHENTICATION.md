# Authentication

**There is no user account or login system in Symphonia Legato.** It's a
local desktop application — anyone who can run the executable can use it
fully, with no sign-in step.

## Claude API key

The one credential in the app is a Claude API key, entered directly into the
AI Assistant sidebar to unlock the online AI features (harmonisation, score
analysis, practice recommendations). This is API authentication to
Anthropic's Claude API, not authentication *to* Symphonia Legato itself.

- Stored in memory only for the current session — see `docs/CONFIGURATION.md`.
- Never written to disk, logged, or transmitted anywhere except directly to
  the Claude API.
- The offline AI features (chord detection, fingering suggestions) require no
  key at all.

## Cloud sync

`ScoreSyncService` (folder-based sync) relies entirely on whatever
authentication the user's own cloud-drive client (OneDrive, Dropbox, etc.)
already has — Symphonia Legato never touches those credentials; it only reads
and writes files in a folder the OS already gives it access to.

## Per-score git history

`ScoreVersionControl` uses a local, hidden git repository per score — no
remote, no credentials, nothing to authenticate.
