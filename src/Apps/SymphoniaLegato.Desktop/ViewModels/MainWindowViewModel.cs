using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.NotationEngine;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IScoreRepository _repository;
    private readonly IPlaybackEngine _playback;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly AboutViewModel _aboutVm;
    private readonly ScorePropertiesViewModel _scorePropertiesVm;
    private readonly GitHistoryViewModel _gitHistoryVm;
    private readonly PluginManagerViewModel _pluginManagerVm;
    private readonly MidiSettingsViewModel _midiSettingsVm;
    private readonly SyncSettingsViewModel _syncSettingsVm;

    [ObservableProperty] private string _title = "Symphonia Legato";
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private string? _currentFilePath;
    [ObservableProperty] private ScoreEditorViewModel? _scoreEditor;
    [ObservableProperty] private PlaybackViewModel? _playbackVm;
    [ObservableProperty] private MixerViewModel? _mixerVm;
    [ObservableProperty] private PianoKeyboardViewModel? _pianoKeyboard;
    [ObservableProperty] private bool _showMixer;
    [ObservableProperty] private bool _showPianoKeyboard = true;
    [ObservableProperty] private bool _showGitHistory;
    [ObservableProperty] private bool _showMetronome;
    [ObservableProperty] private bool _isHighContrast;
    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private string _statusMessage = "Ready";

    public GitHistoryViewModel  GitHistory  => _gitHistoryVm;
    public MetronomeViewModel   Metronome   { get; }
    public ObservableCollection<string> RecentFiles { get; } = [];

    public MainWindowViewModel(
        IScoreRepository repository,
        IPlaybackEngine playback,
        ScoreEditorViewModel scoreEditor,
        PlaybackViewModel playbackVm,
        MixerViewModel mixerVm,
        PianoKeyboardViewModel pianoKeyboard,
        AboutViewModel aboutVm,
        ScorePropertiesViewModel scorePropertiesVm,
        GitHistoryViewModel gitHistoryVm,
        PluginManagerViewModel pluginManagerVm,
        MidiSettingsViewModel midiSettingsVm,
        SyncSettingsViewModel syncSettingsVm,
        MetronomeViewModel metronomeVm,
        ILogger<MainWindowViewModel> logger)
    {
        _repository = repository;
        _playback = playback;
        _logger = logger;
        _aboutVm = aboutVm;
        _scorePropertiesVm = scorePropertiesVm;
        _gitHistoryVm = gitHistoryVm;
        _pluginManagerVm = pluginManagerVm;
        _midiSettingsVm = midiSettingsVm;
        _syncSettingsVm = syncSettingsVm;
        Metronome = metronomeVm;

        ScoreEditor   = scoreEditor;
        PlaybackVm    = playbackVm;
        MixerVm       = mixerVm;
        PianoKeyboard = pianoKeyboard;

        NewScore();
    }

    // ── File ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void NewScore()
    {
        var score = Score.CreatePianoScore("Untitled");
        LoadScore(score);
        // Add 4 empty measures so the canvas has content to render
        ScoreEditor?.Editor?.AddMeasures(0, 4);
        CurrentFilePath = null;
        StatusMessage = "New score created";
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        OpenFileRequested?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (ScoreEditor?.Editor is null) return;

        if (CurrentFilePath is null)
        {
            await SaveAsAsync();
            return;
        }

        await _repository.SaveAsync(ScoreEditor.Editor.Score, CurrentFilePath);
        ScoreEditor.Editor.MarkSaved();
        IsDirty = false;
        StatusMessage = $"Saved: {Path.GetFileName(CurrentFilePath)}";
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        SaveAsRequested?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    // Called by MainWindow after the user picks a file path
    public async Task SaveToPathAsync(string path)
    {
        if (ScoreEditor?.Editor is null) return;
        await _repository.SaveAsync(ScoreEditor.Editor.Score, path);
        ScoreEditor.Editor.MarkSaved();
        CurrentFilePath = path;
        IsDirty = false;
        StatusMessage = $"Saved: {Path.GetFileName(path)}";
    }

    public async Task OpenFromPathAsync(string path)
    {
        var score = await _repository.LoadAsync(path);
        if (score is null) { StatusMessage = "Could not open file"; return; }
        LoadScore(score);
        CurrentFilePath = path;
        StatusMessage = $"Opened: {score.Title}";
        if (RecentFiles.Contains(path)) RecentFiles.Remove(path);
        RecentFiles.Insert(0, path);
    }

    // ── Edit ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void Undo() => ScoreEditor?.Editor?.Undo();

    [RelayCommand]
    private void Redo() => ScoreEditor?.Editor?.Redo();

    // ── View ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void ZoomIn()  => Zoom = Math.Min(8.0, Zoom * 1.25);

    [RelayCommand]
    private void ZoomOut() => Zoom = Math.Max(0.25, Zoom / 1.25);

    [RelayCommand]
    private void ZoomReset() => Zoom = 1.0;

    [RelayCommand]
    private void ToggleMixer()         => ShowMixer         = !ShowMixer;
    [RelayCommand]
    private void TogglePianoKeyboard() => ShowPianoKeyboard = !ShowPianoKeyboard;
    [RelayCommand]
    private void ToggleGitHistory()  => ShowGitHistory  = !ShowGitHistory;
    [RelayCommand]
    private void ToggleMetronome()   => ShowMetronome   = !ShowMetronome;
    [RelayCommand]
    private void ToggleHighContrast()
    {
        IsHighContrast = !IsHighContrast;
        ThemeChangeRequested?.Invoke(this, IsHighContrast);
    }

    // ── Score Properties ──────────────────────────────────────────────

    [RelayCommand]
    private void ShowScoreProperties()
    {
        if (ScoreEditor?.Editor is null) return;
        _scorePropertiesVm.LoadFrom(ScoreEditor.Editor.Score);
        ScorePropertiesRequested?.Invoke(this, _scorePropertiesVm);
    }

    // ── Export ────────────────────────────────────────────────────────

    [RelayCommand]
    private void ExportMusicXml() => ExportRequested?.Invoke(this, "musicxml");

    [RelayCommand]
    private void ExportMidi() => ExportRequested?.Invoke(this, "midi");

    [RelayCommand]
    private void ExportPdf() => ExportRequested?.Invoke(this, "pdf");

    [RelayCommand]
    private void ExportPng() => ExportRequested?.Invoke(this, "png");

    [RelayCommand]
    private void ExportSvg() => ExportRequested?.Invoke(this, "svg");

    // ── Dialogs ───────────────────────────────────────────────────────

    [RelayCommand]
    private void Exit() => ExitRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ShowAbout() => AboutRequested?.Invoke(this, _aboutVm);

    [RelayCommand]
    private void ShowPluginManager() => PluginManagerRequested?.Invoke(this, _pluginManagerVm);

    [RelayCommand]
    private void ShowMidiSettings() => MidiSettingsRequested?.Invoke(this, _midiSettingsVm);

    [RelayCommand]
    private void ShowSyncSettings() => SyncSettingsRequested?.Invoke(this, _syncSettingsVm);

    // ── Events (View opens the actual windows) ────────────────────────

    public event EventHandler? ExitRequested;
    public event EventHandler<AboutViewModel>? AboutRequested;
    public event EventHandler<ScorePropertiesViewModel>? ScorePropertiesRequested;
    public event EventHandler<PluginManagerViewModel>? PluginManagerRequested;
    public event EventHandler<MidiSettingsViewModel>? MidiSettingsRequested;
    public event EventHandler<SyncSettingsViewModel>? SyncSettingsRequested;
    public event EventHandler<string>? ExportRequested;   // payload: "pdf", "png", "svg", "musicxml", "midi"
    public event EventHandler? OpenFileRequested;
    public event EventHandler? SaveAsRequested;
    public event EventHandler<bool>? ThemeChangeRequested; // payload: isHighContrast

    // ── Helpers ───────────────────────────────────────────────────────

    private void LoadScore(Score score)
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ScoreEditor>.Instance;
        var editor = new ScoreEditor(score, logger);
        editor.ScoreChanged += OnScoreChanged;
        ScoreEditor?.Initialize(editor);
        Title = $"{score.Title} — Symphonia Legato";
        IsDirty = false;
    }

    private void OnScoreChanged(object? sender, EventArgs e)
    {
        IsDirty = ScoreEditor?.Editor?.IsDirty ?? false;
        Title = $"{ScoreEditor?.Editor?.Score.Title}{(IsDirty ? " •" : "")} — Symphonia Legato";
    }

    public Score? CurrentScore => ScoreEditor?.Editor?.Score;
}
