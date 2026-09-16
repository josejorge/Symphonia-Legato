# Operations

The full operations & maintenance guide is `docs/operations_guide.html`
(system description, prerequisites, start/stop/verify, logs, configuration,
troubleshooting) — written as a standalone HTML document so it prints/exports
to PDF or Word cleanly. This file is a short Markdown index pointing at it,
kept because the standard docs suite expects `OPERATIONS.md` to exist by
name.

Every module also has its own scoped `docs/operations_guide.html` under
`src/Core/<Module>/docs/` or `src/Apps/<Module>/docs/` — see that module's
`module.md` for the link.

## Quick reference

```powershell
# Build + run
dotnet build SymphoniaLegato.sln -c Debug
dotnet run --project src\Apps\SymphoniaLegato.Desktop --no-build

# Test
dotnet test SymphoniaLegato.sln --no-build

# Publish
dotnet publish src\Apps\SymphoniaLegato.Desktop -c Release -r win-x64 `
  --self-contained -p:PublishSingleFile=true -o publish\windows
```

For anything beyond this quick reference — logs, configuration parameters,
troubleshooting — go to `docs/operations_guide.html`, not this file.
