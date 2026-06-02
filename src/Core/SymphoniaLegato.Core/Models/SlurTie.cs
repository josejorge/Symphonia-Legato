namespace SymphoniaLegato.Core.Models;

/// <summary>Direction a slur or tie curves.</summary>
public enum CurveDirection { Up, Down, Auto }

/// <summary>A slur spanning from one note to another within or across measures.</summary>
public sealed class Slur
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid StartNoteId { get; set; }
    public Guid EndNoteId { get; set; }
    public CurveDirection Direction { get; set; } = CurveDirection.Auto;
    public int StartMeasure { get; set; }
    public int EndMeasure { get; set; }
}

/// <summary>A tie between two notes of the same pitch.</summary>
public sealed class Tie
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid StartNoteId { get; set; }
    public Guid EndNoteId { get; set; }
    public CurveDirection Direction { get; set; } = CurveDirection.Auto;
}
