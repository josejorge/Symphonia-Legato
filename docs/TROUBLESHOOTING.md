# Troubleshooting

User/build-facing symptoms and their fixes. For debugging *your own* code
changes, see `docs/DEBUGGING.md` instead — this file is about things going
wrong for someone running the app or building it, not about diagnosing new
bugs.

| Symptom | Cause / fix |
|---|---|
| Build fails restoring packages | Confirm the .NET SDK resolved is 9.x+ (`dotnet --version`) — `global.json` pins `9.0.0` with `rollForward: latestMajor`, so anything 9.0+ should work. |
| Android build fails with a workload error | Run `dotnet workload install android` (~1 GB), then build `SymphoniaLegato.Android.sln` specifically — the main `SymphoniaLegato.sln` deliberately excludes Android (`CLAUDE.md` pitfall #13). |
| Android build fails with `XAPRAS7023` | Known Avalonia 11.2.x + Android SDK 35 issue — see `docs/ANDROID.md` for the documented workaround. |
| Android build fails citing JDK version | JDK 21 exactly is required — JDK 25 is known-broken against the Android SDK 35 toolchain. See `docs/ANDROID.md`. |
| App crashes immediately at launch, no window | Historically an invalid Avalonia `InputGesture` string (fixed in commit `3d64da4` — see `docs/BUGS.md`). If it recurs, run under a debugger and look for an `ArgumentException` from XAML population. |
| No sound on Play | Check the MIDI output device in MIDI/Audio Settings — click Apply if you change it (selection now persists and actually takes effect, `docs/CONFIGURATION.md`). If no device is listed at all, no MIDI output is available on this machine. |
| Piano keyboard is silent before the first Play | Fixed — see `docs/BUGS.md` ("Virtual piano silent until first Play"). If it recurs, check `MidiPlaybackEngine.EnsureOutputDevice()` is called from both `PlayAsync` and `PreviewNoteAsync`. |
| AI Assistant panel errors on harmonisation/analysis | The Claude API key wasn't entered this session, or is invalid — it doesn't persist (`docs/CONFIGURATION.md`). Offline chord detection/fingering work with no key. |
| Notes overlap past a bar line | Should be fixed by the note-entry auto-flow (`docs/BUGS.md`) — if it recurs, check `ScoreEditorViewModel.ResolveTargetMeasure`. |
| Score canvas looks empty / dark-on-dark | The page is deliberately always white/near-black regardless of app theme — if it looks dark, something re-themed `ScoreCanvas` against the rule in `CLAUDE.md` pitfall #20. |
| A test project reports "No test is available" | That project has no test files yet, or none match the discovery pattern — check `dotnet test <project> --list-tests`. |

If a symptom isn't listed here, check `docs/BUGS.md` (the full tagged bug
log) and `docs/TODO.md` ("Known limitations / tech debt") before assuming
it's new.
