namespace SymphoniaLegato.Core.Models;

/// <summary>Musical note letters A–G.</summary>
public enum NoteName { C, D, E, F, G, A, B }

/// <summary>Chromatic alteration of a pitch.</summary>
public enum Accidental { DoubleFlat = -2, Flat = -1, Natural = 0, Sharp = 1, DoubleSharp = 2 }

/// <summary>
/// Immutable representation of an absolute pitch (letter + accidental + octave).
/// Middle C = C4, MIDI note 60.
/// </summary>
public readonly record struct Pitch(NoteName Name, Accidental Accidental, int Octave)
{
    public static readonly Pitch MiddleC = new(NoteName.C, Accidental.Natural, 4);

    // Semitone offsets from C within an octave (natural notes only)
    private static readonly int[] NaturalSemitones = { 0, 2, 4, 5, 7, 9, 11 };

    /// <summary>MIDI note number (0–127). Middle C = 60.</summary>
    public int MidiNumber =>
        (Octave + 1) * 12 + NaturalSemitones[(int)Name] + (int)Accidental;

    /// <summary>Constructs a Pitch from a raw MIDI note number.</summary>
    public static Pitch FromMidi(int midi)
    {
        int octave = (midi / 12) - 1;
        int semitone = midi % 12;
        (NoteName name, Accidental acc) = semitone switch
        {
            0  => (NoteName.C, Accidental.Natural),
            1  => (NoteName.C, Accidental.Sharp),
            2  => (NoteName.D, Accidental.Natural),
            3  => (NoteName.D, Accidental.Sharp),
            4  => (NoteName.E, Accidental.Natural),
            5  => (NoteName.F, Accidental.Natural),
            6  => (NoteName.F, Accidental.Sharp),
            7  => (NoteName.G, Accidental.Natural),
            8  => (NoteName.G, Accidental.Sharp),
            9  => (NoteName.A, Accidental.Natural),
            10 => (NoteName.A, Accidental.Sharp),
            11 => (NoteName.B, Accidental.Natural),
            _  => throw new ArgumentOutOfRangeException(nameof(midi))
        };
        return new Pitch(name, acc, octave);
    }

    public override string ToString() =>
        $"{Name}{AccidentalSymbol()}{Octave}";

    private string AccidentalSymbol() => Accidental switch
    {
        Accidental.DoubleFlat  => "bb",
        Accidental.Flat        => "b",
        Accidental.Sharp       => "#",
        Accidental.DoubleSharp => "x",
        _                      => ""
    };
}
