using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class AIAssistantViewModel : ViewModelBase
{
    private readonly IAIEngine _ai;

    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private bool _isConfigured;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "Enter your Claude API key to enable AI features.";

    // Chord detection (offline)
    [ObservableProperty] private string _chordSummary = string.Empty;

    // Fingering (offline)
    [ObservableProperty] private string _fingeringSummary = string.Empty;

    // AI features
    [ObservableProperty] private string _harmonisationText = string.Empty;
    [ObservableProperty] private string _analysisText = string.Empty;
    [ObservableProperty] private string _practiceText = string.Empty;

    // Currently active score — set by MainWindowViewModel when the score changes
    private Score? _score;
    private Guid _activeStaffId;

    public AIAssistantViewModel(IAIEngine ai)
    {
        _ai = ai;
    }

    partial void OnApiKeyChanged(string value)
    {
        _ai.Configure(value);
        IsConfigured = _ai.IsConfigured;
        StatusMessage = IsConfigured
            ? "API key set — AI features enabled."
            : "Enter your Claude API key to enable AI features.";
    }

    /// <summary>Called by MainWindowViewModel whenever the active score or staff changes.</summary>
    public void LoadScore(Score score, Guid staffId)
    {
        _score = score;
        _activeStaffId = staffId;
        ChordSummary = string.Empty;
        FingeringSummary = string.Empty;
        HarmonisationText = string.Empty;
        AnalysisText = string.Empty;
        PracticeText = string.Empty;
        StatusMessage = "Score loaded. Run an analysis to begin.";
    }

    // ── Offline commands ─────────────────────────────────────────────────

    [RelayCommand]
    private void DetectChords()
    {
        if (_score is null) { StatusMessage = "No score loaded."; return; }

        var measures = _ai.DetectChords(_score, _activeStaffId);
        if (measures.Count == 0) { ChordSummary = "No chords detected."; return; }

        var lines = measures
            .Where(m => m.Chords.Count > 0)
            .Select(m => $"Bar {m.MeasureNumber + 1}: {string.Join("  ", m.Chords.Select(c => c.DisplayName + " (" + c.RomanNumeral + ")"))}");

        ChordSummary = string.Join("\n", lines);
        StatusMessage = $"Detected chords in {measures.Count} measures.";
    }

    [RelayCommand]
    private void SuggestFingering()
    {
        if (_score is null) { StatusMessage = "No score loaded."; return; }

        var result = _ai.SuggestFingering(_score, _activeStaffId);
        if (result.Notes.Count == 0) { FingeringSummary = "No notes to finger."; return; }

        var lines = result.Notes
            .Select(n => $"{(int)n.Finger}{(n.IsCrossing ? "*" : "")}");

        FingeringSummary = string.Join("  ", lines);
        StatusMessage = $"Fingering suggested for {result.Notes.Count} notes.";
    }

    // ── AI (Claude) commands ─────────────────────────────────────────────

    [RelayCommand]
    private async Task GetHarmonisationAsync()
    {
        if (!CheckReady()) return;
        IsBusy = true;
        StatusMessage = "Asking Claude for harmonisation suggestions…";
        try
        {
            var measures = await _ai.GetHarmonisationAsync(_score!, _activeStaffId);
            var lines = measures
                .Where(m => m.Chords.Count > 0)
                .Select(m => $"Bar {m.MeasureNumber + 1}: {string.Join("  ", m.Chords.Select(c => c.DisplayName))}");
            HarmonisationText = string.Join("\n", lines);
            StatusMessage = "Harmonisation complete.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task GetAnalysisAsync()
    {
        if (!CheckReady()) return;
        IsBusy = true;
        StatusMessage = "Asking Claude to analyse the score…";
        try
        {
            var result = await _ai.GetScoreAnalysisAsync(_score!);
            AnalysisText = $"""
                Key: {result.Key}
                Form: {result.Form}
                Style: {result.Style}
                Difficulty: {result.DifficultyLevel}

                {result.AnalysisText}

                Technical challenges:
                {string.Join("\n", result.TechnicalChallenges.Select(c => "• " + c))}
                """;
            StatusMessage = "Analysis complete.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task GetPracticeAsync()
    {
        if (!CheckReady()) return;
        IsBusy = true;
        StatusMessage = "Asking Claude for a practice plan…";
        try
        {
            var result = await _ai.GetPracticeRecommendationsAsync(_score!);
            PracticeText = $"""
                Starting tempo: {result.StartingTempoBpm} BPM
                Estimated sessions: {result.EstimatedSessions}

                Steps:
                {string.Join("\n", result.Steps.Select((s, i) => $"{i + 1}. {s}"))}

                Focus areas:
                {string.Join("\n", result.FocusAreas.Select(f => "• " + f))}
                """;
            StatusMessage = "Practice plan ready.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private bool CheckReady()
    {
        if (_score is null)    { StatusMessage = "No score loaded.";                     return false; }
        if (!IsConfigured)     { StatusMessage = "Enter your API key first.";            return false; }
        if (IsBusy)            { StatusMessage = "Please wait for the current request."; return false; }
        return true;
    }
}
