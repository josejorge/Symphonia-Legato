using Microsoft.Extensions.Logging;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.LayoutEngine;

/// <summary>
/// Proportional layout engine.
/// Computes pixel positions for every notational element including
/// noteheads, stems, beams, accidentals, dynamics, slurs, and lyrics.
/// Uses proportional spacing: longer notes get more horizontal space.
/// </summary>
public sealed class LayoutEngine : ILayoutEngine
{
    private readonly ILogger<LayoutEngine> _logger;

    private const double StaffLines     = 5.0;
    private const double LineSpacing    = 10.0;   // px per space at zoom=1
    private const double StemLength     = 3.5;    // staff spaces
    private const double ClefWidth      = 24.0;
    private const double TimeSigWidth   = 14.0;
    private const double KeySigWidth    = 10.0;   // per accidental
    private const double NoteHeadRx     = 0.55;   // note head x-radius in spaces
    private const double NoteHeadRy     = 0.40;   // note head y-radius in spaces
    private const double AccidentalW    = 8.0;
    private const double MinNoteW       = 12.0;
    private const double BeamThickness  = 3.0;

    public LayoutEngine(ILogger<LayoutEngine> logger) => _logger = logger;

    public LayoutResult ComputeLayout(Score score, LayoutOptions options)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        double sp = LineSpacing * options.Zoom; // staff space in px
        double pageW = score.PageWidthMm  * options.DPI / 25.4 * options.Zoom;
        double pageH = score.PageHeightMm * options.DPI / 25.4 * options.Zoom;
        double margin = options.PageMarginPx * options.Zoom;
        double usableW = pageW - 2 * margin;

        var allMeasureNumbers = GetAllMeasureNumbers(score);
        var systems = BuildSystems(score, allMeasureNumbers, usableW, sp, options);
        var pages   = Paginate(systems, score, pageW, pageH, margin, sp, options);

        sw.Stop();
        _logger.LogDebug("Layout: {Pages} pages in {Ms}ms", pages.Count, sw.ElapsedMilliseconds);
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

    // ── Systems ───────────────────────────────────────────────────────

    private static List<int> GetAllMeasureNumbers(Score score) =>
        score.Parts.SelectMany(p => p.Staves)
             .SelectMany(s => s.Measures)
             .Select(m => m.Number)
             .Distinct().OrderBy(n => n).ToList();

    private List<SystemLayout> BuildSystems(Score score, List<int> measures,
        double usableW, double sp, LayoutOptions options)
    {
        if (measures.Count == 0) return [];

        int hint = options.MeasuresPerSystemHint;
        if (hint > 0)
        {
            return measures.Chunk(hint)
                .Select(chunk => new SystemLayout { MeasureNumbers = [.. chunk] })
                .ToList();
        }

        // Proportional layout: distribute measures until line is full
        var systems = new List<SystemLayout>();
        var currentSystem = new List<int>();
        double currentW = 0;

        foreach (int mNum in measures)
        {
            double mW = EstimateMeasureWidth(score, mNum, sp,
                isFirst: currentSystem.Count == 0);
            if (currentW + mW > usableW && currentSystem.Count > 0)
            {
                systems.Add(new SystemLayout { MeasureNumbers = [.. currentSystem] });
                currentSystem.Clear();
                currentW = 0;
            }
            currentSystem.Add(mNum);
            currentW += mW;
        }
        if (currentSystem.Count > 0)
            systems.Add(new SystemLayout { MeasureNumbers = [.. currentSystem] });

        return systems;
    }

    private static double EstimateMeasureWidth(Score score, int measureNumber, double sp, bool isFirst)
    {
        var staff = score.Parts.SelectMany(p => p.Staves).FirstOrDefault();
        var measure = staff?.GetMeasure(measureNumber);
        if (measure is null) return sp * 8;

        double accW  = isFirst ? ClefWidth * sp / LineSpacing : 0;
        double ksW   = isFirst ? Math.Abs(score.InitialKeySignature.Fifths) * KeySigWidth * sp / LineSpacing : 0;
        double tsW   = isFirst ? TimeSigWidth * sp / LineSpacing : 0;

        // Proportional: each duration takes proportional space
        double noteW = measure.Notes.Sum(n => NoteWidthPx(n.Duration, sp));
        noteW = Math.Max(noteW, MinNoteW * sp / LineSpacing);

        return accW + ksW + tsW + noteW + sp; // + barline padding
    }

    private static double NoteWidthPx(Duration d, double sp)
    {
        // Whole gets more space, shorter notes less
        double quarters = (double)d.Ticks / 1024.0;
        return Math.Max(MinNoteW, quarters * sp * 2.5);
    }

    // ── Pagination ────────────────────────────────────────────────────

    private List<RenderedPage> Paginate(List<SystemLayout> systems, Score score,
        double pageW, double pageH, double margin, double sp, LayoutOptions options)
    {
        var pages = new List<RenderedPage>();
        var currentSystems = new List<RenderedSystem>();

        int staffCount = score.Parts.Sum(p => p.Staves.Count);
        double staffH = (StaffLines - 1) * sp;
        double partH  = staffH + sp * 2;  // staff + inter-staff gap
        double sysH   = staffCount * partH + options.SystemSpacingPx * options.Zoom;

        double yPos = margin;
        int pageNum = 1;

        foreach (var sysLayout in systems)
        {
            if (yPos + sysH > pageH - margin && currentSystems.Count > 0)
            {
                pages.Add(new RenderedPage
                {
                    PageNumber = pageNum++, WidthPx = pageW, HeightPx = pageH,
                    Systems = currentSystems
                });
                currentSystems = [];
                yPos = margin;
            }

            var rsys = BuildSystem(sysLayout, score, margin, yPos,
                pageW - 2 * margin, sp, options, staffH, partH);
            currentSystems.Add(rsys);
            yPos += sysH;
        }

        if (currentSystems.Count > 0)
            pages.Add(new RenderedPage
            {
                PageNumber = pageNum, WidthPx = pageW, HeightPx = pageH,
                Systems = currentSystems
            });

        return pages;
    }

    // ── System ────────────────────────────────────────────────────────

    private RenderedSystem BuildSystem(SystemLayout sysLayout, Score score,
        double x, double y, double usableW, double sp,
        LayoutOptions options, double staffH, double partH)
    {
        var rStaves = new List<RenderedStaff>();
        double staffY = y;

        // Compute measure widths for this system (stretch to fill usableW)
        var mWidths = ComputeMeasureWidths(sysLayout.MeasureNumbers, score, sp, usableW);

        foreach (var part in score.Parts)
        foreach (var staff in part.Staves)
        {
            var rStaff = BuildStaff(staff, sysLayout.MeasureNumbers, mWidths,
                x, staffY, sp, score);
            rStaves.Add(rStaff);
            staffY += partH;
        }

        return new RenderedSystem
        {
            X = x, Y = y, Width = usableW,
            Height = staffY - y,
            FirstMeasure = sysLayout.MeasureNumbers[0],
            LastMeasure  = sysLayout.MeasureNumbers[^1],
            Staves = rStaves
        };
    }

    private Dictionary<int, double> ComputeMeasureWidths(List<int> measureNumbers,
        Score score, double sp, double usableW)
    {
        bool isFirst = true;
        var rawWidths = new Dictionary<int, double>();
        double totalRaw = 0;
        foreach (int mNum in measureNumbers)
        {
            double w = EstimateMeasureWidth(score, mNum, sp, isFirst);
            rawWidths[mNum] = w;
            totalRaw += w;
            isFirst = false;
        }

        // Stretch to fill usable width
        double scale = totalRaw > 0 ? usableW / totalRaw : 1;
        var result = new Dictionary<int, double>();
        foreach (var kvp in rawWidths)
            result[kvp.Key] = kvp.Value * scale;
        return result;
    }

    // ── Staff ─────────────────────────────────────────────────────────

    private RenderedStaff BuildStaff(Staff staff, List<int> measureNumbers,
        Dictionary<int, double> mWidths, double startX, double y, double sp, Score score)
    {
        var rMeasures = new List<RenderedMeasure>();
        double x = startX;
        bool isFirst = true;

        KeySignature currentKey = score.InitialKeySignature;
        TimeSignature currentTs = score.InitialTimeSignature;
        Clef currentClef = staff.DefaultClef;

        foreach (int mNum in measureNumbers)
        {
            double w = mWidths.TryGetValue(mNum, out var mw) ? mw : sp * 8;
            var measure = staff.GetMeasure(mNum);

            if (measure?.KeySignatureChange.HasValue == true) currentKey = measure.KeySignatureChange.Value;
            if (measure?.ClefChange.HasValue == true)         currentClef = measure.ClefChange.Value;
            TimeSignature ts = measure?.TimeSignature ?? currentTs;

            var rMeasure = BuildMeasure(measure, mNum, x, y, w, sp,
                currentClef, currentKey, ts, isFirst, staff.Id);
            rMeasures.Add(rMeasure);

            x += w;
            isFirst = false;
            currentTs = ts;
        }

        return new RenderedStaff
        {
            StaffId = staff.Id,
            Y = y,
            Height = (StaffLines - 1) * sp,
            Measures = rMeasures
        };
    }

    // ── Measure ───────────────────────────────────────────────────────

    private RenderedMeasure BuildMeasure(Measure? measure, int mNum,
        double x, double y, double w, double sp,
        Clef clef, KeySignature key, TimeSignature ts,
        bool isFirst, Guid staffId)
    {
        // Header decorations: clef, key sig, time sig
        double headerX = x + 2;
        if (isFirst)
        {
            headerX += ClefWidth  * sp / LineSpacing;
            headerX += Math.Abs(key.Fifths) * KeySigWidth * sp / LineSpacing;
            headerX += TimeSigWidth * sp / LineSpacing;
        }

        var elements = new List<RenderedNoteElement>();
        var beams    = new List<RenderedBeam>();
        var hairpins = new List<RenderedHairpin>();
        var slurs    = new List<RenderedSlur>();
        var dynamics = new List<RenderedDynamic>();
        var tempos   = new List<RenderedTempo>();

        if (measure is not null)
        {
            // Note positions (proportional within measure)
            double noteAreaW = x + w - headerX - 4;
            int totalTicks = measure.TimeSignature.TicksPerMeasure;
            double tickWidth = totalTicks > 0 ? noteAreaW / totalTicks : noteAreaW;

            // Place notes
            var beamGroups = new Dictionary<int, List<(RenderedNoteElement elem, double stemX)>>();

            foreach (var note in measure.Notes.OrderBy(n => n.TickOffset))
            {
                double nx = headerX + note.TickOffset * tickWidth + tickWidth * 0.5;

                int pos    = note.StaffPosition;
                double ny  = y + NoteY(pos, sp);
                var (lc, above) = LedgerLines(pos);

                StemDirection stemDir = note.Stem == StemDirection.Auto
                    ? (pos >= 5 ? StemDirection.Down : StemDirection.Up)
                    : note.Stem;

                bool stemNone = note.Duration.Value == NoteValue.Whole || note.IsRest;
                double stemEndY = stemNone ? ny : (stemDir == StemDirection.Up
                    ? ny - StemLength * sp
                    : ny + StemLength * sp);

                var elem = new RenderedNoteElement
                {
                    NoteId          = note.Id,
                    X               = nx, Y = ny,
                    StaffPosition   = pos,
                    IsRest          = note.IsRest,
                    NeedsLedgerLines = lc > 0,
                    LedgerLineCount  = lc,
                    LedgerLinesAbove = above,
                    NoteValue        = note.Duration.Value,
                    Dots             = note.Duration.Dots,
                    Stem             = stemNone ? StemDirection.None : stemDir,
                    StemEndY         = stemEndY,
                    BeamGroup        = note.BeamGroup,
                    IsBeamStart      = note.IsBeamStart,
                    IsBeamEnd        = note.IsBeamEnd,
                    ShowAccidental   = note.ShowAccidental,
                    Accidental       = note.Pitch?.Accidental ?? Accidental.Natural,
                    Articulation     = note.Articulation,
                    Hand             = note.Hand,
                    Lyrics           = note.Lyrics
                        .Select(l => (l.Verse, l.Text, l.Syllable)).ToList(),
                    ChordPositions   = note.ChordNotes
                        .Select(cp => (PitchToStaffPosition(cp, clef), false, cp.Accidental))
                        .ToList()
                };
                elements.Add(elem);

                // Collect beam groups
                if (note.BeamGroup > 0)
                {
                    if (!beamGroups.TryGetValue(note.BeamGroup, out var grp))
                    {
                        grp = new List<(RenderedNoteElement, double)>();
                        beamGroups[note.BeamGroup] = grp;
                    }
                    grp.Add((elem, nx));
                }
            }

            // Build beams
            foreach (var (_, grp) in beamGroups)
            {
                if (grp.Count < 2) continue;
                var first = grp[0];
                var last  = grp[^1];
                double beamY = first.elem.StemEndY; // use first note's stem tip
                beams.Add(new RenderedBeam
                {
                    StartX = first.stemX, StartY = beamY,
                    EndX   = last.stemX,  EndY   = last.elem.StemEndY,
                    BeamLevel = 0
                });
                // Sixteenth sub-beams
                if (grp[0].elem.NoteValue == NoteValue.Sixteenth)
                {
                    beams.Add(new RenderedBeam
                    {
                        StartX = first.stemX, StartY = beamY + (first.elem.Stem == StemDirection.Up ? BeamThickness * 2 : -BeamThickness * 2),
                        EndX   = last.stemX,  EndY   = last.elem.StemEndY + (last.elem.Stem == StemDirection.Up ? BeamThickness * 2 : -BeamThickness * 2),
                        BeamLevel = 1
                    });
                }
            }

            // Dynamics
            double bottomY = y + (StaffLines - 1) * sp + sp * 1.5;
            foreach (var dyn in measure.Dynamics)
            {
                double dx = headerX + dyn.TickOffset * tickWidth;
                dynamics.Add(new RenderedDynamic { Symbol = dyn.Symbol, X = dx, Y = bottomY });
            }

            // Hairpins
            foreach (var hp in measure.Hairpins)
            {
                double hx1 = headerX + hp.StartTick * tickWidth;
                double hx2 = headerX + hp.EndTick   * tickWidth;
                hairpins.Add(new RenderedHairpin
                {
                    StartX = hx1, EndX = hx2,
                    Y = bottomY + sp,
                    Type = hp.Type
                });
            }

            // Slurs
            foreach (var slur in measure.Slurs)
            {
                var startElem = elements.FirstOrDefault(e => e.NoteId == slur.StartNoteId);
                var endElem   = elements.FirstOrDefault(e => e.NoteId == slur.EndNoteId);
                if (startElem is null || endElem is null) continue;

                bool curvesUp = slur.Direction == CurveDirection.Down ||
                               (slur.Direction == CurveDirection.Auto &&
                                startElem.Stem == StemDirection.Up);
                double curveY = curvesUp ? startElem.Y - sp * 1.5 : startElem.Y + sp * 1.5;
                slurs.Add(new RenderedSlur
                {
                    StartX = startElem.X, StartY = startElem.Y,
                    EndX   = endElem.X,   EndY   = endElem.Y,
                    Cp1X   = startElem.X + (endElem.X - startElem.X) * 0.25, Cp1Y = curveY,
                    Cp2X   = startElem.X + (endElem.X - startElem.X) * 0.75, Cp2Y = curveY,
                    CurvesUp = curvesUp
                });
            }

            // Tempo markings
            foreach (var tempo in measure.TempoMarkings)
            {
                tempos.Add(new RenderedTempo
                {
                    Text = tempo.Text, BPM = tempo.BPM,
                    X = x + 2, Y = y - sp * 1.8
                });
            }
        }

        return new RenderedMeasure
        {
            MeasureNumber     = mNum,
            X = x, Width = w,
            StartBarline      = measure?.StartBarline ?? BarlineType.Single,
            EndBarline        = measure?.EndBarline   ?? BarlineType.Single,
            ShowClef          = isFirst,
            ShowTimeSignature = isFirst,
            ShowKeySignature  = isFirst && key.Fifths != 0,
            TimeSignature     = measure?.TimeSignature ?? ts,
            KeySignature      = key,
            ClefType          = clef.Type,
            Elements          = elements,
            Beams             = beams,
            Hairpins          = hairpins,
            Slurs             = slurs,
            Dynamics          = dynamics,
            TempoMarkings     = tempos
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static double NoteY(int staffPosition, double sp) =>
        (9 - staffPosition) * (sp / 2.0);

    private static (int count, bool above) LedgerLines(int pos)
    {
        if (pos <= 0)  return ((Math.Abs(pos) + 1) / 2, false);
        if (pos > 9)   return ((pos - 9 + 1) / 2, true);
        return (0, false);
    }

    /// <summary>
    /// Converts a pitch to a staff position (1 = bottom line, 9 = top line)
    /// for the given clef, using diatonic distance from the clef's bottom-line reference note.
    /// </summary>
    private static int PitchToStaffPosition(Pitch pitch, Clef clef)
    {
        var bottomRef = Pitch.FromMidi(clef.BottomLineMidi);
        int diatonicSteps = 7 * (pitch.Octave - bottomRef.Octave)
                          + (int)pitch.Name - (int)bottomRef.Name;
        return 1 + diatonicSteps;
    }

    private sealed class SystemLayout
    {
        public List<int> MeasureNumbers { get; init; } = [];
    }
}
