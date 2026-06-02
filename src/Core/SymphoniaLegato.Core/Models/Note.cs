namespace SymphoniaLegato.Core.Models;

/// <summary>Articulation marking on a note.</summary>
public enum Articulation
{
    None, Staccato, Staccatissimo, Accent, Tenuto,
    Marcato, Fermata, Trill, Mordent, Turn, Snap
}

/// <summary>Which hand plays this note (for piano coloring).</summary>
public enum Hand { Unassigned, Right, Left }

/// <summary>
/// A single pitched note (or rest) within a measure, carrying all its musical properties.
/// </summary>
public sealed class Note
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Null = rest.</summary>
    public Pitch? Pitch { get; set; }

    public Duration Duration { get; set; }
    public int TickOffset { get; set; }

    public bool IsRest => Pitch is null;
    public bool IsTiedFrom { get; set; }
    public bool IsTiedTo { get; set; }
    public bool IsSlurStart { get; set; }
    public bool IsSlurEnd { get; set; }

    public Articulation Articulation { get; set; } = Articulation.None;
    public Hand Hand { get; set; } = Hand.Unassigned;

    /// <summary>Semitone offset for playback (e.g. vibrato, bend).</summary>
    public float PitchBend { get; set; }

    /// <summary>Velocity 0–127.</summary>
    public int Velocity { get; set; } = 80;

    /// <summary>Additional notes forming a chord with this note.</summary>
    public List<Pitch> ChordNotes { get; init; } = [];

    /// <summary>Fingering suggestion (1–5, 0 = none).</summary>
    public int Fingering { get; set; }

    /// <summary>Staff-line/space position from the bottom line of the staff (0-based).</summary>
    public int StaffPosition { get; set; }

    public override string ToString() =>
        IsRest ? $"Rest({Duration})" : $"{Pitch}({Duration})";
}
