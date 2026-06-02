namespace SymphoniaLegato.Core.Models;

public enum Mode { Major, Minor }

/// <summary>
/// Immutable key signature.
/// Fifths: negative = flats, positive = sharps, 0 = C major / A minor.
/// </summary>
public readonly record struct KeySignature(int Fifths, Mode Mode)
{
    public static readonly KeySignature CMajor  = new(0, Mode.Major);
    public static readonly KeySignature AMinor  = new(0, Mode.Minor);
    public static readonly KeySignature GMajor  = new(1, Mode.Major);
    public static readonly KeySignature FMajor  = new(-1, Mode.Major);
    public static readonly KeySignature DMajor  = new(2, Mode.Major);
    public static readonly KeySignature BbMajor = new(-2, Mode.Major);
    public static readonly KeySignature AMajor  = new(3, Mode.Major);
    public static readonly KeySignature EbMajor = new(-3, Mode.Major);
    public static readonly KeySignature EMajor  = new(4, Mode.Major);
    public static readonly KeySignature AbMajor = new(-4, Mode.Major);
    public static readonly KeySignature BMajor  = new(5, Mode.Major);
    public static readonly KeySignature DbMajor = new(-5, Mode.Major);
    public static readonly KeySignature FsMajor = new(6, Mode.Major);
    public static readonly KeySignature GbMajor = new(-6, Mode.Major);
    public static readonly KeySignature CsMajor = new(7, Mode.Major);
    public static readonly KeySignature CbMajor = new(-7, Mode.Major);

    private static readonly string[] SharpOrder = { "F", "C", "G", "D", "A", "E", "B" };
    private static readonly string[] FlatOrder  = { "B", "E", "A", "D", "G", "C", "F" };

    public IReadOnlyList<string> AlteredNotes =>
        Fifths > 0 ? SharpOrder[..Fifths] : FlatOrder[..Math.Abs(Fifths)];

    public override string ToString()
    {
        string symbol = Fifths > 0 ? $"{Fifths}#" : Fifths < 0 ? $"{-Fifths}b" : "0";
        return $"{Mode}({symbol})";
    }
}
