using Microsoft.Extensions.Logging;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.LayoutEngine;

/// <summary>
/// Computes pixel-level layout for a score.
/// Uses an automatic measure-spacing algorithm that distributes measures
/// across systems to achieve balanced line lengths.
/// </summary>
public sealed class LayoutEngine : ILayoutEngine
{
    private readonly ILogger<LayoutEngine> _logger;

    private const double StaffLineCount = 5;
    private const double LineSpacingDefault = 10.0; // px per staff space at 100% zoom

    public LayoutEngine(ILogger<LayoutEngine> logger) => _logger = logger;

    public LayoutResult ComputeLayout(Score score, LayoutOptions options)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var pages = new List<RenderedPage>();

        double pageW = score.PageWidthMm  * options.DPI / 25.4 * options.Zoom;
        double pageH = score.PageHeightMm * options.DPI / 25.4 * options.Zoom;
        double margin = options.PageMarginPx * options.Zoom;
        double usableW = pageW - 2 * margin;
        double lineSpacing = LineSpacingDefault * options.Zoom;

        var allMeasures = GetMeasureNumbers(score);
        if (allMeasures.Count == 0)
        {
            pages.Add(new RenderedPage
            {
                PageNumber = 1,
                WidthPx = pageW,
                HeightPx = pageH,
                Systems = []
            });
            return new LayoutResult { Pages = pages, ComputationTime = sw.Elapsed };
        }

        var systems = BuildSystems(score, allMeasures, usableW, lineSpacing, options);
        pages.AddRange(PaginateSystems(systems, pageW, pageH, margin, lineSpacing, score, options));

        sw.Stop();
        _logger.LogDebug("Layout computed: {Pages} pages, {Time}ms", pages.Count, sw.ElapsedMilliseconds);
        return new LayoutResult { Pages = pages, ComputationTime = sw.Elapsed };
    }

    public LayoutResult ComputePageLayout(Score score, LayoutOptions options, int pageNumber)
    {
        var full = ComputeLayout(score, options);
        var page = full.Pages.FirstOrDefault(p => p.PageNumber == pageNumber);
        return new LayoutResult
        {
            Pages = page is not null ? [page] : [],
            ComputationTime = full.ComputationTime
        };
    }

    // ── Private helpers ────────────────────────────────────────────────

    private static List<int> GetMeasureNumbers(Score score) =>
        score.Parts
             .SelectMany(p => p.Staves)
             .SelectMany(s => s.Measures)
             .Select(m => m.Number)
             .Distinct()
             .OrderBy(n => n)
             .ToList();

    private List<SystemLayout> BuildSystems(
        Score score, List<int> measures, double usableW,
        double lineSpacing, LayoutOptions options)
    {
        var systems = new List<SystemLayout>();
        double minMeasureW = lineSpacing * 8;  // minimum measure width
        int measuresPerSystem = options.MeasuresPerSystemHint > 0
            ? options.MeasuresPerSystemHint
            : Math.Max(1, (int)(usableW / (minMeasureW * 1.5)));

        int i = 0;
        while (i < measures.Count)
        {
            int end = Math.Min(i + measuresPerSystem, measures.Count);
            systems.Add(new SystemLayout
            {
                FirstMeasure = measures[i],
                LastMeasure  = measures[end - 1],
                MeasureNumbers = measures[i..end]
            });
            i = end;
        }
        return systems;
    }

    private static List<RenderedPage> PaginateSystems(
        List<SystemLayout> systems,
        double pageW, double pageH, double margin,
        double lineSpacing, Score score, LayoutOptions options)
    {
        var pages = new List<RenderedPage>();
        var currentPageSystems = new List<RenderedSystem>();
        double yPos = margin;
        int pageNumber = 1;

        int staffCount = score.Parts.Sum(p => p.Staves.Count);
        double systemHeight = staffCount * (StaffLineCount * lineSpacing + options.SystemSpacingPx * options.Zoom);

        foreach (var sysLayout in systems)
        {
            if (yPos + systemHeight > pageH - margin && currentPageSystems.Count > 0)
            {
                pages.Add(BuildPage(pageNumber++, pageW, pageH, currentPageSystems));
                currentPageSystems = [];
                yPos = margin;
            }

            var renderedSystem = BuildSystem(sysLayout, score, margin, yPos,
                pageW - 2 * margin, lineSpacing, options);
            currentPageSystems.Add(renderedSystem);
            yPos += systemHeight + options.SystemSpacingPx * options.Zoom;
        }

        if (currentPageSystems.Count > 0)
            pages.Add(BuildPage(pageNumber, pageW, pageH, currentPageSystems));

        return pages;
    }

    private static RenderedPage BuildPage(int number, double w, double h, List<RenderedSystem> systems) =>
        new() { PageNumber = number, WidthPx = w, HeightPx = h, Systems = systems };

    private static RenderedSystem BuildSystem(
        SystemLayout sysLayout, Score score,
        double x, double y, double width,
        double lineSpacing, LayoutOptions options)
    {
        var staves = new List<RenderedStaff>();
        double staffY = y;

        foreach (var part in score.Parts)
        foreach (var staff in part.Staves)
        {
            double staffHeight = StaffLineCount * lineSpacing;
            staves.Add(BuildStaff(staff, sysLayout, x, staffY, width, lineSpacing));
            staffY += staffHeight + options.SystemSpacingPx * options.Zoom * 0.4;
        }

        return new RenderedSystem
        {
            X = x, Y = y, Width = width,
            Height = staffY - y,
            FirstMeasure = sysLayout.FirstMeasure,
            LastMeasure  = sysLayout.LastMeasure,
            Staves = staves
        };
    }

    private static RenderedStaff BuildStaff(
        Staff staff, SystemLayout sysLayout,
        double x, double y, double width, double lineSpacing)
    {
        var measures = new List<RenderedMeasure>();
        double measureCount = sysLayout.MeasureNumbers.Count;
        double measureW = width / measureCount;
        double measureX = x;

        foreach (int mNum in sysLayout.MeasureNumbers)
        {
            var measure = staff.GetMeasure(mNum);
            var elements = new List<RenderedNoteElement>();

            if (measure is not null)
            {
                double noteAreaW = measureW - 16; // leave room for barline
                double noteX = measureX + 16;     // offset for clef/time sig on first measure

                if (measure.Notes.Count > 0)
                {
                    double noteSpacing = noteAreaW / (measure.Notes.Count + 1);
                    for (int i = 0; i < measure.Notes.Count; i++)
                    {
                        var note = measure.Notes[i];
                        double nx = noteX + noteSpacing * (i + 1);
                        double ny = y + NoteYOffset(note.StaffPosition, lineSpacing);
                        var (lc, above) = note.IsRest ? (0, false)
                            : (Math.Abs(note.StaffPosition) > 9 || note.StaffPosition < 1
                                ? Math.Max(0, (note.StaffPosition < 1
                                    ? Math.Abs(note.StaffPosition)
                                    : (note.StaffPosition - 9 + 1) / 2))
                                : 0, note.StaffPosition > 9);

                        elements.Add(new RenderedNoteElement
                        {
                            NoteId = note.Id,
                            X = nx, Y = ny,
                            StaffPosition = note.StaffPosition,
                            NeedsLedgerLines = lc > 0,
                            LedgerLineCount = lc
                        });
                    }
                }
            }

            measures.Add(new RenderedMeasure
            {
                MeasureNumber = mNum,
                X = measureX,
                Width = measureW,
                Elements = elements
            });

            measureX += measureW;
        }

        return new RenderedStaff
        {
            StaffId = staff.Id,
            Y = y,
            Height = StaffLineCount * lineSpacing,
            Measures = measures
        };
    }

    private static double NoteYOffset(int staffPosition, double lineSpacing) =>
        // staffPosition 1 = bottom line, increases up
        (9 - staffPosition) * (lineSpacing / 2.0);

    private sealed class SystemLayout
    {
        public int FirstMeasure { get; init; }
        public int LastMeasure { get; init; }
        public List<int> MeasureNumbers { get; init; } = [];
    }
}
