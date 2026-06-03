using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.AIEngine;

/// <summary>
/// Identifies chord labels from sets of simultaneously sounding pitches.
/// Pure algorithmic analysis — no network calls.
/// </summary>
public static class ChordDetector
{
    private static readonly string[] NoteNames =
        ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    // Root pitch class (0=C … 11=B) for each Fifths value from -7 to +7.
    // Index = Fifths + 7, so index 7 = C major = 0.
    private static readonly int[] FifthsToPitchClass =
        [11, 6, 1, 8, 3, 10, 5, 0, 7, 2, 9, 4, 11, 6, 1];

    /// <summary>
    /// Detects chords for each measure in the given staff using algorithmic analysis.
    /// Groups notes by onset tick, derives pitch-class sets, and pattern-matches against
    /// known chord interval structures.
    /// </summary>
    public static IReadOnlyList<HarmonyMeasure> DetectForStaff(Score score, Guid staffId)
    {
        var staff = score.Parts.SelectMany(p => p.Staves).FirstOrDefault(s => s.Id == staffId);
        if (staff is null) return [];

        var keySig = score.InitialKeySignature;
        var result = new List<HarmonyMeasure>();

        for (int i = 0; i < staff.Measures.Count; i++)
        {
            var measure = staff.Measures[i];
            // Override key sig if this measure has a change
            var effectiveKey = measure.KeySignatureChange ?? keySig;
            var chords = DetectInMeasure(measure, effectiveKey);
            result.Add(new HarmonyMeasure { MeasureNumber = i, Chords = chords });
        }

        return result;
    }

    private static IReadOnlyList<ChordLabel> DetectInMeasure(Measure measure, KeySignature keySig)
    {
        // Group all sounding pitches by onset tick
        var byTick = measure.Notes
            .Where(n => n.Pitch is not null)
            .GroupBy(n => n.TickOffset)
            .OrderBy(g => g.Key)
            .ToList();

        if (byTick.Count == 0) return [];

        // One representative chord per beat onset
        var chords = new List<ChordLabel>();
        foreach (var group in byTick)
        {
            var pitchClasses = group
                .SelectMany(n => n.ChordNotes.Prepend(n.Pitch!.Value))
                .Select(p => p.MidiNumber % 12)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var label = IdentifyChord(pitchClasses, keySig);
            if (label is not null) chords.Add(label);
        }

        return chords;
    }

    public static ChordLabel? IdentifyChord(List<int> pitchClasses, KeySignature keySig)
    {
        if (pitchClasses.Count == 0) return null;

        ChordLabel? best = null;
        int bestScore = -1;

        foreach (int root in pitchClasses)
        {
            var intervals = pitchClasses
                .Select(p => (p - root + 12) % 12)
                .OrderBy(i => i)
                .ToList();

            var (quality, score) = MatchQuality(intervals);
            if (score > bestScore)
            {
                bestScore = score;
                int bassPc = pitchClasses.Min();
                best = BuildLabel(root, quality, bassPc, keySig);
            }
        }

        return best;
    }

    private static (ChordQuality quality, int score) MatchQuality(List<int> intervals)
    {
        if (Contains(intervals, 0, 4, 7) && intervals.Count <= 3) return (ChordQuality.Major, 10);
        if (Contains(intervals, 0, 3, 7) && intervals.Count <= 3) return (ChordQuality.Minor, 10);
        if (Contains(intervals, 0, 4, 7, 10))                     return (ChordQuality.DominantSeventh, 10);
        if (Contains(intervals, 0, 4, 7, 11))                     return (ChordQuality.MajorSeventh, 10);
        if (Contains(intervals, 0, 3, 7, 10))                     return (ChordQuality.MinorSeventh, 10);
        if (Contains(intervals, 0, 3, 6, 10))                     return (ChordQuality.HalfDiminishedSeventh, 9);
        if (Contains(intervals, 0, 3, 6, 9))                      return (ChordQuality.DiminishedSeventh, 9);
        if (Contains(intervals, 0, 3, 6) && intervals.Count <= 3) return (ChordQuality.Diminished, 9);
        if (Contains(intervals, 0, 4, 8) && intervals.Count <= 3) return (ChordQuality.Augmented, 9);
        if (Contains(intervals, 0, 2, 7))                         return (ChordQuality.Suspended2, 7);
        if (Contains(intervals, 0, 5, 7))                         return (ChordQuality.Suspended4, 7);
        if (intervals.Count == 1)                                  return (ChordQuality.Major, 1);
        return (ChordQuality.Unknown, 0);
    }

    private static bool Contains(List<int> set, params int[] required) =>
        required.All(set.Contains);

    private static ChordLabel BuildLabel(int rootPc, ChordQuality quality, int bassPc, KeySignature keySig)
    {
        string rootName = NoteNames[rootPc];
        string bassName = NoteNames[bassPc % 12];
        bool hasInversion = bassPc % 12 != rootPc;

        return new ChordLabel
        {
            Root = rootName.TrimEnd('#'),
            RootAccidental = rootName.EndsWith('#') ? "#" : "",
            Quality = quality,
            BassNote = hasInversion ? bassName : null,
            RomanNumeral = ToRomanNumeral(rootPc, quality, keySig)
        };
    }

    private static string ToRomanNumeral(int rootPc, ChordQuality quality, KeySignature keySig)
    {
        // Derive key root from Fifths value (clamped to valid range -7..7)
        int fifths = Math.Clamp(keySig.Fifths, -7, 7);
        int keyRoot = FifthsToPitchClass[fifths + 7];

        int[] majorScale = [0, 2, 4, 5, 7, 9, 11];
        int degree = (rootPc - keyRoot + 12) % 12;
        int scaleStep = Array.IndexOf(majorScale, degree);
        if (scaleStep < 0) return "?";

        string[] romans = ["I", "II", "III", "IV", "V", "VI", "VII"];
        string numeral = romans[scaleStep];

        return quality switch
        {
            ChordQuality.Minor                   => numeral.ToLower(),
            ChordQuality.Diminished              => numeral.ToLower() + "°",
            ChordQuality.Augmented               => numeral + "+",
            ChordQuality.DominantSeventh         => numeral + "7",
            ChordQuality.MajorSeventh            => numeral + "maj7",
            ChordQuality.MinorSeventh            => numeral.ToLower() + "7",
            ChordQuality.HalfDiminishedSeventh   => numeral.ToLower() + "ø7",
            ChordQuality.DiminishedSeventh       => numeral.ToLower() + "°7",
            _                                    => numeral
        };
    }
}
