namespace SymphoniaLegato.Core.Models;

/// <summary>Immutable time signature (beats per measure / beat unit).</summary>
public readonly record struct TimeSignature(int Numerator, int Denominator)
{
    public static readonly TimeSignature Common       = new(4, 4);
    public static readonly TimeSignature Cut          = new(2, 2);
    public static readonly TimeSignature ThreeFour    = new(3, 4);
    public static readonly TimeSignature SixEight     = new(6, 8);
    public static readonly TimeSignature NineEight    = new(9, 8);
    public static readonly TimeSignature TwelveEight  = new(12, 8);
    public static readonly TimeSignature TwoTwo       = new(2, 2);
    public static readonly TimeSignature TwoFour      = new(2, 4);
    public static readonly TimeSignature FiveFour     = new(5, 4);
    public static readonly TimeSignature SevenEight   = new(7, 8);

    /// <summary>Total ticks per measure (1 quarter = 1024).</summary>
    public int TicksPerMeasure =>
        (4 * Duration.Quarter.Ticks * Numerator) / Denominator;

    public bool IsCompound => Numerator % 3 == 0 && Numerator > 3;

    public override string ToString() => $"{Numerator}/{Denominator}";
}
