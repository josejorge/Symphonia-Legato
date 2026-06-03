using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.AIEngine;

/// <summary>
/// Implementation of <see cref="IAIEngine"/> that uses algorithmic analysis for offline
/// features (chord detection, fingering) and the Claude API for AI-powered features
/// (harmonisation, score analysis, practice recommendations).
/// </summary>
public sealed class ClaudeAIEngine : IAIEngine
{
    private readonly HttpClient _http;
    private string _apiKey = string.Empty;

    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string ModelId = "claude-sonnet-4-6";
    private const string ApiVersion = "2023-06-01";

    public ClaudeAIEngine(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    // ── Configuration ────────────────────────────────────────────────────

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public void Configure(string apiKey) => _apiKey = apiKey?.Trim() ?? string.Empty;

    // ── Algorithmic (offline) ────────────────────────────────────────────

    public IReadOnlyList<HarmonyMeasure> DetectChords(Score score, Guid staffId) =>
        ChordDetector.DetectForStaff(score, staffId);

    public FingeringResult SuggestFingering(Score score, Guid staffId) =>
        FingeringAdvisor.SuggestForStaff(score, staffId);

    // ── Claude-powered ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<HarmonyMeasure>> GetHarmonisationAsync(
        Score score, Guid melodyStaffId, CancellationToken ct = default)
    {
        EnsureConfigured();
        string melody = ScoreSerializer.MelodyToText(score, melodyStaffId);
        string keySig = KeySigName(score.InitialKeySignature);

        string prompt = $$"""
            You are an expert music theory assistant.
            Analyse this melody in {{keySig}} and suggest chords for each measure.
            Melody (one note per tick offset, format: Pitch:TickOffset):
            {{melody}}

            Respond in this exact JSON format only — no prose, no markdown:
            {
              "measures": [
                { "measureNumber": 0, "chords": ["Cmaj", "G7"] }
              ]
            }
            Use standard chord symbols (Cmaj, Dm, G7, Am7, etc.).
            """;

        string json = await SendAsync(prompt, ct);
        return ParseHarmonisation(json, score.InitialKeySignature);
    }

    public async Task<AIAnalysisResult> GetScoreAnalysisAsync(Score score, CancellationToken ct = default)
    {
        EnsureConfigured();
        string summary = ScoreSerializer.ScoreToText(score);

        string prompt = $$"""
            You are an expert music analyst. Analyse this score and respond in JSON only:
            {{summary}}

            Respond in this exact JSON format:
            {
              "key": "C major",
              "form": "ABA",
              "difficultyLevel": "Intermediate",
              "style": "Classical",
              "analysisText": "...",
              "technicalChallenges": ["...", "..."]
            }
            """;

        string json = await SendAsync(prompt, ct);
        return ParseAnalysis(json);
    }

    public async Task<PracticeRecommendation> GetPracticeRecommendationsAsync(
        Score score, CancellationToken ct = default)
    {
        EnsureConfigured();
        string summary = ScoreSerializer.ScoreToText(score);

        string prompt = $$"""
            You are an expert piano teacher. Create a practice plan for this score:
            {{summary}}

            Respond in this exact JSON format:
            {
              "startingTempoBpm": 60,
              "steps": ["...", "..."],
              "focusAreas": ["...", "..."],
              "estimatedSessions": 10
            }
            """;

        string json = await SendAsync(prompt, ct);
        return ParsePractice(json);
    }

    // ── HTTP ─────────────────────────────────────────────────────────────

    private async Task<string> SendAsync(string userPrompt, CancellationToken ct)
    {
        var body = new
        {
            model = ModelId,
            max_tokens = 2048,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        string bodyJson = JsonSerializer.Serialize(body);
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", ApiVersion);
        request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, ct);
        string responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Claude API error {(int)response.StatusCode}: {responseBody}");

        // Parse the Claude response envelope and extract the text content
        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        return content;
    }

    // ── Response parsers ─────────────────────────────────────────────────

    private static IReadOnlyList<HarmonyMeasure> ParseHarmonisation(string json, KeySignature keySig)
    {
        try
        {
            json = ExtractJson(json);
            using var doc = JsonDocument.Parse(json);
            var measures = doc.RootElement.GetProperty("measures");
            var result = new List<HarmonyMeasure>();

            foreach (var m in measures.EnumerateArray())
            {
                int num = m.GetProperty("measureNumber").GetInt32();
                var chords = new List<ChordLabel>();
                foreach (var chord in m.GetProperty("chords").EnumerateArray())
                {
                    string symbol = chord.GetString() ?? "";
                    var label = ParseChordSymbol(symbol, keySig);
                    if (label is not null) chords.Add(label);
                }
                result.Add(new HarmonyMeasure { MeasureNumber = num, Chords = chords });
            }
            return result;
        }
        catch
        {
            return [];
        }
    }

    private static AIAnalysisResult ParseAnalysis(string json)
    {
        try
        {
            json = ExtractJson(json);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var challenges = root.TryGetProperty("technicalChallenges", out var tc)
                ? tc.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                : (List<string>)[];

            return new AIAnalysisResult
            {
                Key             = root.TryGetProperty("key",             out var k)  ? k.GetString()  ?? "" : "",
                Form            = root.TryGetProperty("form",            out var f)  ? f.GetString()  ?? "" : "",
                DifficultyLevel = root.TryGetProperty("difficultyLevel", out var d)  ? d.GetString()  ?? "" : "",
                Style           = root.TryGetProperty("style",           out var s)  ? s.GetString()  ?? "" : "",
                AnalysisText    = root.TryGetProperty("analysisText",    out var at) ? at.GetString() ?? "" : "",
                TechnicalChallenges = challenges
            };
        }
        catch
        {
            return new AIAnalysisResult { AnalysisText = json };
        }
    }

    private static PracticeRecommendation ParsePractice(string json)
    {
        try
        {
            json = ExtractJson(json);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var steps = root.TryGetProperty("steps", out var st)
                ? st.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                : (List<string>)[];
            var focus = root.TryGetProperty("focusAreas", out var fa)
                ? fa.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                : (List<string>)[];

            return new PracticeRecommendation
            {
                StartingTempoBpm  = root.TryGetProperty("startingTempoBpm",  out var t) ? t.GetInt32() : 60,
                EstimatedSessions = root.TryGetProperty("estimatedSessions", out var e) ? e.GetInt32() : 10,
                Steps      = steps,
                FocusAreas = focus
            };
        }
        catch
        {
            return new PracticeRecommendation { Steps = [json] };
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "Claude API key not configured. Call Configure(apiKey) first.");
    }

    private static string ExtractJson(string text)
    {
        // Strip markdown code fences if present
        int start = text.IndexOf('{');
        int end   = text.LastIndexOf('}');
        if (start >= 0 && end > start)
            return text[start..(end + 1)];
        return text;
    }

    private static string KeySigName(KeySignature ks)
    {
        string[] majorKeys = ["Cb", "Gb", "Db", "Ab", "Eb", "Bb", "F", "C", "G", "D", "A", "E", "B", "F#", "C#"];
        int idx = Math.Clamp(ks.Fifths + 7, 0, 14);
        string mode = ks.Mode == Mode.Minor ? " minor" : " major";
        return majorKeys[idx] + mode;
    }

    private static ChordLabel? ParseChordSymbol(string symbol, KeySignature keySig)
    {
        if (string.IsNullOrWhiteSpace(symbol)) return null;

        // Very basic parser: root + optional quality suffix
        int i = 0;
        if (i >= symbol.Length) return null;

        string root = symbol[i].ToString().ToUpper(); i++;
        string acc = "";
        if (i < symbol.Length && (symbol[i] == '#' || symbol[i] == 'b')) { acc = symbol[i].ToString(); i++; }

        string suffix = i < symbol.Length ? symbol[i..] : "";
        ChordQuality quality = suffix switch
        {
            "m" or "min"            => ChordQuality.Minor,
            "°" or "dim"            => ChordQuality.Diminished,
            "+" or "aug"            => ChordQuality.Augmented,
            "7"                     => ChordQuality.DominantSeventh,
            "maj7" or "M7"          => ChordQuality.MajorSeventh,
            "m7" or "min7"          => ChordQuality.MinorSeventh,
            "ø7" or "m7b5"          => ChordQuality.HalfDiminishedSeventh,
            "°7" or "dim7"          => ChordQuality.DiminishedSeventh,
            "sus2"                  => ChordQuality.Suspended2,
            "sus4"                  => ChordQuality.Suspended4,
            _ when suffix == "" || suffix == "maj" => ChordQuality.Major,
            _                       => ChordQuality.Unknown
        };

        return new ChordLabel { Root = root, RootAccidental = acc, Quality = quality };
    }
}

/// <summary>Converts Score data to concise text for Claude prompts.</summary>
internal static class ScoreSerializer
{
    public static string MelodyToText(Score score, Guid staffId)
    {
        var staff = score.Parts.SelectMany(p => p.Staves).FirstOrDefault(s => s.Id == staffId);
        if (staff is null) return "(empty)";

        var sb = new StringBuilder();
        int measureIdx = 0;
        foreach (var measure in staff.Measures)
        {
            sb.Append($"Measure {measureIdx}: ");
            foreach (var note in measure.Notes.Where(n => n.Pitch is not null))
                sb.Append($"{note.Pitch}@{note.TickOffset} ");
            sb.AppendLine();
            measureIdx++;
        }
        return sb.ToString();
    }

    public static string ScoreToText(Score score)
    {
        int measures = score.TotalMeasures;
        int noteCount = score.Parts
            .SelectMany(p => p.Staves)
            .SelectMany(s => s.Measures)
            .SelectMany(m => m.Notes)
            .Count(n => n.Pitch is not null);

        return $"""
            Title: {score.Title}
            Composer: {score.Composer}
            Key: {score.InitialKeySignature.Fifths} fifths, {score.InitialKeySignature.Mode}
            Tempo: {score.InitialTempo} BPM
            Time Signature: {score.InitialTimeSignature.Numerator}/{score.InitialTimeSignature.Denominator}
            Measures: {measures}
            Total notes: {noteCount}
            Parts: {score.Parts.Count} ({string.Join(", ", score.Parts.Select(p => p.Name))})
            """;
    }
}
