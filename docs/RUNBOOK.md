# Runbook

Step-by-step procedures for recurring operational tasks. Unlike
`docs/OPERATIONS.md` (day-to-day build/run/verify), this file covers the
less-frequent "how do I actually do X" tasks.

## Cut a new version

1. Decide the bump size per the root `CLAUDE.md` versioning policy (content
   change → patch, new file(s) → minor, new folder(s)/module → major).
2. Update the `<Version>` in every `.csproj` and the version line in
   `README.md` to match.
3. Add a dated entry to `CHANGELOG.md` under `[Unreleased]` summarizing what
   changed.
4. Confirm `dotnet build SymphoniaLegato.sln` is 0 warnings/0 errors and
   `dotnet test SymphoniaLegato.sln` is all green.
5. Applying the actual git tag is the project owner's own step — not
   automated here (see root `CLAUDE.md`, "Git workflow").

## Publish a Windows build

```powershell
dotnet publish src\Apps\SymphoniaLegato.Desktop -c Release -r win-x64 `
  --self-contained -p:PublishSingleFile=true -o publish\windows
```
Output lands in `publish\windows\` (gitignored — see `.gitignore`).

## Publish an Android build

1. One-time: `dotnet workload install android` (~1 GB); JDK 21 exactly
   required (see `docs/ANDROID.md`).
2. `dotnet build SymphoniaLegato.Android.sln -c Debug` to confirm it compiles.
3. `dotnet publish SymphoniaLegato.Android.sln -c Release -r android-arm64 --self-contained -o publish\android`.

## Add a new Core module

1. Create `src/Core/SymphoniaLegato.<Name>/SymphoniaLegato.<Name>.csproj`
   (match the `<PropertyGroup>` shape of an existing Core `.csproj` — net9.0,
   nullable enabled, etc.).
2. Add it to `SymphoniaLegato.sln` (and `SymphoniaLegato.Android.sln` if
   Android needs it too).
3. Add a `module.md` at its root (see any existing module's for the format).
4. Add `docs/operations_guide.html` + `docs/executive_overview.html` under
   its own `docs/` folder — copy an existing module's as a starting template
   and edit the content; keep the shared embedded-CSS look.
5. Wire its DI registrations into `Program.cs` if the Desktop app consumes it.
6. Roll it into the root `CLAUDE.md`'s solution-structure list and dependency
   rule notes.

## Recover a score from its version history

Open the Git History panel in the Desktop app for the score in question —
`ScoreVersionControl` (in `SymphoniaLegato.GitIntegration`) opens the hidden
per-score git repo automatically once the score has been saved at least once.
There's no CLI path for this today; it's Desktop-app-only.

## Something looks broken and this runbook doesn't cover it

Check `docs/TROUBLESHOOTING.md` first, then `docs/BUGS.md`.
