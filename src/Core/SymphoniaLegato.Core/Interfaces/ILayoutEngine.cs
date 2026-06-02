using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Core.Interfaces;

/// <summary>Result of a layout computation — a list of ready-to-render pages.</summary>
public sealed class LayoutResult
{
    public IReadOnlyList<RenderedPage> Pages { get; init; } = [];
    public TimeSpan ComputationTime { get; init; }
}

/// <summary>One rendered page, containing system layout information.</summary>
public sealed class RenderedPage
{
    public int PageNumber { get; init; }
    public double WidthPx { get; init; }
    public double HeightPx { get; init; }
    public IReadOnlyList<RenderedSystem> Systems { get; init; } = [];
}

/// <summary>A system (horizontal row of staves) within a page.</summary>
public sealed class RenderedSystem
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    public int FirstMeasure { get; init; }
    public int LastMeasure { get; init; }
    public IReadOnlyList<RenderedStaff> Staves { get; init; } = [];
}

/// <summary>A single staff line within a system, with per-measure element positions.</summary>
public sealed class RenderedStaff
{
    public Guid StaffId { get; init; }
    public double Y { get; init; }
    public double Height { get; init; }
    public IReadOnlyList<RenderedMeasure> Measures { get; init; } = [];
}

/// <summary>A measure with absolute pixel coordinates for all its elements.</summary>
public sealed class RenderedMeasure
{
    public int MeasureNumber { get; init; }
    public double X { get; init; }
    public double Width { get; init; }
    public BarlineType StartBarline { get; init; }
    public BarlineType EndBarline { get; init; }
    public bool ShowClef { get; init; }
    public bool ShowTimeSignature { get; init; }
    public bool ShowKeySignature { get; init; }
    public TimeSignature TimeSignature { get; init; }
    public KeySignature KeySignature { get; init; }
    public ClefType ClefType { get; init; }
    public IReadOnlyList<RenderedNoteElement> Elements { get; init; } = [];
    public IReadOnlyList<RenderedHairpin> Hairpins { get; init; } = [];
    public IReadOnlyList<RenderedSlur> Slurs { get; init; } = [];
    public IReadOnlyList<RenderedDynamic> Dynamics { get; init; } = [];
    public IReadOnlyList<RenderedTempo> TempoMarkings { get; init; } = [];
    public IReadOnlyList<RenderedBeam> Beams { get; init; } = [];
}

/// <summary>A rendered hairpin (crescendo/decrescendo) with pixel coordinates.</summary>
public sealed class RenderedHairpin
{
    public double StartX { get; init; }
    public double EndX { get; init; }
    public double Y { get; init; }
    public HairpinType Type { get; init; }
}

/// <summary>A rendered slur with Bézier control points.</summary>
public sealed class RenderedSlur
{
    public double StartX { get; init; }
    public double StartY { get; init; }
    public double EndX { get; init; }
    public double EndY { get; init; }
    public double Cp1X { get; init; }
    public double Cp1Y { get; init; }
    public double Cp2X { get; init; }
    public double Cp2Y { get; init; }
    public bool CurvesUp { get; init; }
}

/// <summary>A rendered dynamic marking.</summary>
public sealed class RenderedDynamic
{
    public string Symbol { get; init; } = string.Empty;
    public double X { get; init; }
    public double Y { get; init; }
}

/// <summary>A rendered tempo marking.</summary>
public sealed class RenderedTempo
{
    public string Text { get; init; } = string.Empty;
    public int? BPM { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
}

/// <summary>A rendered beam (filled rectangle connecting two note stems).</summary>
public sealed class RenderedBeam
{
    public double StartX { get; init; }
    public double StartY { get; init; }
    public double EndX { get; init; }
    public double EndY { get; init; }
    public int BeamLevel { get; init; }  // 0 = primary beam, 1 = secondary (16th), etc.
}

/// <summary>A note/rest/chord element with its absolute render position and all Phase 2 metadata.</summary>
public sealed class RenderedNoteElement
{
    public Guid NoteId { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public int StaffPosition { get; init; }
    public bool IsRest { get; init; }

    // Ledger lines
    public bool NeedsLedgerLines { get; init; }
    public int LedgerLineCount { get; init; }
    public bool LedgerLinesAbove { get; init; }

    // Notehead / duration
    public NoteValue NoteValue { get; init; }
    public int Dots { get; init; }

    // Stem
    public StemDirection Stem { get; init; }
    public double StemEndY { get; init; }  // absolute Y of stem tip

    // Beam
    public int BeamGroup { get; init; }
    public bool IsBeamStart { get; init; }
    public bool IsBeamEnd { get; init; }

    // Accidental
    public bool ShowAccidental { get; init; }
    public Accidental Accidental { get; init; }

    // Articulation
    public Articulation Articulation { get; init; }

    // Hand coloring
    public Hand Hand { get; init; }

    // Chord notes (additional pitches, with their staff positions)
    public IReadOnlyList<(int StaffPosition, bool ShowAccidental, Accidental Accidental)> ChordPositions { get; init; } = [];

    // Lyrics (verse → text)
    public IReadOnlyList<(int Verse, string Text, LyricSyllable Syllable)> Lyrics { get; init; } = [];
}

/// <summary>Layout computation contract.</summary>
public interface ILayoutEngine
{
    LayoutResult ComputeLayout(Score score, LayoutOptions options);
    LayoutResult ComputePageLayout(Score score, LayoutOptions options, int pageNumber);
}

public sealed class LayoutOptions
{
    public double Zoom { get; set; } = 1.0;          // 0.25–8.0
    public double DPI { get; set; } = 96;
    public double StaffSpacingPx { get; set; } = 24; // space between staff lines
    public double SystemSpacingPx { get; set; } = 64;
    public double PageMarginPx { get; set; } = 60;
    public int MeasuresPerSystemHint { get; set; } = 0;  // 0 = auto
    public bool ForPrinting { get; set; }
}
