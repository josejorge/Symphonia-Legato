using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.NotationEngine;

/// <summary>Tuplet definition (e.g. triplet = 3 notes in the space of 2).</summary>
public sealed class Tuplet
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int ActualNotes { get; set; }    // e.g. 3 for triplet
    public int NormalNotes { get; set; }    // e.g. 2 for triplet
    public NoteValue NoteType { get; set; } = NoteValue.Eighth;
    public List<Note> Notes { get; init; } = [];
    public bool ShowBracket { get; set; } = true;
    public bool ShowNumber { get; set; } = true;

    /// <summary>Scale factor applied to each note duration.</summary>
    public double DurationScale => (double)NormalNotes / ActualNotes;

    public int TotalTicks =>
        (int)(new Duration(NoteType).Ticks * NormalNotes);
}

/// <summary>Builds and validates tuplet groups.</summary>
public static class TupletEngine
{
    public static Tuplet CreateTriplet(NoteValue noteType = NoteValue.Eighth) =>
        new() { ActualNotes = 3, NormalNotes = 2, NoteType = noteType };

    public static Tuplet CreateQuintuplet(NoteValue noteType = NoteValue.Sixteenth) =>
        new() { ActualNotes = 5, NormalNotes = 4, NoteType = noteType };

    public static bool Validate(Tuplet tuplet) =>
        tuplet.Notes.Count == tuplet.ActualNotes &&
        tuplet.ActualNotes > 1 &&
        tuplet.NormalNotes > 0;

    /// <summary>Adjusts note tick offsets within a tuplet group.</summary>
    public static void RecalculateOffsets(Tuplet tuplet)
    {
        int baseTicks = (int)(new Duration(tuplet.NoteType).Ticks * tuplet.DurationScale);
        int offset = 0;
        foreach (var note in tuplet.Notes)
        {
            note.TickOffset = offset;
            offset += baseTicks;
        }
    }
}
