# Symphonia Legato — Android Build Guide

## Prerequisites

```powershell
# Install the Android workload (one-time, ~1 GB)
dotnet workload install android

# Install Android SDK dependencies (one-time, after workload install)
dotnet build src/Apps/SymphoniaLegato.Android/SymphoniaLegato.Android.csproj `
  -t:InstallAndroidDependencies -f net9.0-android `
  -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk" `
  -p:JavaSdkDirectory="C:\Program Files\Android\Android Studio\jbr" `
  -p:AcceptAndroidSDKLicenses=True
```

You also need:
- **Android SDK** — installed via [Android Studio](https://developer.android.com/studio)
  - Default location: `%LOCALAPPDATA%\Android\Sdk`
  - Required: API 35 platform (installed by `InstallAndroidDependencies` above)
- **JDK 21** (exactly) — the JDK bundled with Android Studio works:
  `C:\Program Files\Android\Android Studio\jbr`
  - JDK 25 does **not** work (blocked by Android SDK 35 toolchain)
  - JDK 17+ in theory but 21 is what the toolchain validates
- **A device or emulator** — API level 26+ (Android 8.0 Oreo)

### Known issue: XAPRAS7023 with Avalonia 11.2.x + Android SDK 35

`Avalonia.Android` 11.2.3 has a path-doubling bug with the .NET 10 Android
workload (SDK 35.x). The project csproj includes a workaround target
`FixAvaloniaAndroidDoubledPath` that pre-creates the expected intermediate
directory. This workaround should be removed when upgrading to Avalonia 11.3+.

## Build

```powershell
# Debug APK (for emulator / sideload)
dotnet build SymphoniaLegato.Android.sln -c Debug `
  -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk" `
  -p:JavaSdkDirectory="C:\Program Files\Android\Android Studio\jbr"

# Release APK
dotnet publish SymphoniaLegato.Android.sln -c Release -r android-arm64 `
  --self-contained -o publish\android `
  -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk" `
  -p:JavaSdkDirectory="C:\Program Files\Android\Android Studio\jbr"
```

To avoid passing `-p:` flags every time, create a `Directory.Build.props` override
in the Android project folder, or set `ANDROID_SDK_ROOT` and `JAVA_HOME` environment
variables pointing to the SDK and JDK 21 directories.

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

- **Score rendering** — `ScoreCanvas` lives in the shared `SymphoniaLegato.Rendering` library
  (not in Desktop). Both Desktop and Android reference it via `using:SymphoniaLegato.Rendering`.
  It was extracted from Desktop.Controls during Phase 4 to avoid a WinExe reference in Android.

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
