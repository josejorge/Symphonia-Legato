using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Microsoft.Extensions.Logging;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.PlaybackEngine;

/// <summary>
/// MIDI-based playback engine using DryWetMidi.
/// Converts a <see cref="Score"/> to a MidiFile and plays it via the system output device.
/// </summary>
public sealed class MidiPlaybackEngine : IPlaybackEngine
{
    private readonly ILogger<MidiPlaybackEngine> _logger;
    private readonly ScoreToMidiConverter _converter;
    private readonly Dictionary<Guid, MixerChannel> _channels = new();

    private Playback? _playback;
    private OutputDevice? _outputDevice;
    private MidiFile? _midiFile;
    private Score? _score;
    private System.Timers.Timer? _positionTimer;

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;
    public TimeSpan Position => _playback?.GetCurrentTime(TimeSpanType.Metric) as MetricTimeSpan ?? TimeSpan.Zero;
    public TimeSpan Duration { get; private set; }
    public double TempoMultiplier { get; set; } = 1.0;
    public bool IsLooping { get; set; }
    public TimeSpan LoopStart { get; set; }
    public TimeSpan LoopEnd { get; set; }
    public bool MetronomeEnabled { get; set; }
    public bool CountInEnabled { get; set; }

    private TimeSpan _pendingSeek = TimeSpan.Zero;  // start position applied when (re)building playback

    public event EventHandler<PlaybackPositionChangedEventArgs>? PositionChanged;
    public event EventHandler<NotePlayedEventArgs>? NotePlayed;
    public event EventHandler? PlaybackEnded;

    public MidiPlaybackEngine(ILogger<MidiPlaybackEngine> logger, ScoreToMidiConverter converter)
    {
        _logger = logger;
        _converter = converter;
    }

    public async Task LoadScoreAsync(Score score, CancellationToken ct = default)
    {
        await StopAsync();
        _score = score;
        // Bake the metronome track into the MIDI when enabled, so beat clicks are
        // sample-accurate rather than fired from the (jittery) UI position timer.
        _midiFile = _converter.Convert(score, MetronomeEnabled);
        Duration = _midiFile.GetDuration<MetricTimeSpan>();
        _logger.LogInformation("Score loaded for playback. Duration: {Duration}", Duration);
    }

    public async Task PlayAsync(CancellationToken ct = default)
    {
        if (_midiFile is null) return;

        EnsureOutputDevice();

        if (State == PlaybackState.Paused && _playback is not null)
        {
            _playback.Start();
            State = PlaybackState.Playing;
            StartPositionTimer();
            return;
        }

        DisposePlayback();

        if (_outputDevice is not null)
        {
            _playback = _midiFile.GetPlayback(_outputDevice);
        }
        else
        {
            // Null playback for headless/testing
            State = PlaybackState.Playing;
            return;
        }

        _playback.Speed = TempoMultiplier;
        _playback.Loop = IsLooping;
        _playback.Finished += OnPlaybackFinished;
        _playback.EventPlayed += OnEventPlayed;

        if (_pendingSeek > TimeSpan.Zero)
            _playback.MoveToTime((MetricTimeSpan)_pendingSeek);

        if (CountInEnabled)
            await PlayCountInAsync();

        _playback.Start();
        State = PlaybackState.Playing;
        StartPositionTimer();
        _logger.LogInformation("Playback started");
    }

    public Task PauseAsync()
    {
        _playback?.Stop();
        State = PlaybackState.Paused;
        StopPositionTimer();
        _logger.LogDebug("Playback paused at {Position}", Position);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        DisposePlayback();
        State = PlaybackState.Stopped;
        StopPositionTimer();
        _pendingSeek = TimeSpan.Zero;
        return Task.CompletedTask;
    }

    public Task SeekAsync(TimeSpan position)
    {
        // Remember the position so it is applied when playback is (re)built,
        // and apply it live if playback already exists.
        _pendingSeek = position;
        _playback?.MoveToTime((MetricTimeSpan)position);
        return Task.CompletedTask;
    }

    public void SetStaffVolume(Guid staffId, int volume)
    {
        GetOrCreateChannel(staffId).Volume = volume;
        ApplyChannelToMidi(staffId);
    }

    public void SetStaffPan(Guid staffId, int pan)
    {
        GetOrCreateChannel(staffId).Pan = pan;
        ApplyChannelToMidi(staffId);
    }

    public void SetStaffMuted(Guid staffId, bool muted)
    {
        GetOrCreateChannel(staffId).IsMuted = muted;
        ApplyChannelToMidi(staffId);
    }

    public void SetStaffSolo(Guid staffId, bool solo)
    {
        GetOrCreateChannel(staffId).IsSolo = solo;
    }

    public async Task PreviewNoteAsync(Pitch pitch, int velocity = 80, int durationMs = 300)
    {
        EnsureOutputDevice();
        if (_outputDevice is null) return;
        try
        {
            var noteNumber = (Melanchall.DryWetMidi.Common.SevenBitNumber)(byte)pitch.MidiNumber;
            var vel = (Melanchall.DryWetMidi.Common.SevenBitNumber)(byte)velocity;
            _outputDevice.SendEvent(new NoteOnEvent(noteNumber, vel));
            await Task.Delay(durationMs);
            _outputDevice.SendEvent(new NoteOffEvent(noteNumber, (Melanchall.DryWetMidi.Common.SevenBitNumber)(byte)0));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Preview note failed");
        }
    }

    /// <summary>
    /// Lazily opens the system's first MIDI output device (index 0 — the
    /// Microsoft GS Wavetable Synth on Windows). Shared by playback and the
    /// piano-keyboard preview so both produce sound. Safe to call repeatedly.
    /// </summary>
    private void EnsureOutputDevice()
    {
        if (_outputDevice is not null) return;
        try
        {
            _outputDevice = OutputDevice.GetByIndex(0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No MIDI output device available — playback will be silent");
        }
    }

    // ── Position reporting ───────────────────────────────────────────

    private void StartPositionTimer()
    {
        _positionTimer ??= new System.Timers.Timer(50) { AutoReset = true };
        _positionTimer.Elapsed -= OnPositionTimer;
        _positionTimer.Elapsed += OnPositionTimer;
        _positionTimer.Start();
    }

    private void StopPositionTimer() => _positionTimer?.Stop();

    private void OnPositionTimer(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (State != PlaybackState.Playing) return;
        var pos = Position;
        var (measure, beat) = ComputeMeasureBeat(pos);
        // (The per-beat metronome click is baked into the MIDI in LoadScoreAsync —
        //  no timer-driven click here. The count-in still uses SendClick below.)
        PositionChanged?.Invoke(this, new PlaybackPositionChangedEventArgs(pos, measure, beat));
    }

    /// <summary>Plays one bar of metronome clicks before playback begins.</summary>
    private async Task PlayCountInAsync()
    {
        var ts = _score?.Parts.SelectMany(p => p.Staves).FirstOrDefault()?.Measures.FirstOrDefault()?.TimeSignature
                 ?? _score?.InitialTimeSignature ?? SymphoniaLegato.Core.Models.TimeSignature.Common;
        double bpm = Math.Max(1, _score?.InitialTempo ?? 120);
        int beats = Math.Max(1, ts.Numerator);
        int beatMs = (int)(60_000.0 / bpm * (4.0 / ts.Denominator));
        for (int b = 0; b < beats; b++)
        {
            SendClick(accent: b == 0);
            await Task.Delay(beatMs);
        }
    }

    /// <summary>Sends a single metronome click on the GM percussion channel (10).</summary>
    private void SendClick(bool accent)
    {
        if (_outputDevice is null) return;
        try
        {
            var note = (Melanchall.DryWetMidi.Common.SevenBitNumber)(byte)(accent ? 76 : 77); // hi/lo wood block
            var vel  = (Melanchall.DryWetMidi.Common.SevenBitNumber)(byte)(accent ? 115 : 85);
            _outputDevice.SendEvent(new NoteOnEvent(note, vel)
            {
                Channel = (Melanchall.DryWetMidi.Common.FourBitNumber)9
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Metronome click failed");
        }
    }

    /// <summary>Maps an elapsed playback time to (measure number, beat) using the score's tempo.</summary>
    private (int measure, int beat) ComputeMeasureBeat(TimeSpan pos)
    {
        var staff = _score?.Parts.SelectMany(p => p.Staves).FirstOrDefault();
        if (_score is null || staff is null) return (1, 1);

        double bpm = Math.Max(1, _score.InitialTempo);
        double totalTicks = pos.TotalSeconds * bpm / 60.0 * 1024.0;

        double acc = 0;
        foreach (var m in staff.Measures.OrderBy(m => m.Number))
        {
            int cap = Math.Max(1, m.TimeSignature.TicksPerMeasure);
            if (totalTicks < acc + cap)
            {
                int ticksPerBeat = Math.Max(1, cap / Math.Max(1, m.TimeSignature.Numerator));
                int beat = (int)((totalTicks - acc) / ticksPerBeat) + 1;
                return (m.Number, beat);
            }
            acc += cap;
        }
        return (staff.Measures.Count, 1);
    }

    private void OnPlaybackFinished(object? sender, EventArgs e)
    {
        State = PlaybackState.Stopped;
        StopPositionTimer();
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }

    private void OnEventPlayed(object? sender, MidiEventPlayedEventArgs e)
    {
        if (e.Event is NoteOnEvent noteOn && noteOn.Velocity > 0 && _score is not null)
        {
            var pitch = Pitch.FromMidi(noteOn.NoteNumber);
            NotePlayed?.Invoke(this, new NotePlayedEventArgs(pitch, Guid.Empty, noteOn.Velocity));
        }
    }

    private MixerChannel GetOrCreateChannel(Guid staffId)
    {
        if (!_channels.TryGetValue(staffId, out var ch))
        {
            ch = new MixerChannel();
            _channels[staffId] = ch;
        }
        return ch;
    }

    private void ApplyChannelToMidi(Guid staffId)
    {
        // Volume/pan CC changes are sent live during playback
        if (_outputDevice is null || !_channels.TryGetValue(staffId, out var ch)) return;
        // The staff→MIDI channel mapping is built inside ScoreToMidiConverter during conversion.
        // To send live CC 7 (volume) / CC 10 (pan) here, expose that map and call
        // _outputDevice.SendEvent(new ControlChangeEvent(ControlNumber.Volume, value) { Channel = n }).
    }

    private void DisposePlayback()
    {
        if (_playback is null) return;
        _playback.Stop();
        _playback.Finished -= OnPlaybackFinished;
        _playback.EventPlayed -= OnEventPlayed;
        _playback.Dispose();
        _playback = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _positionTimer?.Dispose();
        _outputDevice?.Dispose();
    }

    private sealed class MixerChannel
    {
        public int Volume { get; set; } = 100;
        public int Pan { get; set; } = 64;
        public bool IsMuted { get; set; }
        public bool IsSolo { get; set; }
    }
}
