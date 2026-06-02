namespace SymphoniaLegato.Core.Models;

/// <summary>Standard dynamic markings.</summary>
public enum DynamicLevel
{
    Pppp = 8,  Ppp  = 16, Pp   = 24, P   = 32,
    Mp   = 56, Mf   = 64,
    F    = 80, Ff   = 96, Fff  = 112, Ffff = 127,
    Sfz  = 100, Sfzp = 68, Fp  = 40
}

/// <summary>A dynamic event attached to a score position.</summary>
public sealed class Dynamic
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DynamicLevel Level { get; set; }
    public int TickOffset { get; set; }
    public bool IsCrescendo { get; set; }
    public bool IsDecrescendo { get; set; }
    public int DurationTicks { get; set; }

    public string Symbol => Level switch
    {
        DynamicLevel.Pppp => "pppp", DynamicLevel.Ppp  => "ppp", DynamicLevel.Pp   => "pp",
        DynamicLevel.P    => "p",    DynamicLevel.Mp   => "mp",  DynamicLevel.Mf   => "mf",
        DynamicLevel.F    => "f",    DynamicLevel.Ff   => "ff",  DynamicLevel.Fff  => "fff",
        DynamicLevel.Ffff => "ffff", DynamicLevel.Sfz  => "sfz", DynamicLevel.Sfzp => "sfzp",
        DynamicLevel.Fp   => "fp",
        _ => "mf"
    };
}

/// <summary>A tempo marking with optional metronome indication.</summary>
public sealed class TempoMarking
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
    public int? BPM { get; set; }
    public NoteValue BeatUnit { get; set; } = NoteValue.Quarter;
    public int TickOffset { get; set; }

    public static TempoMarking Allegro    => new() { Text = "Allegro",     BPM = 132 };
    public static TempoMarking Moderato   => new() { Text = "Moderato",    BPM = 96  };
    public static TempoMarking Andante    => new() { Text = "Andante",     BPM = 72  };
    public static TempoMarking Adagio     => new() { Text = "Adagio",      BPM = 60  };
    public static TempoMarking Presto     => new() { Text = "Presto",      BPM = 168 };
    public static TempoMarking Largo      => new() { Text = "Largo",       BPM = 46  };
}
