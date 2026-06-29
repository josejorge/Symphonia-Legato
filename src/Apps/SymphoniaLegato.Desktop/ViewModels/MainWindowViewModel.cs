using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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
    private readonly AIAssistantViewModel _aiVm;

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
    [ObservableProperty] private bool _showAIAssistant;
    [ObservableProperty] private bool _isHighContrast;
    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private string _statusMessage = "Ready";

    public GitHistoryViewModel   GitHistory   => _gitHistoryVm;
    public MetronomeViewModel    Metronome    { get; }
    public AIAssistantViewModel  AIAssistant  => _aiVm;
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
        AIAssistantViewModel aiAssistantVm,
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
        _aiVm = aiAssistantVm;
        Metronome = metronomeVm;

        ScoreEditor   = scoreEditor;
        PlaybackVm    = playbackVm;
        MixerVm       = mixerVm;
        PianoKeyboard = pianoKeyboard;

        // Playback always rebuilds from the live score when Play is pressed.
        PlaybackVm.ScoreProvider = () => ScoreEditor?.Editor?.Score;

        // Drive the on-sheet playback cursor from engine position updates, and
        // hide it whenever playback returns to the Stopped state.
        _playback.PositionChanged += OnPlaybackPositionChanged;
        PlaybackVm.PropertyChanged += OnPlaybackVmPropertyChanged;

        // Clicking a note arms "play from here".
        ScoreEditor.PlaybackStartTickChanged += (_, tick) => PlaybackVm.StartTick = tick;

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

    // Zoom must drive the score editor's own Zoom (which recomputes layout);
    // the toolbar/menu used to change only this local Zoom value, so the canvas
    // never actually zoomed. We delegate to the editor and mirror its value
    // back here for the percentage label.
    [RelayCommand]
    private void ZoomIn()
    {
        if (ScoreEditor is null) return;
        ScoreEditor.ZoomInCommand.Execute(null);
        Zoom = ScoreEditor.Zoom;
    }

    [RelayCommand]
    private void ZoomOut()
    {
        if (ScoreEditor is null) return;
        ScoreEditor.ZoomOutCommand.Execute(null);
        Zoom = ScoreEditor.Zoom;
    }

    [RelayCommand]
    private void ZoomReset()
    {
        if (ScoreEditor is null) return;
        ScoreEditor.ZoomResetCommand.Execute(null);
        Zoom = ScoreEditor.Zoom;
    }

    [RelayCommand]
    private void ToggleMixer()         => ShowMixer         = !ShowMixer;
    [RelayCommand]
    private void TogglePianoKeyboard() => ShowPianoKeyboard = !ShowPianoKeyboard;
    [RelayCommand]
    private void ToggleGitHistory()  => ShowGitHistory  = !ShowGitHistory;
    [RelayCommand]
    private void ToggleMetronome()     => ShowMetronome     = !ShowMetronome;
    [RelayCommand]
    private void ToggleAIAssistant()   => ShowAIAssistant   = !ShowAIAssistant;
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

        // Populate the mixer with this score's staves (was never called, so the
        // mixer panel was always empty).
        MixerVm?.LoadScore(score);

        // Notify AI assistant with the first treble staff (if any)
        var firstStaff = score.Parts.SelectMany(p => p.Staves).FirstOrDefault();
        if (firstStaff is not null)
            _aiVm.LoadScore(score, firstStaff.Id);
    }

    private void OnScoreChanged(object? sender, EventArgs e)
    {
        IsDirty = ScoreEditor?.Editor?.IsDirty ?? false;
        Title = $"{ScoreEditor?.Editor?.Score.Title}{(IsDirty ? " •" : "")} — Symphonia Legato";
    }

    public Score? CurrentScore => ScoreEditor?.Editor?.Score;

    // ── Playback cursor wiring ────────────────────────────────────────

    private void OnPlaybackPositionChanged(object? sender, PlaybackPositionChangedEventArgs e)
    {
        var score = ScoreEditor?.Editor?.Score;
        if (score is null) return;
        double bpm = Math.Max(1, score.InitialTempo);
        double ticks = e.Position.TotalSeconds * bpm / 60.0 * 1024.0;
        Dispatcher.UIThread.Post(() =>
        {
            if (ScoreEditor is not null) ScoreEditor.PlaybackTick = ticks;
        });
    }

    private void OnPlaybackVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlaybackViewModel.State) &&
            PlaybackVm?.State == PlaybackState.Stopped)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (ScoreEditor is not null) ScoreEditor.PlaybackTick = -1;
            });
        }
    }

    // ── Demo score ────────────────────────────────────────────────────

    [RelayCommand]
    private void LoadDemoScore()
    {
        var score = Score.CreatePianoScore("Demo — Ode to Joy");
        LoadScore(score);
        var editor = ScoreEditor?.Editor;
        if (editor is null) return;

        editor.AddMeasures(0, 8);
        var treble = score.Parts[0].Staves[0];
        var bass   = score.Parts[0].Staves[1];

        // Beethoven's "Ode to Joy" theme (quarter notes), flowing 4 per 4/4 bar.
        int[] melody =
        {
            64, 64, 65, 67,  67, 65, 64, 62,  60, 60, 62, 64,  64, 62, 62, 62,
            64, 64, 65, 67,  67, 65, 64, 62,  60, 60, 62, 64,  62, 60, 60, 60
        };
        for (int i = 0; i < melody.Length; i++)
            AppendDemoNote(editor, treble, Clef.Treble, melody[i], 1 + i / 4);

        // Simple bass: one root whole note per bar (C, G alternating).
        int[] bassRoots = { 48, 43, 48, 43, 48, 43, 48, 43 };
        for (int bar = 0; bar < bassRoots.Length; bar++)
            AppendDemoNote(editor, bass, Clef.Bass, bassRoots[bar], bar + 1, whole: true);

        StatusMessage = "Demo score loaded — press Play ▶ to hear it and watch the cursor.";
    }

    private static void AppendDemoNote(ScoreEditor editor, Staff staff, Clef clef,
        int midi, int measureNumber, bool whole = false)
    {
        var pitch = Pitch.FromMidi(midi);
        var note = new Note
        {
            Pitch         = pitch,
            Duration      = whole ? Duration.Whole : Duration.Quarter,
            StaffPosition = StaffPositionCalculator.Calculate(pitch, clef)
        };
        editor.AddNote(staff.Id, measureNumber, note);
    }
}
