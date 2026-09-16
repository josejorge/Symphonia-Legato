// File: IScoreRepository.cs
// Description: Persistence contract for Score documents, plus the lightweight ScoreMetadata used in recent-files lists.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-01
// Last edit date: 2026-09-15
// Version: 1.0.0

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
