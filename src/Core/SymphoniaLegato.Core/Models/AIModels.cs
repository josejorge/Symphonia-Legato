namespace SymphoniaLegato.Core.Models;

// ── Chord detection ─────────────────────────────────────────────────────────

/// <summary>Quality of a detected chord.</summary>
public enum ChordQuality
{
    Major, Minor, Diminished, Augmented,
    DominantSeventh, MajorSeventh, MinorSeventh,
    HalfDiminishedSeventh, DiminishedSeventh,
    Suspended2, Suspended4, Unknown
}

/// <summary>Chord detected from a cluster of simultaneous pitches.</summary>
public sealed class ChordLabel
{
    /// <summary>Root note name (C, D, E, F, G, A, B).</summary>
    public string Root { get; init; } = string.Empty;

    /// <summary>Accidental on root: empty, "#", or "b".</summary>
    public string RootAccidental { get; init; } = string.Empty;

    /// <summary>Chord quality.</summary>
    public ChordQuality Quality { get; init; }

    /// <summary>Bass note when not in root position (e.g. "E" for C/E).</summary>
    public string? BassNote { get; init; }

    /// <summary>Roman-numeral analysis relative to the score key (e.g. "IV", "V7", "ii°").</summary>
    public string RomanNumeral { get; init; } = string.Empty;

    /// <summary>Compact display name (e.g. "Cmaj", "Dm7", "G7").</summary>
    public string DisplayName =>
        $"{Root}{RootAccidental}{QualitySuffix}{(BassNote is null ? "" : $"/{BassNote}")}";

    private string QualitySuffix => Quality switch
    {
        ChordQuality.Major                   => "",
        ChordQuality.Minor                   => "m",
        ChordQuality.Diminished              => "°",
        ChordQuality.Augmented               => "+",
        ChordQuality.DominantSeventh         => "7",
        ChordQuality.MajorSeventh            => "maj7",
        ChordQuality.MinorSeventh            => "m7",
        ChordQuality.HalfDiminishedSeventh   => "ø7",
        ChordQuality.DiminishedSeventh       => "°7",
        ChordQuality.Suspended2              => "sus2",
        ChordQuality.Suspended4              => "sus4",
        _                                    => "?"
    };
}

/// <summary>Chord suggestion for one measure.</summary>
public sealed class HarmonyMeasure
{
    /// <summary>Zero-based measure index.</summary>
    public int MeasureNumber { get; init; }

    /// <summary>Suggested chord labels ordered by beat.</summary>
    public IReadOnlyList<ChordLabel> Chords { get; init; } = [];
}

// ── Fingering ────────────────────────────────────────────────────────────────

/// <summary>Finger number (1 = thumb, 5 = pinky).</summary>
public enum Finger { Thumb = 1, Index = 2, Middle = 3, Ring = 4, Pinky = 5 }

/// <summary>Suggested finger assignment for one note.</summary>
public sealed class FingeringNote
{
    /// <summary>ID of the <see cref="Note"/> this applies to.</summary>
    public Guid NoteId { get; init; }

    /// <summary>Suggested finger.</summary>
    public Finger Finger { get; init; }

    /// <summary>True if a thumb-under or finger-over crossing happens before this note.</summary>
    public bool IsCrossing { get; init; }
}

/// <summary>Complete fingering suggestion for one staff/hand.</summary>
public sealed class FingeringResult
{
    /// <summary>Staff this result applies to.</summary>
    public Guid StaffId { get; init; }

    /// <summary>Per-note finger assignments in score order.</summary>
    public IReadOnlyList<FingeringNote> Notes { get; init; } = [];
}

// ── Score analysis / practice ────────────────────────────────────────────────

/// <summary>Holistic AI analysis of a score.</summary>
public sealed class AIAnalysisResult
{
    /// <summary>Detected or confirmed key (e.g. "C major", "A minor").</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>Estimated musical form (e.g. "ABA", "Rondo", "Theme and Variations").</summary>
    public string Form { get; init; } = string.Empty;

    /// <summary>Estimated difficulty level (Beginner / Intermediate / Advanced / Expert).</summary>
    public string DifficultyLevel { get; init; } = string.Empty;

    /// <summary>Stylistic period or genre (e.g. "Baroque", "Classical", "Jazz").</summary>
    public string Style { get; init; } = string.Empty;

    /// <summary>Free-form analysis text (key relationships, motives, harmonic patterns).</summary>
    public string AnalysisText { get; init; } = string.Empty;

    /// <summary>Notable technical challenges identified.</summary>
    public IReadOnlyList<string> TechnicalChallenges { get; init; } = [];
}

/// <summary>AI-generated practice plan for a score.</summary>
public sealed class PracticeRecommendation
{
    /// <summary>Suggested starting tempo (BPM) for slow practice.</summary>
    public int StartingTempoBpm { get; init; }

    /// <summary>Ordered practice steps or strategies.</summary>
    public IReadOnlyList<string> Steps { get; init; } = [];

    /// <summary>Specific focus areas (e.g. "Right-hand passagework bars 12-16").</summary>
    public IReadOnlyList<string> FocusAreas { get; init; } = [];

    /// <summary>Estimated practice sessions to reach performance tempo.</summary>
    public int EstimatedSessions { get; init; }
}
