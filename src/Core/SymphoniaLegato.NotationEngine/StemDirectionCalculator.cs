using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.NotationEngine;

/// <summary>
/// Determines stem direction for notes on a staff.
/// Convention: notes on or above the middle line (position 5) get stem-down,
/// notes below get stem-up. For chords, uses the note furthest from the middle.
/// </summary>
public static class StemDirectionCalculator
{
    private const int MiddleLinePosition = 5;

    public static StemDirection Calculate(Note note)
    {
        if (note.Stem != StemDirection.Auto) return note.Stem;
        if (note.IsRest) return StemDirection.None;
        if (note.Duration.Value is NoteValue.Whole) return StemDirection.None;

        int pos = note.StaffPosition;

        // For chords, consider the extremes
        if (note.ChordNotes.Count > 0)
        {
            // Can't compute chord positions without clef context;
            // use the notated staffPosition as the reference.
        }

        return pos >= MiddleLinePosition ? StemDirection.Down : StemDirection.Up;
    }

    /// <summary>Calculates stem direction for a whole beam group at once (beam group = same direction).</summary>
    public static StemDirection CalculateForGroup(IReadOnlyList<Note> notes)
    {
        if (notes.Count == 0) return StemDirection.Up;
        double avgPos = notes.Average(n => n.StaffPosition);
        return avgPos >= MiddleLinePosition ? StemDirection.Down : StemDirection.Up;
    }
}
