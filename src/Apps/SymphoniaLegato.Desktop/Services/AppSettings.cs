// File: AppSettings.cs
// Description: Persisted user preferences — theme, MIDI output device, cloud sync folder, and recent files. Not the same as Score data (see docs/DATABASE.md); this is app/session state only.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-09-15
// Last edit date: 2026-09-15
// Version: 1.1.0

namespace SymphoniaLegato.Desktop.Services;

/// <summary>The app's visual theme. <see cref="HighContrast"/> is WCAG 2.1 AA+ (black/yellow,
/// see HighContrastTheme.axaml) — a distinct accessibility palette, not just a Light/Dark pair.</summary>
public enum AppTheme { Dark, Light, HighContrast }

/// <summary>Everything persisted across app restarts. Deliberately excludes the Claude
/// API key and SoundFont path — see AppSettingsService's remarks for why.</summary>
public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public string? MidiOutputDeviceName { get; set; }
    public string? SyncFolder { get; set; }
    public List<string> RecentFiles { get; set; } = [];
}
