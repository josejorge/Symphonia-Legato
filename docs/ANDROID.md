# Symphonia Legato — Android Build Guide

## Prerequisites

```powershell
# Install the Android workload (one-time)
dotnet workload install android

# Optionally install AOT for release builds
dotnet workload install android-aot
```

You also need:
- **Android SDK** — installed via Android Studio or the CLI tool
- **JDK 17+** — `JAVA_HOME` must point to it
- **A device or emulator** — API level 26+ (Android 8.0 Oreo)

## Build

```powershell
# Debug APK (for emulator / sideload)
dotnet build SymphoniaLegato.Android.sln -c Debug

# Release APK
dotnet publish SymphoniaLegato.Android.sln -c Release -r android-arm64 `
  --self-contained -o publish\android
```

## Run on a connected device

```powershell
# List connected devices
adb devices

# Install APK
adb install publish\android\com.josejorgehz.symphonialegato.apk

# Or run directly via dotnet
dotnet run --project src\Apps\SymphoniaLegato.Android -r android-arm64
```

## Project structure

```
src/Apps/SymphoniaLegato.Android/
  MainActivity.cs              # Avalonia Android entry point
  MainApplication.cs           # Application class
  App.axaml / App.axaml.cs    # DI wiring, service registration
  ViewLocator.cs               # ViewModel → View auto-mapping
  Themes/MobileTheme.axaml     # Touch-optimised dark palette

  ViewModels/
    AndroidMainViewModel.cs    # Root VM: tab navigation
    FileBrowserViewModel.cs    # Library: scan & open .enscore files
    ScoreViewerViewModel.cs    # Read-only score display
    MobilePlaybackViewModel.cs # Play/pause/stop + loop section
    MobileMetronomeViewModel.cs # Metronome with tap tempo
    AnnotationViewModel.cs     # Pencil annotation management

  Views/
    MainShellView.axaml        # Bottom-nav shell (Library/Score/Play/Metro)
    FileBrowserView.axaml      # Score library browser
    ScoreViewerView.axaml      # ScoreCanvas + page nav + zoom
    PlayerView.axaml           # Transport controls + loop bounds
    MetronomeView.axaml        # Metronome with beat display
```

## Architecture notes

- **Shared engines** — All Core projects (`SymphoniaLegato.Core`, `NotationEngine`,
  `LayoutEngine`, `ImportExport`, `PlaybackEngine`) are shared between Desktop and Android.
  They target `net9.0` and work cross-platform.

- **Score rendering** — The Android Score Viewer reuses `ScoreCanvas` from the Desktop
  project. The AXAML references `using:SymphoniaLegato.Desktop.Controls` — this is allowed
  because ScoreCanvas has no Android-specific dependencies.

- **Playback** — `MidiPlaybackEngine` uses DryWetMidi's `OutputDevice`, which is not
  available on Android. The `MobilePlaybackViewModel` models all state but defers audio to
  a future `AndroidMidiEngine` that will use Android's `MidiManager` API.

- **Annotations** — Stored in `Score.Annotations` (a `List<ScoreAnnotation>`) and serialised
  as part of the `.enscore` ZIP alongside `score.xml`. Each stroke carries normalised [0,1]
  coordinates relative to the page bounding box.

## Sync with Desktop

Use the **Cloud Sync** feature in the Desktop app (menu: Sync → Cloud Sync Settings) to
push/pull `.enscore` files through any folder that syncs with your phone:
- OneDrive
- Dropbox
- Nextcloud
- Google Drive (local client)

Point the Android app's Library scanner at the same local sync folder.
