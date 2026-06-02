using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.ImportExport;

/// <summary>
/// Reads and writes the .enscore file format — a ZIP container with:
///   score.xml   (MusicXML)
///   project.json (metadata + settings)
///   audio/      (audio references)
///   assets/     (images, etc.)
/// </summary>
public sealed class EnScoreRepository : IScoreRepository
{
    private readonly MusicXmlImporter _xmlImporter;
    private readonly MusicXmlExporter _xmlExporter;
    private readonly ILogger<EnScoreRepository> _logger;

    public EnScoreRepository(
        MusicXmlImporter xmlImporter,
        MusicXmlExporter xmlExporter,
        ILogger<EnScoreRepository> logger)
    {
        _xmlImporter = xmlImporter;
        _xmlExporter = xmlExporter;
        _logger = logger;
    }

    public async Task<Score?> LoadAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) return null;

        using var zip = ZipFile.OpenRead(filePath);

        // Read project metadata
        ProjectJson? meta = null;
        var metaEntry = zip.GetEntry("project.json");
        if (metaEntry is not null)
        {
            await using var metaStream = metaEntry.Open();
            meta = await JsonSerializer.DeserializeAsync<ProjectJson>(metaStream, cancellationToken: ct);
        }

        // Read score XML
        var xmlEntry = zip.GetEntry("score.xml")
            ?? throw new InvalidDataException("Missing score.xml in .enscore file");

        Score score;
        await using (var xmlStream = xmlEntry.Open())
        {
            // Buffer because ZipArchiveEntry stream doesn't support seeking
            using var buf = new MemoryStream();
            await xmlStream.CopyToAsync(buf, ct);
            buf.Position = 0;
            score = await _xmlImporter.ImportFromStreamAsync(buf, ct);
        }

        // Merge metadata
        if (meta is not null)
        {
            score.Title    = meta.Title    ?? score.Title;
            score.Composer = meta.Composer ?? score.Composer;
            score.Copyright = meta.Copyright ?? score.Copyright;
            if (meta.InitialTempo > 0) score.InitialTempo = meta.InitialTempo;
        }

        _logger.LogInformation("Loaded .enscore: {Path}", filePath);
        return score;
    }

    public async Task SaveAsync(Score score, string filePath, CancellationToken ct = default)
    {
        var tmpPath = filePath + ".tmp";
        using (var zip = ZipFile.Open(tmpPath, ZipArchiveMode.Create))
        {
            // Write score.xml
            var xmlEntry = zip.CreateEntry("score.xml", CompressionLevel.Optimal);
            await using (var xmlStream = xmlEntry.Open())
                await _xmlExporter.ExportToStreamAsync(score, xmlStream, null, ct);

            // Write project.json
            var meta = new ProjectJson
            {
                Title       = score.Title,
                Composer    = score.Composer,
                Copyright   = score.Copyright,
                CreatedAt   = score.CreatedAt,
                ModifiedAt  = DateTime.UtcNow,
                FormatVersion = score.FormatVersion,
                InitialTempo = score.InitialTempo,
                PageSize    = score.PageSize.ToString()
            };
            var jsonEntry = zip.CreateEntry("project.json", CompressionLevel.Optimal);
            await using (var jsonStream = jsonEntry.Open())
                await JsonSerializer.SerializeAsync(jsonStream, meta,
                    new JsonSerializerOptions { WriteIndented = true }, ct);

            // Placeholder directories
            zip.CreateEntry("audio/");
            zip.CreateEntry("assets/");
        }

        // Atomic replace
        File.Move(tmpPath, filePath, overwrite: true);
        _logger.LogInformation("Saved .enscore: {Path}", filePath);
    }

    public async Task<IReadOnlyList<ScoreMetadata>> GetRecentAsync(int count = 10, CancellationToken ct = default)
    {
        // Stored in app data; simple JSON list
        string recentPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SymphoniaLegato", "recent.json");

        if (!File.Exists(recentPath)) return [];

        await using var stream = File.OpenRead(recentPath);
        var list = await JsonSerializer.DeserializeAsync<List<ScoreMetadata>>(stream, cancellationToken: ct)
                   ?? [];
        return list.Take(count).ToList();
    }

    public Task<bool> ExistsAsync(string filePath, CancellationToken ct = default) =>
        Task.FromResult(File.Exists(filePath));

    private sealed class ProjectJson
    {
        public string? Title { get; set; }
        public string? Composer { get; set; }
        public string? Copyright { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string? FormatVersion { get; set; }
        public int InitialTempo { get; set; }
        public string? PageSize { get; set; }
    }
}
