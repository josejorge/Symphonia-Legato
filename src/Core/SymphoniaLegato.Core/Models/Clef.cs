namespace SymphoniaLegato.Core.Models;

/// <summary>Supported clef types.</summary>
public enum ClefType { Treble, Bass, Alto, Tenor, Percussion }

/// <summary>Clef definition with staff-line placement and octave transposition.</summary>
public readonly record struct Clef(ClefType Type, int StaffLine = 2, int OctaveShift = 0)
{
    public static readonly Clef Treble     = new(ClefType.Treble,     2, 0);
    public static readonly Clef Bass       = new(ClefType.Bass,       4, 0);
    public static readonly Clef Alto       = new(ClefType.Alto,       3, 0);
    public static readonly Clef Tenor      = new(ClefType.Tenor,      4, 0);  // C on 4th line
    public static readonly Clef TrebleOct8 = new(ClefType.Treble,     2, -1); // 8vb
    public static readonly Clef Percussion = new(ClefType.Percussion, 3, 0);

    /// <summary>MIDI number of the note on the bottom line of the staff for this clef.</summary>
    public int BottomLineMidi => Type switch
    {
        ClefType.Treble     => 64 + OctaveShift * 12, // E4
        ClefType.Bass       => 43 + OctaveShift * 12, // G2
        ClefType.Alto       => 48 + OctaveShift * 12, // C3 (middle of staff)
        ClefType.Tenor      => 45 + OctaveShift * 12, // A2
        ClefType.Percussion => 60,
        _                   => 60
    };
}
