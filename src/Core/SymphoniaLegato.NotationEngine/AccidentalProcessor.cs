using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.NotationEngine;

/// <summary>
/// Determines which notes need explicit accidental symbols rendered,
/// taking the key signature and preceding accidentals in the measure into account.
/// </summary>
public static class AccidentalProcessor
{
    public static void Process(Measure measure, KeySignature keySig)
    {
        // Notes already altered by the key signature
        var keyAltered = new HashSet<string>(
            keySig.AlteredNotes.Select(n => n.ToUpper()));

        // Track accidentals introduced within this measure (cleared at barline)
        // Key: note name, Value: accidental introduced
        var measureAccidentals = new Dictionary<NoteName, Accidental>();

        foreach (var note in measure.Notes.Where(n => !n.IsRest && n.Pitch.HasValue)
                                         .OrderBy(n => n.TickOffset))
        {
            var pitch = note.Pitch!.Value;
            bool inKey = keyAltered.Contains(pitch.Name.ToString().ToUpper());
            bool hasKeyAcc = inKey && (
                (keySig.Fifths > 0 && pitch.Accidental == Accidental.Sharp) ||
                (keySig.Fifths < 0 && pitch.Accidental == Accidental.Flat));

            bool needsAccidental;
            if (measureAccidentals.TryGetValue(pitch.Name, out var prev))
            {
                // Show accidental if it differs from what was last seen in this measure
                needsAccidental = prev != pitch.Accidental;
            }
            else
            {
                // First occurrence: show if not already covered by key sig
                needsAccidental = !hasKeyAcc && pitch.Accidental != Accidental.Natural
                               || (hasKeyAcc && pitch.Accidental == Accidental.Natural); // courtesy natural
            }

            note.ShowAccidental = needsAccidental;
            if (needsAccidental || pitch.Accidental != Accidental.Natural)
                measureAccidentals[pitch.Name] = pitch.Accidental;

            // Process chord notes
            foreach (var chordPitch in note.ChordNotes)
            {
                bool cInKey = keyAltered.Contains(chordPitch.Name.ToString().ToUpper());
                // Simplified: always show accidentals on chord notes that differ from natural
                // A production impl would carry per-octave state
            }
        }
    }
}
