# Android Companion App

## Overview

The Symphonia Legato Android app is a companion viewer and playback tool for scores created on the desktop. It shares the Core, PlaybackEngine, and LayoutEngine libraries.

## Target SDK

- Min SDK: Android 10 (API 29)
- Target SDK: Android 15 (API 35)
- Framework: .NET 9 for Android (MAUI or Avalonia Android)

## Features (Phase 4)

### View Scores
- Open `.enscore` files from device storage or cloud sync
- Page-by-page rendering using the shared LayoutEngine
- Pinch-to-zoom (25%–400%)

### Playback
- MIDI playback using SoundFont
- Play, pause, stop, loop
- Tempo multiplier

### Practice Mode
- Metronome (visual + audio)
- Loop a selected section
- Slow-down mode

### Annotation Mode
- Pencil tool for handwriting annotations
- Highlight passages
- Annotations stored separately from the score XML

### Cloud Sync
- Connect to Nextcloud, Dropbox, or GitHub
- Download scores for offline use
- Upload annotations

## Architecture

The Android app wraps the same Core assemblies. The Avalonia renderer runs on Android using the Avalonia.Android package. Platform-specific code (MIDI, file access, cloud) is injected via DI.

## Building

```bash
dotnet workload install android
dotnet build src/Apps/SymphoniaLegato.Android -f net9.0-android
dotnet publish src/Apps/SymphoniaLegato.Android -f net9.0-android -c Release
```

## Signing

Set environment variables before release publish:
```
ANDROID_KEYSTORE_PATH=path/to/keystore.jks
ANDROID_KEY_ALIAS=your-alias
ANDROID_KEY_PASSWORD=...
```
