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
    public IReadOnlyList<RenderedNoteElement> Elements { get; init; } = [];
}

/// <summary>A note/rest/chord element with its absolute render position.</summary>
public sealed class RenderedNoteElement
{
    public Guid NoteId { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public int StaffPosition { get; init; }
    public bool NeedsLedgerLines { get; init; }
    public int LedgerLineCount { get; init; }
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
