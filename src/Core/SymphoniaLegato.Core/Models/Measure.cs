namespace SymphoniaLegato.Core.Models;

/// <summary>Barline style.</summary>
public enum BarlineType
{
    Single, Double, Final, RepeatStart, RepeatEnd, RepeatBoth, Dashed, Dotted, None
}

/// <summary>
/// One measure of music on a single staff. Carries notes, clef changes,
/// time/key changes, dynamics, and barline information.
/// </summary>
public sealed class Measure
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int Number { get; set; }

    public TimeSignature TimeSignature { get; set; } = TimeSignature.Common;
    public KeySignature? KeySignatureChange { get; set; }
    public Clef? ClefChange { get; set; }

    public List<Note> Notes { get; init; } = [];
    public List<Dynamic> Dynamics { get; init; } = [];
    public List<TempoMarking> TempoMarkings { get; init; } = [];
    public List<string> Lyrics { get; init; } = [];
    public List<TextAnnotation> Annotations { get; init; } = [];

    public BarlineType StartBarline { get; set; } = BarlineType.Single;
    public BarlineType EndBarline { get; set; } = BarlineType.Single;

    public bool IsPickup { get; set; }
    public int RepeatCount { get; set; } = 1;

    /// <summary>Total ticks currently occupied by notes/rests.</summary>
    public int UsedTicks => Notes.Sum(n => n.Duration.Ticks);

    /// <summary>Ticks remaining before measure is full.</summary>
    public int RemainingTicks => TimeSignature.TicksPerMeasure - UsedTicks;

    public bool IsFull => RemainingTicks <= 0;

    public void AddNote(Note note)
    {
        note.TickOffset = UsedTicks;
        Notes.Add(note);
    }
}

/// <summary>Free-text annotation placed at a score position.</summary>
public sealed class TextAnnotation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
    public int TickOffset { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public double FontSize { get; set; } = 10;
}
