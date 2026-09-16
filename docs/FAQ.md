# FAQ

**Does Symphonia Legato need an internet connection?**
No, for editing, playback, and export. Only the Claude-powered AI features
(harmonisation, score analysis, practice recommendations) need network
access; offline chord detection and fingering suggestions work with no
connection.

**Is my Claude API key sent anywhere besides Anthropic?**
No. It's held in memory for the current session and used only to call the
Claude API directly. See `docs/AUTHENTICATION.md`.

**Why is there no sound when I press Play?**
Check the MIDI output device in MIDI/Audio Settings — the selection resets
every time the app restarts (nothing persists yet; see
`docs/CONFIGURATION.md`). See `docs/TROUBLESHOOTING.md` for more.

**Can I import a score from MuseScore, Finale, or Sibelius?**
Yes, via MusicXML (File ▸ Open, `.xml`/`.musicxml`) — every major notation
app can export to that format. Compressed MusicXML (`.mxl`) isn't supported
yet.

**Can other software open my Symphonia Legato scores?**
Export to MusicXML and it'll open in essentially any notation software. The
native `.enscore` format is specific to this app (it's a ZIP with embedded
MusicXML plus extra metadata/annotations), but the MusicXML inside it is
standard.

**Does it support Mac and Linux, or just Windows?**
Yes — it's built on Avalonia, a cross-platform UI framework, and targets
Windows, Linux, and macOS from the same codebase. Audible MIDI playback
depends on each OS having a reachable MIDI output device.

**Is the Android app a full editor?**
Not yet — it's viewer-focused today (view, play back, annotate). Full editing
parity with Desktop is a long-term goal, not yet built. See
`docs/KNOWN_ISSUES.md`.

**Where do my score files and their version history actually live?**
Scores save as `.enscore` files wherever you choose. Per-score version
history is a hidden git repository created next to the saved file the first
time you save — unrelated to this project's own source-code repository. See
`docs/DATABASE.md`.

**Is this affiliated with Encore, Finale, Sibelius, or MuseScore?**
No — it's an independent, open-source project inspired by Encore's classic
UI approach, with no code or organizational relationship to any of them.

**I found a bug — where do I report it?**
See `CONTRIBUTING.md`. Check `docs/BUGS.md` and `docs/KNOWN_ISSUES.md` first
in case it's already documented.
