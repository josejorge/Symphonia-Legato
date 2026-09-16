// File: AppSettingsService.cs
// Description: Loads/saves AppSettings to a local JSON file in %AppData%\SymphoniaLegato — the first working settings persistence the app has had (see docs/TODO.md).
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-09-15
// Last edit date: 2026-09-15
// Version: 1.1.0

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace SymphoniaLegato.Desktop.Services;

/// <summary>
/// Loads <see cref="AppSettings"/> once at construction and keeps it in memory as
/// <see cref="Current"/>; callers mutate <c>Current</c> directly and call <see cref="Save"/>.
/// Deliberately does NOT persist the Claude API key (see AIAssistantViewModel) — that stays
/// session-only rather than sitting in plaintext on disk, a conscious security tradeoff
/// documented in docs/AUTHENTICATION.md. The SoundFont path also isn't persisted here: it
/// isn't wired to an actual synth yet (docs/TODO.md), so persisting it would imply a
/// working feature that doesn't exist.
/// </summary>
public sealed class AppSettingsService
{
    private readonly string _path;
    private readonly ILogger<AppSettingsService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() } // "Dark"/"Light"/"HighContrast", not 0/1/2
    };

    public AppSettings Current { get; }

    public AppSettingsService(ILogger<AppSettingsService> logger)
    {
        _logger = logger;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SymphoniaLegato");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
        Current = Load();
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings is not null) return settings;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load settings from {Path}; using defaults", _path);
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Current, JsonOptions);
            File.WriteAllText(_path, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save settings to {Path}", _path);
        }
    }
}
