using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Core.Interfaces;

/// <summary>
/// AI-powered analysis and suggestion service.
/// Algorithmic methods (chord detection, fingering) work offline.
/// Claude-powered methods require a valid API key set via <see cref="Configure"/>.
/// </summary>
public interface IAIEngine
{
    /// <summary>
    /// Detects chords present in each measure of the given staff using algorithmic analysis.
    /// No network call — works offline.
    /// </summary>
    IReadOnlyList<HarmonyMeasure> DetectChords(Score score, Guid staffId);

    /// <summary>
    /// Suggests optimal finger assignments for every note in a staff.
    /// No network call — works offline.
    /// </summary>
    FingeringResult SuggestFingering(Score score, Guid staffId);

    /// <summary>
    /// Calls Claude to generate chord harmonisation suggestions for a melody staff.
    /// Requires a configured API key.
    /// </summary>
    Task<IReadOnlyList<HarmonyMeasure>> GetHarmonisationAsync(
        Score score, Guid melodyStaffId, CancellationToken ct = default);

    /// <summary>
    /// Calls Claude for a holistic analysis of the score's form, style, and harmonic language.
    /// Requires a configured API key.
    /// </summary>
    Task<AIAnalysisResult> GetScoreAnalysisAsync(Score score, CancellationToken ct = default);

    /// <summary>
    /// Calls Claude to build a step-by-step practice plan tailored to this score.
    /// Requires a configured API key.
    /// </summary>
    Task<PracticeRecommendation> GetPracticeRecommendationsAsync(Score score, CancellationToken ct = default);

    /// <summary>True when the Claude API key is configured.</summary>
    bool IsConfigured { get; }

    /// <summary>Set (or clear by passing null/empty) the Claude API key.</summary>
    void Configure(string apiKey);
}
