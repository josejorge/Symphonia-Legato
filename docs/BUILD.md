# Build Guide

## Prerequisites

| Tool | Minimum Version |
|---|---|
| .NET SDK | 9.0 |
| Git | 2.40 |
| (Optional) JetBrains Rider | 2025.1+ |
| (Optional) Visual Studio | 2022 17.8+ |

---

## Clone and Restore

```bash
git clone https://github.com/yourorg/symphonia-legato
cd symphonia-legato
dotnet restore
```

---

## Development Build

```bash
dotnet build
dotnet run --project src/Apps/SymphoniaLegato.Desktop
```

---

## Run Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/SymphoniaLegato.Core.Tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Coverage report (requires reportgenerator)
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage/report"
```

---

## Release Builds

### Windows (self-contained)
```bash
dotnet publish src/Apps/SymphoniaLegato.Desktop \
  -c Release -r win-x64 --self-contained \
  -o publish/windows
```

### Linux
```bash
dotnet publish src/Apps/SymphoniaLegato.Desktop \
  -c Release -r linux-x64 --self-contained \
  -o publish/linux
```

### macOS
```bash
dotnet publish src/Apps/SymphoniaLegato.Desktop \
  -c Release -r osx-arm64 --self-contained \
  -o publish/macos
```

---

## SoundFont Setup

Download a General MIDI SoundFont (e.g. `GeneralUser GS.sf2`) and place it at:

```
assets/soundfonts/GeneralUser.sf2
```

---

## Android Build (Phase 4)

Requires .NET MAUI or Android SDK configured. See [ANDROID_APP.md](ANDROID_APP.md).

---

## Packaging

Use the scripts in `tools/` to create installers:

- `tools/package-windows.ps1` — MSIX or Inno Setup installer
- `tools/package-linux.sh` — AppImage or .deb
- `tools/package-macos.sh` — .dmg with code-signing
