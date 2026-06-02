namespace SymphoniaLegato.Core.Models;

/// <summary>Page size presets for printing/export.</summary>
public enum PageSize { Letter, Legal, A4, A3, Custom }

/// <summary>
/// Top-level score document. Contains all staves, global settings,
/// and metadata required to save/load a .enscore file.
/// </summary>
public sealed class Score
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; set; } = "Untitled";
    public string Subtitle { get; set; } = string.Empty;
    public string Composer { get; set; } = string.Empty;
    public string Lyricist { get; set; } = string.Empty;
    public string Arranger { get; set; } = string.Empty;
    public string Copyright { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public string FormatVersion { get; set; } = "1.0";

    // Global defaults (overridden per measure)
    public TimeSignature InitialTimeSignature { get; set; } = TimeSignature.Common;
    public KeySignature InitialKeySignature { get; set; } = KeySignature.CMajor;
    public int InitialTempo { get; set; } = 120;

    public List<Part> Parts { get; init; } = [];

    public List<ScoreAnnotation> Annotations { get; init; } = [];

    public ScoreAnnotation GetOrCreateAnnotation(int pageNumber)
    {
        var ann = Annotations.FirstOrDefault(a => a.PageNumber == pageNumber);
        if (ann is not null) return ann;
        ann = new ScoreAnnotation { PageNumber = pageNumber };
        Annotations.Add(ann);
        return ann;
    }

    // Page / layout settings
    public PageSize PageSize { get; set; } = PageSize.A4;
    public double PageWidthMm { get; set; } = 210;
    public double PageHeightMm { get; set; } = 297;
    public double MarginTopMm { get; set; } = 15;
    public double MarginBottomMm { get; set; } = 15;
    public double MarginLeftMm { get; set; } = 15;
    public double MarginRightMm { get; set; } = 15;

    public int TotalMeasures => Parts.Count > 0
        ? Parts.Max(p => p.Staves.Count > 0 ? p.Staves.Max(s => s.Measures.Count) : 0)
        : 0;

    /// <summary>Creates a new score configured for solo piano (grand staff).</summary>
    public static Score CreatePianoScore(string title = "Untitled")
    {
        var score = new Score { Title = title };
        var part = new Part { Name = "Piano", ShortName = "Pno." };

        var treble = new Staff
        {
            Name = "Piano",
            Instrument = Instrument.GrandPiano,
            DefaultClef = Clef.Treble,
            IsGrandStaffTop = true
        };
        var bass = new Staff
        {
            Name = "Piano",
            Instrument = Instrument.GrandPiano,
            DefaultClef = Clef.Bass
        };

        treble.LinkedStaffId = bass.Id;
        bass.LinkedStaffId = treble.Id;

        part.Staves.Add(treble);
        part.Staves.Add(bass);
        score.Parts.Add(part);
        return score;
    }
}

/// <summary>An instrumental part (may contain 1–2 staves for grand staff).</summary>
public sealed class Part
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public List<Staff> Staves { get; init; } = [];
    public int OrderIndex { get; set; }
}
