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
    [ObservableProperty] private int _tempo = 120;

    public bool IsPlaying => State == PlaybackState.Playing;
    public bool IsStopped => State == PlaybackState.Stopped;

    /// <summary>
    /// Supplies the score to (re)load into the engine when playback starts.
    /// Set by <c>MainWindowViewModel</c> so that pressing Play always renders
    /// the *current* edited score (notes added since the last play included).
    /// </summary>
    public Func<Score?>? ScoreProvider { get; set; }

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
                await _engine.LoadScoreAsync(score);
            await _engine.PlayAsync();
        }
        State = _engine.State;
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(IsStopped));
    }

    [RelayCommand]
    private async Task StopAsync()
    {
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
        await _engine.SeekAsync(TimeSpan.Zero);
        Position = TimeSpan.Zero;
    }

    partial void OnTempoMultiplierChanged(double value) =>
        _engine.TempoMultiplier = value;

    partial void OnIsLoopingChanged(bool value) =>
        _engine.IsLooping = value;

    public async Task LoadScoreAsync(Score score) =>
        await _engine.LoadScoreAsync(score);

    private void OnPositionChanged(object? sender, PlaybackPositionChangedEventArgs e)
    {
        Position       = e.Position;
        CurrentMeasure = e.CurrentMeasure;
        CurrentBeat    = e.CurrentBeat;
        Duration       = _engine.Duration;
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        State = PlaybackState.Stopped;
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(IsStopped));
    }
}
