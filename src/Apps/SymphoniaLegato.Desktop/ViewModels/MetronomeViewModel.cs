using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SymphoniaLegato.PlaybackEngine;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class MetronomeViewModel : ViewModelBase, IDisposable
{
    private readonly MetronomeEngine _engine;

    [ObservableProperty] private bool   _isRunning;
    [ObservableProperty] private int    _bpm = 120;
    [ObservableProperty] private int    _beatsPerMeasure = 4;
    [ObservableProperty] private int    _subdivisionIndex = 0; // 0=♩ 1=♪ 2=♬
    [ObservableProperty] private int    _currentBeat = 1;
    [ObservableProperty] private bool   _isAccent;
    [ObservableProperty] private string _tapLabel = "TAP";

    private DateTime _lastTap = DateTime.MinValue;
    private readonly List<double> _tapIntervals = new(8);

    public static IReadOnlyList<string> SubdivisionOptions { get; } =
        ["♩ Quarter", "♪ Eighth", "♬ Sixteenth"];

    public MetronomeViewModel(MetronomeEngine engine)
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
            IsRunning   = false;
            CurrentBeat = 1;
            IsAccent    = false;
        }
        else
        {
            ApplySettings();
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
            double interval = (now - _lastTap).TotalMilliseconds;
            if (interval < 3000)
            {
                _tapIntervals.Add(interval);
                if (_tapIntervals.Count > 8) _tapIntervals.RemoveAt(0);

                int newBpm = (int)Math.Round(60_000.0 / _tapIntervals.Average());
                Bpm = Math.Clamp(newBpm, 20, 400);
                if (IsRunning) _engine.BPM = Bpm;
            }
            else
            {
                _tapIntervals.Clear();
            }
        }
        _lastTap = now;
    }

    partial void OnBpmChanged(int value)
    {
        if (IsRunning) _engine.BPM = value;
    }

    partial void OnBeatsPerMeasureChanged(int value)
    {
        if (IsRunning) _engine.BeatsPerMeasure = value;
    }

    partial void OnSubdivisionIndexChanged(int value)
    {
        if (IsRunning)
        {
            _engine.Subdivision = value switch { 1 => 2, 2 => 4, _ => 1 };
        }
    }

    private void ApplySettings()
    {
        _engine.BPM             = Bpm;
        _engine.BeatsPerMeasure = BeatsPerMeasure;
        _engine.Subdivision     = SubdivisionIndex switch { 1 => 2, 2 => 4, _ => 1 };
    }

    private void OnBeat(object? sender, MetronomeBeatEventArgs e)
    {
        // Marshal to UI thread
        Dispatcher.UIThread.Post(() =>
        {
            CurrentBeat = e.Beat;
            IsAccent    = e.IsAccent;
        });
    }

    public void Dispose()
    {
        _engine.Beat -= OnBeat;
        _engine.Stop();
    }
}
