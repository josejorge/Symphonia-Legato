using FluentAssertions;
using SymphoniaLegato.AIEngine;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.NotationEngine;
using Xunit;

namespace SymphoniaLegato.Integration.Tests;

/// <summary>Phase 5: AI engine — offline algorithmic features (no network calls).</summary>
public class Phase5AITests
{
    // ── ChordDetector ────────────────────────────────────────────────────

    [Fact]
    public void ChordDetector_CMajorTriad_DetectsMajorChord()
    {
        // C-E-G pitch classes = 0, 4, 7
        var pitchClasses = new List<int> { 0, 4, 7 };
        var keySig = KeySignature.CMajor;

        var chord = ChordDetector.IdentifyChord(pitchClasses, keySig);

        chord.Should().NotBeNull();
        chord!.Quality.Should().Be(ChordQuality.Major);
        chord.Root.Should().Be("C");
        chord.RomanNumeral.Should().Be("I");
    }

    [Fact]
    public void ChordDetector_AMinorTriad_DetectsMinorChord()
    {
        // A-C-E = 9, 0, 4
        var pitchClasses = new List<int> { 9, 0, 4 };
        var keySig = KeySignature.CMajor;

        var chord = ChordDetector.IdentifyChord(pitchClasses, keySig);

        chord.Should().NotBeNull();
        chord!.Quality.Should().Be(ChordQuality.Minor);
        chord.Root.Should().Be("A");
        chord.RomanNumeral.Should().Be("vi");
    }

    [Fact]
    public void ChordDetector_GDominantSeventh_DetectsG7()
    {
        // G-B-D-F = 7, 11, 2, 5
        var pitchClasses = new List<int> { 7, 11, 2, 5 };
        var keySig = KeySignature.CMajor;

        var chord = ChordDetector.IdentifyChord(pitchClasses, keySig);

        chord.Should().NotBeNull();
        chord!.Quality.Should().Be(ChordQuality.DominantSeventh);
        chord.Root.Should().Be("G");
        chord.RomanNumeral.Should().Be("V7");
    }

    [Fact]
    public void ChordDetector_EmptyScore_ReturnsEmpty()
    {
        var score = Score.CreatePianoScore("Test");
        var staffId = score.Parts[0].Staves[0].Id;

        var result = ChordDetector.DetectForStaff(score, staffId);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ChordDetector_ScoreWithNotes_DetectsChordsPerMeasure()
    {
        var score = Score.CreatePianoScore("Chord Test");
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ScoreEditor>.Instance;
        var editor = new ScoreEditor(score, logger);
        editor.AddMeasures(0, 2);

        var staff = score.Parts[0].Staves[0];
        var measure = staff.Measures[0];

        // Add C-E-G chord
        var c4 = new Note { Pitch = new Pitch(NoteName.C, Accidental.Natural, 4), Duration = Duration.Quarter };
        c4.ChordNotes.Add(new Pitch(NoteName.E, Accidental.Natural, 4));
        c4.ChordNotes.Add(new Pitch(NoteName.G, Accidental.Natural, 4));
        measure.AddNote(c4);

        var result = ChordDetector.DetectForStaff(score, staff.Id);

        result.Should().NotBeEmpty();
        result[0].Chords.Should().NotBeEmpty();
        result[0].Chords[0].Quality.Should().Be(ChordQuality.Major);
    }

    [Fact]
    public void ChordDetector_DisplayName_FormatsCorrectly()
    {
        var chord = new ChordLabel
        {
            Root = "D", RootAccidental = "", Quality = ChordQuality.Minor
        };
        chord.DisplayName.Should().Be("Dm");
    }

    [Fact]
    public void ChordDetector_DisplayNameWithBass_IncludesBassNote()
    {
        var chord = new ChordLabel
        {
            Root = "C", RootAccidental = "", Quality = ChordQuality.Major, BassNote = "E"
        };
        chord.DisplayName.Should().Be("C/E");
    }

    // ── FingeringAdvisor ─────────────────────────────────────────────────

    [Fact]
    public void FingeringAdvisor_EmptyStaff_ReturnsEmptyResult()
    {
        var score = Score.CreatePianoScore("Finger Test");
        var staffId = score.Parts[0].Staves[0].Id;

        var result = FingeringAdvisor.SuggestForStaff(score, staffId);

        result.StaffId.Should().Be(staffId);
        result.Notes.Should().BeEmpty();
    }

    [Fact]
    public void FingeringAdvisor_SingleNote_AssignsFinger()
    {
        var score = Score.CreatePianoScore("Finger Test");
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ScoreEditor>.Instance;
        var editor = new ScoreEditor(score, logger);
        editor.AddMeasures(0, 1);

        var staff = score.Parts[0].Staves[0];
        var measure = staff.Measures[0];
        measure.AddNote(new Note
        {
            Pitch = new Pitch(NoteName.C, Accidental.Natural, 4),
            Duration = Duration.Quarter
        });

        var result = FingeringAdvisor.SuggestForStaff(score, staff.Id);

        result.Notes.Should().HaveCount(1);
        result.Notes[0].Finger.Should().BeOneOf(Finger.Thumb, Finger.Index, Finger.Middle, Finger.Ring, Finger.Pinky);
    }

    [Fact]
    public void FingeringAdvisor_AscendingScale_AssignsSequentialFingers()
    {
        var score = Score.CreatePianoScore("Scale Test");
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ScoreEditor>.Instance;
        var editor = new ScoreEditor(score, logger);
        editor.AddMeasures(0, 2);

        var staff = score.Parts[0].Staves[0];
        var measure = staff.Measures[0];

        // C-D-E-F ascending
        NoteName[] names = [NoteName.C, NoteName.D, NoteName.E, NoteName.F];
        foreach (var name in names)
            measure.AddNote(new Note
            {
                Pitch = new Pitch(name, Accidental.Natural, 4),
                Duration = Duration.Quarter
            });

        var result = FingeringAdvisor.SuggestForStaff(score, staff.Id);

        result.Notes.Should().HaveCount(4);
        result.Notes.All(n => (int)n.Finger >= 1 && (int)n.Finger <= 5).Should().BeTrue();
    }

    // ── ClaudeAIEngine (offline/configuration checks only) ───────────────

    [Fact]
    public void ClaudeAIEngine_NotConfigured_IsConfiguredIsFalse()
    {
        var engine = new ClaudeAIEngine();
        engine.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void ClaudeAIEngine_Configure_SetsConfigured()
    {
        var engine = new ClaudeAIEngine();
        engine.Configure("sk-ant-test-key");
        engine.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void ClaudeAIEngine_ConfigureEmpty_SetsNotConfigured()
    {
        var engine = new ClaudeAIEngine();
        engine.Configure("sk-ant-test-key");
        engine.Configure("");
        engine.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task ClaudeAIEngine_GetAnalysis_WhenNotConfigured_Throws()
    {
        var engine = new ClaudeAIEngine();
        var score = Score.CreatePianoScore("Test");

        await engine.Invoking(e => e.GetScoreAnalysisAsync(score))
                    .Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*API key*");
    }

    [Fact]
    public void ClaudeAIEngine_DetectChords_WorksOffline()
    {
        var engine = new ClaudeAIEngine();  // no API key needed
        var score = Score.CreatePianoScore("Test");
        var staffId = score.Parts[0].Staves[0].Id;

        // Offline — should not throw even without API key
        var result = engine.DetectChords(score, staffId);
        result.Should().NotBeNull();
    }

    [Fact]
    public void ClaudeAIEngine_SuggestFingering_WorksOffline()
    {
        var engine = new ClaudeAIEngine();  // no API key needed
        var score = Score.CreatePianoScore("Test");
        var staffId = score.Parts[0].Staves[0].Id;

        var result = engine.SuggestFingering(score, staffId);
        result.Should().NotBeNull();
        result.StaffId.Should().Be(staffId);
    }
}
