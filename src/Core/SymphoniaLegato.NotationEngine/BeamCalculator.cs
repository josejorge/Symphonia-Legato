using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.NotationEngine;

/// <summary>
/// Groups eighth/sixteenth notes into beam groups within a measure,
/// following the beat structure of the time signature.
/// </summary>
public static class BeamCalculator
{
    private static readonly HashSet<NoteValue> BeamableValues =
    [
        NoteValue.Eighth, NoteValue.Sixteenth, NoteValue.ThirtySecond, NoteValue.SixtyFourth
    ];

    public static void AssignBeams(Measure measure)
    {
        // Reset all beam assignments
        foreach (var note in measure.Notes)
        {
            note.BeamGroup = 0;
            note.IsBeamStart = false;
            note.IsBeamEnd = false;
        }

        int beatGroupSize = GetBeatGroupTicks(measure.TimeSignature);
        var beamableNotes = measure.Notes
            .Where(n => !n.IsRest && BeamableValues.Contains(n.Duration.Value))
            .OrderBy(n => n.TickOffset)
            .ToList();

        if (beamableNotes.Count < 2) return;

        // Group notes that fall within the same beat group
        int groupId = 1;
        int i = 0;
        while (i < beamableNotes.Count)
        {
            int beatStart = (beamableNotes[i].TickOffset / beatGroupSize) * beatGroupSize;
            int beatEnd   = beatStart + beatGroupSize;

            var groupNotes = beamableNotes
                .Skip(i)
                .TakeWhile(n => n.TickOffset >= beatStart && n.TickOffset < beatEnd)
                .ToList();

            if (groupNotes.Count >= 2)
            {
                var direction = StemDirectionCalculator.CalculateForGroup(groupNotes);
                for (int j = 0; j < groupNotes.Count; j++)
                {
                    groupNotes[j].BeamGroup = groupId;
                    groupNotes[j].Stem = direction;
                    groupNotes[j].IsBeamStart = j == 0;
                    groupNotes[j].IsBeamEnd   = j == groupNotes.Count - 1;
                }
                groupId++;
            }
            else if (groupNotes.Count == 1)
            {
                // Single beamable note: gets a flag instead
                groupNotes[0].Stem = StemDirectionCalculator.Calculate(groupNotes[0]);
            }

            i += groupNotes.Count == 0 ? 1 : groupNotes.Count;
        }
    }

    private static int GetBeatGroupTicks(TimeSignature ts)
    {
        // Compound time: group in dotted-quarter beats
        if (ts.IsCompound)
            return Duration.DottedQuarter.Ticks;

        // Simple time: one beat per quarter note (or denominator unit)
        return 4 * Duration.Quarter.Ticks / ts.Denominator;
    }
}
