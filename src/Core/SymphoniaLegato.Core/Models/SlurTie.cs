// File: SlurTie.cs
// Description: Slur and tie models connecting notes within or across measures.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-01
// Last edit date: 2026-09-15
// Version: 1.0.0

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
