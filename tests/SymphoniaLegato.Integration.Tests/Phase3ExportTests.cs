using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.ImportExport;
using SymphoniaLegato.NotationEngine;
using LayoutEngineImpl = SymphoniaLegato.LayoutEngine.LayoutEngine;
using SymphoniaLegato.PdfEngine;

namespace SymphoniaLegato.Integration.Tests;

public sealed class Phase3SvgExporterTests
{
    private readonly ILayoutEngine _layout;
    private readonly ScoreSvgExporter _exporter;
    private readonly Score _score;

    public Phase3SvgExporterTests()
    {
        _layout   = new LayoutEngineImpl(NullLogger<LayoutEngineImpl>.Instance);
        _exporter = new ScoreSvgExporter(_layout);
        _score    = Score.CreatePianoScore("Test Score");
        _score.Composer = "Jose Jorge Hernandez";

        // Add measures so the layout engine has content to render
        var editor = new ScoreEditor(_score, NullLogger<ScoreEditor>.Instance);
        editor.AddMeasures(0, 4);
    }

    [Fact]
    public void ExportPage_Returns_ValidSvgDocument()
    {
        var svg = _exporter.ExportPage(_score);

        svg.Should().StartWith("<?xml");
        svg.Should().Contain("<svg");
        svg.Should().Contain("</svg>");
    }

    [Fact]
    public void ExportPage_Contains_ScoreTitle()
    {
        var svg = _exporter.ExportPage(_score);

        svg.Should().Contain("Test Score");
    }

    [Fact]
    public void ExportPage_Contains_ComposerName()
    {
        var svg = _exporter.ExportPage(_score);

        svg.Should().Contain("Jose Jorge Hernandez");
    }

    [Fact]
    public void ExportPage_Contains_StaffLines()
    {
        var svg = _exporter.ExportPage(_score);

        // Staff lines are rendered as SVG <line> elements
        svg.Should().Contain("<line ");
    }

    [Fact]
    public void ExportPage_HasCorrectDimensions()
    {
        var svg = _exporter.ExportPage(_score);

        // A4 at 96 DPI zoom=1 should produce non-trivial width
        svg.Should().MatchRegex(@"width=""[1-9]\d+\.\d+""");
        svg.Should().MatchRegex(@"height=""[1-9]\d+\.\d+""");
    }

    [Fact]
    public async Task ExportAllPagesAsync_WritesSvgFiles()
    {
        var outDir = Path.Combine(Path.GetTempPath(), $"svg_test_{Guid.NewGuid():N}");
        try
        {
            await _exporter.ExportAllPagesAsync(_score, outDir);

            Directory.Exists(outDir).Should().BeTrue();
            var svgFiles = Directory.GetFiles(outDir, "*.svg");
            svgFiles.Should().NotBeEmpty();

            var content = await File.ReadAllTextAsync(svgFiles[0]);
            content.Should().Contain("<svg");
        }
        finally
        {
            if (Directory.Exists(outDir))
                Directory.Delete(outDir, recursive: true);
        }
    }
}

public sealed class Phase3PdfExporterTests
{
    private readonly ScorePdfExporter _exporter;
    private readonly Score _score;

    public Phase3PdfExporterTests()
    {
        _exporter = new ScorePdfExporter();
        _score    = Score.CreatePianoScore("PDF Test");
        _score.Composer = "Jose Jorge Hernandez";
    }

    [Fact]
    public void GenerateFromImages_ProducesNonEmptyPdf()
    {
        // Create a tiny placeholder PNG (1x1 white pixel)
        byte[] fakePagePng = CreateMinimalPng();

        var pdfBytes = _exporter.GenerateFromImages(_score, new List<byte[]> { fakePagePng });

        pdfBytes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateFromImages_StartsWithPdfHeader()
    {
        byte[] fakePagePng = CreateMinimalPng();

        var pdfBytes = _exporter.GenerateFromImages(_score, new List<byte[]> { fakePagePng });

        // All valid PDFs start with %PDF
        var header = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 4);
        header.Should().Be("%PDF");
    }

    [Fact]
    public void GenerateFromImages_ThrowsWhenNoPages()
    {
        var act = () => _exporter.GenerateFromImages(_score, new List<byte[]>());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateFromImages_MultiplePages()
    {
        byte[] fakePagePng = CreateMinimalPng();

        var pdfBytes = _exporter.GenerateFromImages(_score, new List<byte[]> { fakePagePng, fakePagePng });

        pdfBytes.Should().NotBeNullOrEmpty();
        var header = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 4);
        header.Should().Be("%PDF");
    }

    // Minimal valid 1×1 white PNG (67 bytes) to use as a placeholder page image
    private static byte[] CreateMinimalPng()
    {
        // PNG signature + IHDR + IDAT + IEND for a 1x1 white pixel
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwADhQGAWjR9awAAAABJRU5ErkJggg==");
    }
}

public sealed class Phase3ScorePropertiesTests
{
    [Fact]
    public void Score_HasAllMetadataFields()
    {
        var score = new Score
        {
            Title     = "My Sonata",
            Subtitle  = "Op. 1",
            Composer  = "Jose Jorge Hernandez",
            Lyricist  = "J.H.",
            Arranger  = "Anonymous",
            Copyright = "© 2026",
            Notes     = "Allegro, con fuoco"
        };

        score.Title.Should().Be("My Sonata");
        score.Subtitle.Should().Be("Op. 1");
        score.Composer.Should().Be("Jose Jorge Hernandez");
        score.Copyright.Should().Be("© 2026");
    }

    [Fact]
    public void Score_DefaultPageSize_IsA4()
    {
        var score = Score.CreatePianoScore();

        score.PageSize.Should().Be(PageSize.A4);
        score.PageWidthMm.Should().Be(210);
        score.PageHeightMm.Should().Be(297);
    }

    [Fact]
    public void Score_CanChangePageSizeToLetter()
    {
        var score = Score.CreatePianoScore();
        score.PageSize    = PageSize.Letter;
        score.PageWidthMm = 215.9;
        score.PageHeightMm = 279.4;

        score.PageSize.Should().Be(PageSize.Letter);
        score.PageWidthMm.Should().BeApproximately(215.9, 0.1);
    }

    [Fact]
    public void Score_InitialTempo_DefaultsTo120()
    {
        var score = Score.CreatePianoScore();
        score.InitialTempo.Should().Be(120);
    }

    [Fact]
    public void Score_InitialTimeSignature_DefaultsToCommon()
    {
        var score = Score.CreatePianoScore();
        score.InitialTimeSignature.Numerator.Should().Be(4);
        score.InitialTimeSignature.Denominator.Should().Be(4);
    }
}

public sealed class Phase3GitIntegrationTests
{
    // Git pack files on Windows have read-only attributes; this handles cleanup.
    private static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
        Directory.Delete(path, recursive: true);
    }

    [Fact]
    public void ScoreVersionControl_OpenOrInit_CreatesRepository()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"git_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempPath);

        try
        {
            using var vcs = new SymphoniaLegato.GitIntegration.ScoreVersionControl(
                NullLogger<SymphoniaLegato.GitIntegration.ScoreVersionControl>.Instance);

            vcs.Open(tempPath);
            vcs.IsInitialized.Should().BeTrue();
        }
        finally { DeleteDirectory(tempPath); }
    }

    [Fact]
    public void ScoreVersionControl_GetHistory_ReturnsEmpty_OnNewRepo()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"git_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempPath);

        try
        {
            using var vcs = new SymphoniaLegato.GitIntegration.ScoreVersionControl(
                NullLogger<SymphoniaLegato.GitIntegration.ScoreVersionControl>.Instance);

            vcs.Open(tempPath);
            var history = vcs.GetHistory();
            history.Should().BeEmpty();
        }
        finally { DeleteDirectory(tempPath); }
    }

    [Fact]
    public void ScoreVersionControl_Commit_AddsToHistory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"git_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempPath);

        try
        {
            using var vcs = new SymphoniaLegato.GitIntegration.ScoreVersionControl(
                NullLogger<SymphoniaLegato.GitIntegration.ScoreVersionControl>.Instance);

            vcs.Open(tempPath);
            File.WriteAllText(Path.Combine(tempPath, "score.xml"), "<score/>");
            vcs.Commit("Initial commit", "Test User", "test@test.local");

            var history = vcs.GetHistory();
            history.Should().HaveCount(1);
            history[0].Message.Should().Be("Initial commit");
            history[0].Author.Should().Be("Test User");
        }
        finally { DeleteDirectory(tempPath); }
    }

    [Fact]
    public void ScoreVersionControl_GetBranches_ReturnsMaster()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"git_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempPath);

        try
        {
            using var vcs = new SymphoniaLegato.GitIntegration.ScoreVersionControl(
                NullLogger<SymphoniaLegato.GitIntegration.ScoreVersionControl>.Instance);

            vcs.Open(tempPath);
            File.WriteAllText(Path.Combine(tempPath, "score.xml"), "<score/>");
            vcs.Commit("Initial", "User", "u@u.local");

            var branches = vcs.GetBranches();
            branches.Should().NotBeEmpty();
        }
        finally { DeleteDirectory(tempPath); }
    }
}
