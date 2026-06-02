# Deployment Guide

## Windows

### MSIX Package (Recommended)
```powershell
dotnet publish src/Apps/SymphoniaLegato.Desktop `
  -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true `
  -o publish/windows

# Then package with MSIX tools (Visual Studio / makeappx)
```

### Portable ZIP
```powershell
dotnet publish src/Apps/SymphoniaLegato.Desktop `
  -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=false `
  -o publish/windows-portable
Compress-Archive publish/windows-portable publish/SymphoniaLegato-win-x64.zip
```

## Linux

### AppImage
```bash
dotnet publish src/Apps/SymphoniaLegato.Desktop \
  -c Release -r linux-x64 --self-contained \
  -o publish/linux

# Use appimagetool to create AppImage
```

### .deb Package
```bash
# Use dotnet-deb tool
dotnet deb src/Apps/SymphoniaLegato.Desktop -c Release
```

### Flatpak
A Flatpak manifest is in `tools/flatpak/`. Requires the .NET 9 SDK runtime extension.

## macOS

### .app Bundle
```bash
dotnet publish src/Apps/SymphoniaLegato.Desktop \
  -c Release -r osx-arm64 --self-contained \
  -o publish/macos

# Use Avalonia's macOS packaging tools to create .app bundle
```

### .dmg
Wrap the .app bundle with `create-dmg` or `hdiutil`.

## CI/CD

GitHub Actions workflows are in `.github/workflows/`:
- `build.yml` — builds and tests on push/PR (Windows, Linux, macOS)
- `release.yml` — builds release packages and uploads to GitHub Releases on tag push

## Version Numbering

`v{Major}.{Minor}.{Patch}` — Semantic Versioning.
Set via `<Version>` in `Directory.Build.props`.
