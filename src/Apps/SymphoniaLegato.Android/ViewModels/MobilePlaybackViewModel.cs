using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SymphoniaLegato.Android.ViewModels;

public sealed partial class MobilePlaybackViewModel : ViewModelBase
{
    [ObservableProperty] private bool   _isPlaying;
    [ObservableProperty] private double _tempoMultiplier = 1.0;
    [ObservableProperty] private bool   _isLooping;
    [ObservableProperty] private int    _loopStartMeasure = 1;
    [ObservableProperty] private int    _loopEndMeasure   = 4;
    [ObservableProperty] private int    _currentMeasure   = 1;
    [ObservableProperty] private TimeSpan _position;
    [ObservableProperty] private TimeSpan _duration;

    // Android playback uses MediaPlayer / MIDI through MidiManager;
    // for Phase 4 the play/pause/loop state is fully modelled but audio
    // is wired when a platform MIDI service is injected (Phase 4 extension).

    [RelayCommand]
    private void PlayPause() => IsPlaying = !IsPlaying;

    [RelayCommand]
    private void Stop()
    {
        IsPlaying = false;
        Position  = TimeSpan.Zero;
        CurrentMeasure = 1;
    }

    [RelayCommand]
    private void Rewind()
    {
        Position = TimeSpan.Zero;
        CurrentMeasure = LoopStartMeasure;
    }

    partial void OnLoopStartMeasureChanged(int value)
    {
        if (value >= LoopEndMeasure) LoopEndMeasure = value + 1;
    }

    partial void OnLoopEndMeasureChanged(int value)
    {
        if (value <= LoopStartMeasure) LoopStartMeasure = Math.Max(1, value - 1);
    }
}
