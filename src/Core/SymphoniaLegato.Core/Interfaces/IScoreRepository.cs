using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Core.Interfaces;

/// <summary>Persistence contract for Score documents.</summary>
public interface IScoreRepository
{
    Task<Score?> LoadAsync(string filePath, CancellationToken ct = default);
    Task SaveAsync(Score score, string filePath, CancellationToken ct = default);
    Task<IReadOnlyList<ScoreMetadata>> GetRecentAsync(int count = 10, CancellationToken ct = default);
    Task<bool> ExistsAsync(string filePath, CancellationToken ct = default);
}

/// <summary>Lightweight score header used in recent-files lists and search.</summary>
public sealed class ScoreMetadata
{
    public string FilePath { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Composer { get; init; } = string.Empty;
    public DateTime LastOpened { get; init; }
    public DateTime ModifiedAt { get; init; }
}
