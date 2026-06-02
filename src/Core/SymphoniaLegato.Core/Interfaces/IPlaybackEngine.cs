using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Core.Interfaces;

public enum PlaybackState { Stopped, Playing, Paused }

/// <summary>Playback engine contract — decouple from MIDI/audio implementation.</summary>
public interface IPlaybackEngine : IAsyncDisposable
{
    PlaybackState State { get; }
    TimeSpan Position { get; }
    TimeSpan Duration { get; }
    double TempoMultiplier { get; set; }  // 0.25–4.0
    bool IsLooping { get; set; }
    TimeSpan LoopStart { get; set; }
    TimeSpan LoopEnd { get; set; }

    event EventHandler<PlaybackPositionChangedEventArgs>? PositionChanged;
    event EventHandler<NotePlayedEventArgs>? NotePlayed;
    event EventHandler? PlaybackEnded;

    Task LoadScoreAsync(Score score, CancellationToken ct = default);
    Task PlayAsync(CancellationToken ct = default);
    Task PauseAsync();
    Task StopAsync();
    Task SeekAsync(TimeSpan position);

    void SetStaffVolume(Guid staffId, int volume);
    void SetStaffPan(Guid staffId, int pan);
    void SetStaffMuted(Guid staffId, bool muted);
    void SetStaffSolo(Guid staffId, bool solo);

    /// <summary>Plays a single note for audition (e.g. piano keyboard click).</summary>
    Task PreviewNoteAsync(Pitch pitch, int velocity = 80, int durationMs = 300);
}

public sealed class PlaybackPositionChangedEventArgs(TimeSpan position, int currentMeasure, int currentBeat)
    : EventArgs
{
    public TimeSpan Position { get; } = position;
    public int CurrentMeasure { get; } = currentMeasure;
    public int CurrentBeat { get; } = currentBeat;
}

public sealed class NotePlayedEventArgs(Pitch pitch, Guid staffId, int velocity) : EventArgs
{
    public Pitch Pitch { get; } = pitch;
    public Guid StaffId { get; } = staffId;
    public int Velocity { get; } = velocity;
}
