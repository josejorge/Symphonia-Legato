using FluentAssertions;
using Xunit;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.ImportExport;
using SymphoniaLegato.PlaybackEngine;
using Microsoft.Extensions.Logging.Abstractions;

namespace SymphoniaLegato.Integration.Tests;

public sealed class Phase4MetronomeTests
{
    [Fact]
    public void MetronomeEngine_DefaultBpm_Is120()
    {
        using var metro = new MetronomeEngine();
        metro.BPM.Should().Be(120);
    }

    [Fact]
    public void MetronomeEngine_ClampsBpm_ToValidRange()
    {
        using var metro = new MetronomeEngine();
        metro.BPM = 5;
        metro.BPM.Should().Be(20);
        metro.BPM = 500;
        metro.BPM.Should().Be(400);
    }

    [Fact]
    public void MetronomeEngine_Start_FiresBeatEvent()
    {
        using var metro = new MetronomeEngine { BPM = 240 };
        int beatsFired = 0;
        metro.Beat += (_, _) => Interlocked.Increment(ref beatsFired);

        metro.Start();
        System.Threading.Thread.Sleep(350); // ~1.5 beats at 240 BPM
        metro.Stop();

        beatsFired.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MetronomeEngine_Stop_StopsFiring()
    {
        using var metro = new MetronomeEngine { BPM = 240 };
        int beatsFired = 0;
        metro.Beat += (_, _) => Interlocked.Increment(ref beatsFired);

        metro.Start();
        System.Threading.Thread.Sleep(150);
        metro.Stop();
        int countAfterStop = beatsFired;
        System.Threading.Thread.Sleep(200);

        beatsFired.Should().Be(countAfterStop);
    }

    [Fact]
    public void MetronomeEngine_IsRunning_ReflectsState()
    {
        using var metro = new MetronomeEngine();
        metro.IsRunning.Should().BeFalse();
        metro.Start();
        metro.IsRunning.Should().BeTrue();
        metro.Stop();
        metro.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void MetronomeEngine_SubdivisionValues_OnlyAcceptValid()
    {
        using var metro = new MetronomeEngine();
        metro.Subdivision = 1;
        metro.Subdivision.Should().Be(1);
        metro.Subdivision = 2;
        metro.Subdivision.Should().Be(2);
        metro.Subdivision = 4;
        metro.Subdivision.Should().Be(4);
        metro.Subdivision = 3; // invalid
        metro.Subdivision.Should().Be(1);
    }
}

public sealed class Phase4AnnotationTests
{
    [Fact]
    public void ScoreAnnotation_AddStroke_IncreasesCount()
    {
        var ann = new ScoreAnnotation { PageNumber = 1 };
        ann.AddStroke(new AnnotationStroke
        {
            Color = "#FF0000",
            Points = [(0.1, 0.1), (0.5, 0.5)]
        });

        ann.Strokes.Should().HaveCount(1);
        ann.Strokes[0].Color.Should().Be("#FF0000");
    }

    [Fact]
    public void ScoreAnnotation_ClearStrokes_RemovesAll()
    {
        var ann = new ScoreAnnotation();
        ann.AddStroke(new AnnotationStroke { Points = [(0, 0), (1, 1)] });
        ann.AddStroke(new AnnotationStroke { Points = [(0, 1), (1, 0)] });

        ann.ClearStrokes();

        ann.Strokes.Should().BeEmpty();
    }

    [Fact]
    public void Score_GetOrCreateAnnotation_ReturnsSameInstance()
    {
        var score = Score.CreatePianoScore("Test");

        var a1 = score.GetOrCreateAnnotation(1);
        var a2 = score.GetOrCreateAnnotation(1);

        a1.Should().BeSameAs(a2);
    }

    [Fact]
    public void Score_GetOrCreateAnnotation_CreatesForNewPage()
    {
        var score = Score.CreatePianoScore("Test");

        var a1 = score.GetOrCreateAnnotation(1);
        var a2 = score.GetOrCreateAnnotation(2);

        a1.Should().NotBeSameAs(a2);
        score.Annotations.Should().HaveCount(2);
    }

    [Fact]
    public void AnnotationStroke_DefaultColor_IsSet()
    {
        var stroke = new AnnotationStroke();
        stroke.Color.Should().Be("#FF4444");
        stroke.Thickness.Should().Be(2.0);
    }
}

public sealed class Phase4SyncServiceTests
{
    [Fact]
    public async Task ScoreSyncService_Push_CopiesFileToSyncFolder()
    {
        var tempLocal = Path.Combine(Path.GetTempPath(), $"sync_local_{Guid.NewGuid():N}");
        var tempSync  = Path.Combine(Path.GetTempPath(), $"sync_remote_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempLocal);

        try
        {
            var localFile = Path.Combine(tempLocal, "test.enscore");
            await File.WriteAllTextAsync(localFile, "<score/>");

            var repo   = new EnScoreRepository(
                new MusicXmlImporter(NullLogger<MusicXmlImporter>.Instance),
                new MusicXmlExporter(NullLogger<MusicXmlExporter>.Instance),
                NullLogger<EnScoreRepository>.Instance);
            var svc    = new ScoreSyncService(repo, NullLogger<ScoreSyncService>.Instance);
            var result = await svc.PushAsync(localFile, tempSync);

            result.Success.Should().BeTrue();
            result.Action.Should().Be(SyncAction.Pushed);
            File.Exists(Path.Combine(tempSync, "test.enscore")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempLocal))  Directory.Delete(tempLocal,  recursive: true);
            if (Directory.Exists(tempSync))   Directory.Delete(tempSync,   recursive: true);
        }
    }

    [Fact]
    public async Task ScoreSyncService_Push_SkipsWhenRemoteIsNewer()
    {
        var tempLocal = Path.Combine(Path.GetTempPath(), $"sync_local_{Guid.NewGuid():N}");
        var tempSync  = Path.Combine(Path.GetTempPath(), $"sync_remote_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempLocal);
        Directory.CreateDirectory(tempSync);

        try
        {
            var localFile  = Path.Combine(tempLocal, "test.enscore");
            var remoteFile = Path.Combine(tempSync,  "test.enscore");

            await File.WriteAllTextAsync(localFile, "<score/>");
            await File.WriteAllTextAsync(remoteFile, "<score/>");

            // Make remote newer
            File.SetLastWriteTimeUtc(remoteFile, DateTime.UtcNow.AddMinutes(5));

            var repo   = new EnScoreRepository(
                new MusicXmlImporter(NullLogger<MusicXmlImporter>.Instance),
                new MusicXmlExporter(NullLogger<MusicXmlExporter>.Instance),
                NullLogger<EnScoreRepository>.Instance);
            var svc    = new ScoreSyncService(repo, NullLogger<ScoreSyncService>.Instance);
            var result = await svc.PushAsync(localFile, tempSync);

            result.Action.Should().Be(SyncAction.Skipped);
        }
        finally
        {
            if (Directory.Exists(tempLocal))  Directory.Delete(tempLocal,  recursive: true);
            if (Directory.Exists(tempSync))   Directory.Delete(tempSync,   recursive: true);
        }
    }

    [Fact]
    public void ScoreSyncService_ListRemoteScores_ReturnsEmpty_ForMissingFolder()
    {
        var repo = new EnScoreRepository(
            new MusicXmlImporter(NullLogger<MusicXmlImporter>.Instance),
            new MusicXmlExporter(NullLogger<MusicXmlExporter>.Instance),
            NullLogger<EnScoreRepository>.Instance);
        var svc  = new ScoreSyncService(repo, NullLogger<ScoreSyncService>.Instance);

        var scores = svc.ListRemoteScores("/nonexistent/path/xyz");

        scores.Should().BeEmpty();
    }
}
