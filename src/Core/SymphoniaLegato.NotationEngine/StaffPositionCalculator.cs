using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.NotationEngine;

/// <summary>
/// Converts between MIDI pitch numbers and staff line/space positions.
/// Position 0 = first ledger line below the staff.
/// Position 1 = bottom line of the staff.
/// Positions increase upward (2=space, 3=2nd line, etc.)
/// </summary>
public static class StaffPositionCalculator
{
    // Diatonic steps from C (C=0, D=1, E=2, F=3, G=4, A=5, B=6)
    private static readonly int[] DiatonicStep = { 0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 5, 6 };

    /// <summary>
    /// Returns the staff position (half-steps from the bottom staff line).
    /// Positive = above bottom line, negative = below.
    /// </summary>
    public static int Calculate(Pitch pitch, Clef clef)
    {
        int midi = pitch.MidiNumber;
        int refMidi = clef.BottomLineMidi;

        // Convert both to diatonic position (C0 = 0)
        int diatonicNote = DiatonicStep[midi % 12] + (midi / 12) * 7;
        int diatonicRef  = DiatonicStep[refMidi % 12] + (refMidi / 12) * 7;

        // +1 because position 1 = bottom line (ref note), 0 = space below it
        return diatonicNote - diatonicRef + 1;
    }

    /// <summary>Returns how many ledger lines are needed and whether they're above or below.</summary>
    public static (int count, bool above) GetLedgerLines(int staffPosition)
    {
        // Staff occupies positions 1–9 (5 lines, 4 spaces)
        if (staffPosition <= 0)
            return ((Math.Abs(staffPosition) + 1) / 2, false);
        if (staffPosition > 9)
            return ((staffPosition - 9 + 1) / 2, true);
        return (0, false);
    }

    /// <summary>Reconstructs the nearest pitch matching a staff position + key signature.</summary>
    public static Pitch FromStaffPosition(int staffPosition, Clef clef, KeySignature key)
    {
        int refMidi = clef.BottomLineMidi;
        int refDiatonic = DiatonicStep[refMidi % 12] + (refMidi / 12) * 7;
        int targetDiatonic = refDiatonic + staffPosition;

        int octave = targetDiatonic / 7;
        int step = targetDiatonic % 7;
        if (step < 0) { step += 7; octave--; }

        // Map diatonic step back to note name
        NoteName name = (NoteName)step;

        // Apply key signature accidentals
        Accidental acc = Accidental.Natural;
        if (key.Fifths > 0 && key.AlteredNotes.Contains(name.ToString()))
            acc = Accidental.Sharp;
        else if (key.Fifths < 0 && key.AlteredNotes.Contains(name.ToString()))
            acc = Accidental.Flat;

        return new Pitch(name, acc, octave);
    }
}
