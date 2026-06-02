namespace SymphoniaLegato.Core.Models;

/// <summary>Standard note value types.</summary>
public enum NoteValue
{
    Maxima        = 32768,
    Long          = 16384,
    Breve         = 8192,
    Whole         = 4096,
    Half          = 2048,
    Quarter       = 1024,
    Eighth        = 512,
    Sixteenth     = 256,
    ThirtySecond  = 128,
    SixtyFourth   = 64,
    OneTwentyEighth = 32
}

/// <summary>
/// Immutable duration expressed as a base <see cref="NoteValue"/> plus optional dots.
/// The unit of measure is the "division tick" — one quarter note = 1024 ticks.
/// </summary>
public readonly record struct Duration(NoteValue Value, int Dots = 0)
{
    public static readonly Duration Whole        = new(NoteValue.Whole);
    public static readonly Duration Half         = new(NoteValue.Half);
    public static readonly Duration Quarter      = new(NoteValue.Quarter);
    public static readonly Duration Eighth       = new(NoteValue.Eighth);
    public static readonly Duration Sixteenth    = new(NoteValue.Sixteenth);
    public static readonly Duration ThirtySecond = new(NoteValue.ThirtySecond);
    public static readonly Duration DottedHalf   = new(NoteValue.Half, 1);
    public static readonly Duration DottedQuarter = new(NoteValue.Quarter, 1);
    public static readonly Duration DottedEighth  = new(NoteValue.Eighth, 1);

    /// <summary>Total duration in ticks (1 quarter = 1024).</summary>
    public int Ticks
    {
        get
        {
            int base_ = (int)Value;
            int total = base_;
            for (int i = 0; i < Dots; i++)
            {
                base_ /= 2;
                total += base_;
            }
            return total;
        }
    }

    public override string ToString() => $"{Value}{new string('.', Dots)}";
}
