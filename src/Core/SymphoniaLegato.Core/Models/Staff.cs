namespace SymphoniaLegato.Core.Models;

/// <summary>Instrument family grouping.</summary>
public enum InstrumentFamily
{
    Keyboard, Strings, Woodwind, Brass, Percussion, Voice, Other
}

/// <summary>Instrument definition (maps to a General MIDI program).</summary>
public sealed class Instrument
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "Piano";
    public string ShortName { get; set; } = "Pno.";
    public int MidiProgram { get; set; }  // 0-based GM program number
    public int MidiChannel { get; set; }
    public InstrumentFamily Family { get; set; } = InstrumentFamily.Keyboard;
    public int TranspositionSemitones { get; set; }
    public string SoundFontPreset { get; set; } = string.Empty;

    // Standard General MIDI presets
    public static Instrument GrandPiano    => new() { Name = "Grand Piano",    ShortName = "Pno.", MidiProgram = 0  };
    public static Instrument BrightPiano   => new() { Name = "Bright Piano",   ShortName = "Pno.", MidiProgram = 1  };
    public static Instrument ElectricPiano => new() { Name = "Electric Piano", ShortName = "E.Pno.", MidiProgram = 4 };
    public static Instrument Organ         => new() { Name = "Pipe Organ",     ShortName = "Org.", MidiProgram = 19, Family = InstrumentFamily.Keyboard };
    public static Instrument Violin        => new() { Name = "Violin",         ShortName = "Vln.", MidiProgram = 40, Family = InstrumentFamily.Strings };
    public static Instrument Viola         => new() { Name = "Viola",          ShortName = "Vla.", MidiProgram = 41, Family = InstrumentFamily.Strings };
    public static Instrument Cello         => new() { Name = "Cello",          ShortName = "Vc.",  MidiProgram = 42, Family = InstrumentFamily.Strings };
    public static Instrument Flute         => new() { Name = "Flute",          ShortName = "Fl.",  MidiProgram = 73, Family = InstrumentFamily.Woodwind };
    public static Instrument Clarinet      => new() { Name = "Clarinet",       ShortName = "Cl.",  MidiProgram = 71, Family = InstrumentFamily.Woodwind };
    public static Instrument Trumpet       => new() { Name = "Trumpet",        ShortName = "Tpt.", MidiProgram = 56, Family = InstrumentFamily.Brass };
    public static Instrument Trombone      => new() { Name = "Trombone",       ShortName = "Tbn.", MidiProgram = 57, Family = InstrumentFamily.Brass };
    public static Instrument Saxophone     => new() { Name = "Alto Saxophone", ShortName = "A.Sx.", MidiProgram = 65, Family = InstrumentFamily.Woodwind };
    public static Instrument Guitar        => new() { Name = "Acoustic Guitar",ShortName = "Gtr.", MidiProgram = 24, Family = InstrumentFamily.Strings };
    public static Instrument Bass          => new() { Name = "Acoustic Bass",  ShortName = "Bs.",  MidiProgram = 32, Family = InstrumentFamily.Strings };
    public static Instrument ChoirAahs     => new() { Name = "Choir Aahs",     ShortName = "Chr.", MidiProgram = 52, Family = InstrumentFamily.Voice };
}

/// <summary>
/// A single staff line belonging to a part.
/// Contains an ordered list of measures and playback/mixing properties.
/// </summary>
public sealed class Staff
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Instrument Instrument { get; set; } = Instrument.GrandPiano;
    public Clef DefaultClef { get; set; } = Clef.Treble;

    public List<Measure> Measures { get; init; } = [];

    // Mixing
    public int Volume { get; set; } = 100;   // 0–127
    public int Pan { get; set; } = 64;        // 0=left 64=center 127=right
    public bool IsMuted { get; set; }
    public bool IsSolo { get; set; }

    // Grand staff: link treble + bass staves
    public Guid? LinkedStaffId { get; set; }
    public bool IsGrandStaffTop { get; set; }

    public Measure? GetMeasure(int number) =>
        Measures.FirstOrDefault(m => m.Number == number);

    public Measure GetOrAddMeasure(int number, TimeSignature timeSig)
    {
        var m = GetMeasure(number);
        if (m is not null) return m;
        m = new Measure { Number = number, TimeSignature = timeSig };
        Measures.Add(m);
        Measures.Sort((a, b) => a.Number.CompareTo(b.Number));
        return m;
    }
}
