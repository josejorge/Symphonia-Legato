// File: AppSettings.cs
// Description: Persisted user preferences — theme, MIDI output device, cloud sync folder, and recent files. Not the same as Score data (see docs/DATABASE.md); this is app/session state only.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-09-15
// Last edit date: 2026-09-15
// Version: 1.0.0

namespace SymphoniaLegato.Desktop.Services;

/// <summary>Everything persisted across app restarts. Deliberately excludes the Claude
/// API key and SoundFont path — see AppSettingsService's remarks for why.</summary>
public sealed class AppSettings
{
    public bool IsHighContrast { get; set; }
    public string? MidiOutputDeviceName { get; set; }
    public string? SyncFolder { get; set; }
    public List<string> RecentFiles { get; set; } = [];
}
