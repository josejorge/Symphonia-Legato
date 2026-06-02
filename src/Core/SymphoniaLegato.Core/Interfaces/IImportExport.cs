using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Core.Interfaces;

/// <summary>Supported import file formats.</summary>
public enum ImportFormat { MusicXml, Midi, EnScore }

/// <summary>Supported export file formats.</summary>
public enum ExportFormat { MusicXml, Midi, Pdf, Png, Svg, EnScore }

public interface IScoreImporter
{
    ImportFormat Format { get; }
    IReadOnlyList<string> FileExtensions { get; }
    Task<Score> ImportAsync(string filePath, CancellationToken ct = default);
    Task<Score> ImportFromStreamAsync(Stream stream, CancellationToken ct = default);
}

public interface IScoreExporter
{
    ExportFormat Format { get; }
    string DefaultFileExtension { get; }
    Task ExportAsync(Score score, string filePath, ExportOptions? options = null, CancellationToken ct = default);
    Task ExportToStreamAsync(Score score, Stream stream, ExportOptions? options = null, CancellationToken ct = default);
}

public sealed class ExportOptions
{
    public int DPI { get; set; } = 300;
    public PageSize PageSize { get; set; } = PageSize.A4;
    public bool IncludeMetadata { get; set; } = true;
    public int FirstMeasure { get; set; } = 1;
    public int LastMeasure { get; set; } = int.MaxValue;
}
