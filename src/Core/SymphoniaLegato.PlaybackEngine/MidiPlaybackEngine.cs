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

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;
    public TimeSpan Position => _playback?.GetCurrentTime(TimeSpanType.Metric) as MetricTimeSpan ?? TimeSpan.Zero;
    public TimeSpan Duration { get; private set; }
    public double TempoMultiplier { get; set; } = 1.0;
    public bool IsLooping { get; set; }
    public TimeSpan LoopStart { get; set; }
    public TimeSpan LoopEnd { get; set; }

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
        _midiFile = _converter.Convert(score);
        Duration = _midiFile.GetDuration<MetricTimeSpan>();
        _logger.LogInformation("Score loaded for playback. Duration: {Duration}", Duration);
    }

    public async Task PlayAsync(CancellationToken ct = default)
    {
        if (_midiFile is null) return;

        try
        {
            _outputDevice ??= OutputDevice.GetByIndex(0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No MIDI output device available — using null device");
            // Fallback: no-op output
        }

        if (State == PlaybackState.Paused && _playback is not null)
        {
            _playback.Start();
            State = PlaybackState.Playing;
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

        _playback.Start();
        State = PlaybackState.Playing;
        _logger.LogInformation("Playback started");

        await Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        _playback?.Stop();
        State = PlaybackState.Paused;
        _logger.LogDebug("Playback paused at {Position}", Position);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        DisposePlayback();
        State = PlaybackState.Stopped;
        return Task.CompletedTask;
    }

    public Task SeekAsync(TimeSpan position)
    {
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

    private void OnPlaybackFinished(object? sender, EventArgs e)
    {
        State = PlaybackState.Stopped;
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
