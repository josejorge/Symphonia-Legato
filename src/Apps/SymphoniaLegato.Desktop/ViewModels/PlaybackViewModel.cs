using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class PlaybackViewModel : ViewModelBase
{
    private readonly IPlaybackEngine _engine;

    [ObservableProperty] private PlaybackState _state = PlaybackState.Stopped;
    [ObservableProperty] private TimeSpan _position;
    [ObservableProperty] private TimeSpan _duration;
    [ObservableProperty] private double _tempoMultiplier = 1.0;
    [ObservableProperty] private bool _isLooping;
    [ObservableProperty] private int _currentMeasure = 1;
    [ObservableProperty] private int _currentBeat = 1;
    [ObservableProperty] private bool _metronomeEnabled;
    [ObservableProperty] private bool _countInEnabled;
    [ObservableProperty] private int _tempo = 120;

    public bool IsPlaying => State == PlaybackState.Playing;
    public bool IsStopped => State == PlaybackState.Stopped;

    /// <summary>
    /// Supplies the score to (re)load into the engine when playback starts.
    /// Set by <c>MainWindowViewModel</c> so that pressing Play always renders
    /// the *current* edited score (notes added since the last play included).
    /// </summary>
    public Func<Score?>? ScoreProvider { get; set; }

    /// <summary>Absolute domain tick (1 quarter = 1024) playback starts from; 0 = from the top.</summary>
    public double StartTick { get; set; }

    public PlaybackViewModel(IPlaybackEngine engine)
    {
        _engine = engine;
        _engine.PositionChanged += OnPositionChanged;
        _engine.PlaybackEnded   += OnPlaybackEnded;
    }

    [RelayCommand]
    private async Task PlayPauseAsync()
    {
        if (State == PlaybackState.Playing)
        {
            await _engine.PauseAsync();
        }
        else
        {
            // Starting fresh (not resuming a pause): rebuild the MIDI from the
            // current score so freshly entered notes are heard. Without this the
            // engine's MIDI file is never populated and Play is silent.
            if (State != PlaybackState.Paused && ScoreProvider?.Invoke() is { } score)
            {
                await _engine.LoadScoreAsync(score);
                // "Play from here": start at the armed note's tick.
                if (StartTick > 0)
                {
                    double bpm = Math.Max(1, score.InitialTempo);
                    double seconds = StartTick / 1024.0 * 60.0 / bpm;
                    await _engine.SeekAsync(TimeSpan.FromSeconds(seconds));
                }
            }
            await _engine.PlayAsync();
        }
        State = _engine.State;
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(IsStopped));
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        StartTick = 0;
        await _engine.StopAsync();
        State = PlaybackState.Stopped;
        Position = TimeSpan.Zero;
        CurrentMeasure = 1;
        CurrentBeat = 1;
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(IsStopped));
    }

    [RelayCommand]
    private async Task RewindAsync()
    {
        StartTick = 0;
        await _engine.SeekAsync(TimeSpan.Zero);
        Position = TimeSpan.Zero;
    }

    partial void OnTempoMultiplierChanged(double value) =>
        _engine.TempoMultiplier = value;

    partial void OnIsLoopingChanged(bool value) =>
        _engine.IsLooping = value;

    partial void OnMetronomeEnabledChanged(bool value) =>
        _engine.MetronomeEnabled = value;

    partial void OnCountInEnabledChanged(bool value) =>
        _engine.CountInEnabled = value;

    public async Task LoadScoreAsync(Score score) =>
        await _engine.LoadScoreAsync(score);

    private void OnPositionChanged(object? sender, PlaybackPositionChangedEventArgs e)
    {
        // Fired from the engine's position timer (background thread) — marshal.
        Dispatcher.UIThread.Post(() =>
        {
            Position       = e.Position;
            CurrentMeasure = e.CurrentMeasure;
            CurrentBeat    = e.CurrentBeat;
            Duration       = _engine.Duration;
        });
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            State = PlaybackState.Stopped;
            Position = TimeSpan.Zero;
            OnPropertyChanged(nameof(IsPlaying));
            OnPropertyChanged(nameof(IsStopped));
        });
    }
}
