using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.AIEngine;

/// <summary>
/// Suggests finger numbers (1-5) for sequential notes using standard piano fingering rules.
/// Pure algorithmic analysis — no network calls.
/// </summary>
public static class FingeringAdvisor
{
    // Maximum comfortable semitone spans per hand size (thumb-to-finger)
    private static readonly int[] MaxSpan = [0, 0, 3, 5, 8, 12]; // index = finger (1-5)

    /// <summary>
    /// Produces finger assignments for every non-rest note in the given staff,
    /// working through the measures in order using a sliding hand-position model.
    /// </summary>
    public static FingeringResult SuggestForStaff(Score score, Guid staffId)
    {
        var staff = score.Parts.SelectMany(p => p.Staves).FirstOrDefault(s => s.Id == staffId);
        if (staff is null) return new FingeringResult { StaffId = staffId };

        bool isRightHand = staff.DefaultClef == Clef.Treble || staff.IsGrandStaffTop;
        var notes = staff.Measures
            .SelectMany(m => m.Notes)
            .Where(n => n.Pitch is not null)
            .OrderBy(n => n.TickOffset)
            .ToList();

        var assignments = AssignFingers(notes, isRightHand);
        return new FingeringResult { StaffId = staffId, Notes = assignments };
    }

    private static List<FingeringNote> AssignFingers(List<Note> notes, bool rightHand)
    {
        var result = new List<FingeringNote>();
        if (notes.Count == 0) return result;

        // Start with thumb on the first note
        int currentFinger = rightHand ? 1 : 5;
        int? prevMidi = null;

        foreach (var note in notes)
        {
            int midi = note.Pitch!.Value.MidiNumber;
            bool isCrossing = false;

            if (prevMidi.HasValue)
            {
                int semitones = midi - prevMidi.Value;
                (currentFinger, isCrossing) = NextFinger(currentFinger, semitones, rightHand);
            }

            result.Add(new FingeringNote
            {
                NoteId = note.Id,
                Finger = (Finger)currentFinger,
                IsCrossing = isCrossing
            });

            prevMidi = midi;
        }

        return result;
    }

    private static (int finger, bool crossing) NextFinger(int prev, int semitones, bool rightHand)
    {
        // Moving upward (ascending pitches)
        if (semitones > 0)
        {
            // If we've reached pinky (or thumb for left hand), cross thumb under
            int pinky = rightHand ? 5 : 1;
            int thumb = rightHand ? 1 : 5;

            if (prev == pinky || semitones > MaxSpan[prev] + 2)
                return (thumb, crossing: true);

            int next = rightHand ? Math.Min(prev + 1, 5) : Math.Max(prev - 1, 1);
            return (next, crossing: false);
        }

        // Moving downward (descending pitches)
        if (semitones < 0)
        {
            int pinky = rightHand ? 5 : 1;
            int thumb = rightHand ? 1 : 5;

            if (prev == thumb || Math.Abs(semitones) > MaxSpan[Math.Abs(5 - prev) + 1 > 4 ? 4 : Math.Abs(5 - prev) + 1] + 2)
                return (pinky, crossing: true);

            int next = rightHand ? Math.Max(prev - 1, 1) : Math.Min(prev + 1, 5);
            return (next, crossing: false);
        }

        // Repeated note — keep same finger
        return (prev, crossing: false);
    }
}
