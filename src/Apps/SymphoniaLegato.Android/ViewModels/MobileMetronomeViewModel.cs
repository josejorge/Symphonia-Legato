using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SymphoniaLegato.PlaybackEngine;

namespace SymphoniaLegato.Android.ViewModels;

public sealed partial class MobileMetronomeViewModel : ViewModelBase, IDisposable
{
    private readonly MetronomeEngine _engine;

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private int  _bpm = 120;
    [ObservableProperty] private int  _beatsPerMeasure = 4;
    [ObservableProperty] private int  _currentBeat = 1;
    [ObservableProperty] private bool _isAccent;

    private DateTime _lastTap = DateTime.MinValue;
    private readonly List<double> _tapIntervals = [];

    public static IReadOnlyList<string> SubdivisionOptions { get; } =
        ["♩ Quarter", "♪ Eighth", "♬ Sixteenth"];

    [ObservableProperty] private int _subdivisionIndex = 0;

    public MobileMetronomeViewModel(MetronomeEngine engine)
    {
        _engine = engine;
        _engine.Beat += OnBeat;
    }

    [RelayCommand]
    private void StartStop()
    {
        if (IsRunning)
        {
            _engine.Stop();
            IsRunning = false;
            CurrentBeat = 1;
        }
        else
        {
            _engine.BPM             = Bpm;
            _engine.BeatsPerMeasure = BeatsPerMeasure;
            _engine.Subdivision     = SubdivisionIndex switch { 1 => 2, 2 => 4, _ => 1 };
            _engine.Start();
            IsRunning = true;
        }
    }

    [RelayCommand]
    private void Tap()
    {
        var now = DateTime.UtcNow;
        if (_lastTap != DateTime.MinValue)
        {
            double ms = (now - _lastTap).TotalMilliseconds;
            if (ms < 3000)
            {
                _tapIntervals.Add(ms);
                if (_tapIntervals.Count > 8) _tapIntervals.RemoveAt(0);
                Bpm = Math.Clamp((int)Math.Round(60_000.0 / _tapIntervals.Average()), 20, 400);
                if (IsRunning) _engine.BPM = Bpm;
            }
            else _tapIntervals.Clear();
        }
        _lastTap = now;
    }

    partial void OnBpmChanged(int value)            { if (IsRunning) _engine.BPM = value; }
    partial void OnBeatsPerMeasureChanged(int value) { if (IsRunning) _engine.BeatsPerMeasure = value; }

    private void OnBeat(object? sender, MetronomeBeatEventArgs e) =>
        Dispatcher.UIThread.Post(() => { CurrentBeat = e.Beat; IsAccent = e.IsAccent; });

    public void Dispose()
    {
        _engine.Beat -= OnBeat;
        _engine.Stop();
    }
}
