using System.Timers;

namespace SymphoniaLegato.PlaybackEngine;

public sealed class MetronomeBeatEventArgs(int beat, int subdivision, bool isAccent, bool isMeasureStart) : EventArgs
{
    public int  Beat            { get; } = beat;
    public int  Subdivision     { get; } = subdivision;
    public bool IsAccent        { get; } = isAccent;
    public bool IsMeasureStart  { get; } = isMeasureStart;
}

/// <summary>
/// Platform-agnostic metronome. Fires <see cref="Beat"/> on a background thread;
/// callers are responsible for marshalling to the UI thread.
/// </summary>
public sealed class MetronomeEngine : IDisposable
{
    private System.Timers.Timer? _timer;
    private int  _beatCounter;
    private int  _subdivisionCounter;
    private bool _running;
    private readonly Lock _lock = new();

    public event EventHandler<MetronomeBeatEventArgs>? Beat;

    // ── Configuration ─────────────────────────────────────────────────

    private int _bpm = 120;
    public int BPM
    {
        get => _bpm;
        set
        {
            _bpm = Math.Clamp(value, 20, 400);
            RestartIfRunning();
        }
    }

    private int _beatsPerMeasure = 4;
    public int BeatsPerMeasure
    {
        get => _beatsPerMeasure;
        set { _beatsPerMeasure = Math.Clamp(value, 1, 16); _beatCounter = 0; }
    }

    private int _subdivision = 1;
    /// <summary>Subdivisions per beat: 1 = quarter, 2 = eighth, 4 = sixteenth.</summary>
    public int Subdivision
    {
        get => _subdivision;
        set
        {
            _subdivision = value is 1 or 2 or 4 ? value : 1;
            RestartIfRunning();
        }
    }

    public bool IsRunning => _running;

    // ── Lifecycle ─────────────────────────────────────────────────────

    public void Start()
    {
        lock (_lock)
        {
            if (_running) return;
            _beatCounter = 0;
            _subdivisionCounter = 0;
            _running = true;
            StartTimer();
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _running = false;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }
    }

    public void Dispose() => Stop();

    // ── Private ───────────────────────────────────────────────────────

    private void StartTimer()
    {
        double intervalMs = 60_000.0 / _bpm / _subdivision;
        _timer = new System.Timers.Timer(intervalMs) { AutoReset = true };
        _timer.Elapsed += OnTick;
        _timer.Start();

        // Fire the first beat immediately so there's no leading silence
        FireBeat();
    }

    private void OnTick(object? sender, ElapsedEventArgs e) => FireBeat();

    private void FireBeat()
    {
        int sub   = _subdivisionCounter % _subdivision;
        bool onBeat = sub == 0;
        bool accent  = onBeat && _beatCounter == 0;
        bool start   = onBeat && _beatCounter == 0;
        int beatNum  = _beatCounter + 1;

        Beat?.Invoke(this, new MetronomeBeatEventArgs(beatNum, sub + 1, accent, start));

        _subdivisionCounter++;
        if (_subdivisionCounter % _subdivision == 0)
        {
            _beatCounter = (_beatCounter + 1) % _beatsPerMeasure;
        }
    }

    private void RestartIfRunning()
    {
        lock (_lock)
        {
            if (!_running) return;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
            _beatCounter = 0;
            _subdivisionCounter = 0;
            StartTimer();
        }
    }
}
