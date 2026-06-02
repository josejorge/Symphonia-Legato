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

    [ObservableProperty] private string _title = "Symphonia Legato";
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private string? _currentFilePath;
    [ObservableProperty] private ScoreEditorViewModel? _scoreEditor;
    [ObservableProperty] private PlaybackViewModel? _playbackVm;
    [ObservableProperty] private MixerViewModel? _mixerVm;
    [ObservableProperty] private PianoKeyboardViewModel? _pianoKeyboard;
    [ObservableProperty] private bool _showMixer;
    [ObservableProperty] private bool _showPianoKeyboard = true;
    [ObservableProperty] private bool _showPianoRoll;
    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private string _statusMessage = "Ready";

    public ObservableCollection<string> RecentFiles { get; } = [];

    public MainWindowViewModel(
        IScoreRepository repository,
        IPlaybackEngine playback,
        ScoreEditorViewModel scoreEditor,
        PlaybackViewModel playbackVm,
        MixerViewModel mixerVm,
        PianoKeyboardViewModel pianoKeyboard,
        AboutViewModel aboutVm,
        ILogger<MainWindowViewModel> logger)
    {
        _repository = repository;
        _playback = playback;
        _logger = logger;
        _aboutVm = aboutVm;

        ScoreEditor   = scoreEditor;
        PlaybackVm    = playbackVm;
        MixerVm       = mixerVm;
        PianoKeyboard = pianoKeyboard;

        NewScore();
    }

    [RelayCommand]
    private void NewScore()
    {
        var score = Score.CreatePianoScore("Untitled");
        LoadScore(score);
        CurrentFilePath = null;
        StatusMessage = "New score created";
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        // File picker — injected via service in production; simplified here
        var score = await _repository.LoadAsync("placeholder.enscore");
        if (score is null) return;
        LoadScore(score);
        StatusMessage = $"Opened: {score.Title}";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (ScoreEditor?.Editor is null) return;
        string path = CurrentFilePath ?? "Untitled.enscore";
        await _repository.SaveAsync(ScoreEditor.Editor.Score, path);
        ScoreEditor.Editor.MarkSaved();
        IsDirty = false;
        StatusMessage = "Saved";
    }

    [RelayCommand]
    private void Undo() => ScoreEditor?.Editor?.Undo();

    [RelayCommand]
    private void Redo() => ScoreEditor?.Editor?.Redo();

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
    private void ShowAbout() => AboutRequested?.Invoke(this, _aboutVm);

    public event EventHandler<AboutViewModel>? AboutRequested;

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
}
