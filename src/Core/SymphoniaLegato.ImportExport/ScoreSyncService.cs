using Microsoft.Extensions.Logging;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.ImportExport;

/// <summary>
/// Synchronises .enscore files between a local working path and a sync folder
/// (e.g. an OneDrive / Dropbox / Nextcloud local directory).
/// Newer file wins; timestamps are compared to the second.
/// </summary>
public sealed class ScoreSyncService
{
    private readonly IScoreRepository _repo;
    private readonly ILogger<ScoreSyncService> _logger;

    public ScoreSyncService(IScoreRepository repo, ILogger<ScoreSyncService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    /// <summary>Lists all .enscore files found in <paramref name="syncFolder"/>.</summary>
    public IReadOnlyList<SyncedScoreInfo> ListRemoteScores(string syncFolder)
    {
        if (!Directory.Exists(syncFolder)) return [];

        return Directory.GetFiles(syncFolder, "*.enscore", SearchOption.AllDirectories)
            .Select(path => new SyncedScoreInfo
            {
                RemotePath  = path,
                FileName    = Path.GetFileName(path),
                LastModified = File.GetLastWriteTimeUtc(path)
            })
            .OrderByDescending(s => s.LastModified)
            .ToList();
    }

    /// <summary>
    /// Pushes <paramref name="localPath"/> to <paramref name="syncFolder"/>.
    /// Overwrites the remote copy if local is newer.
    /// </summary>
    public async Task<SyncResult> PushAsync(string localPath, string syncFolder,
        CancellationToken ct = default)
    {
        if (!File.Exists(localPath))
            return new SyncResult(SyncAction.Error, localPath, "Local file not found");

        Directory.CreateDirectory(syncFolder);
        string dest = Path.Combine(syncFolder, Path.GetFileName(localPath));

        DateTime localTime  = File.GetLastWriteTimeUtc(localPath);
        DateTime remoteTime = File.Exists(dest) ? File.GetLastWriteTimeUtc(dest) : DateTime.MinValue;

        if (localTime <= remoteTime)
            return new SyncResult(SyncAction.Skipped, dest, "Remote is up-to-date");

        await CopyFileAsync(localPath, dest, ct);
        _logger.LogInformation("Pushed {File} → {Dest}", localPath, dest);
        return new SyncResult(SyncAction.Pushed, dest);
    }

    /// <summary>
    /// Pulls the newest copy of <paramref name="fileName"/> from <paramref name="syncFolder"/>
    /// to <paramref name="localDir"/>. Overwrites local if remote is newer.
    /// </summary>
    public async Task<SyncResult> PullAsync(string fileName, string syncFolder,
        string localDir, CancellationToken ct = default)
    {
        string src = Path.Combine(syncFolder, fileName);
        if (!File.Exists(src))
            return new SyncResult(SyncAction.Error, src, "Remote file not found");

        Directory.CreateDirectory(localDir);
        string dest = Path.Combine(localDir, fileName);

        DateTime remoteTime = File.GetLastWriteTimeUtc(src);
        DateTime localTime  = File.Exists(dest) ? File.GetLastWriteTimeUtc(dest) : DateTime.MinValue;

        if (remoteTime <= localTime)
            return new SyncResult(SyncAction.Skipped, dest, "Local is up-to-date");

        await CopyFileAsync(src, dest, ct);
        _logger.LogInformation("Pulled {Src} → {Dest}", src, dest);
        return new SyncResult(SyncAction.Pulled, dest);
    }

    /// <summary>
    /// Two-way sync: pushes local if newer, pulls remote if newer.
    /// </summary>
    public async Task<SyncResult> SyncAsync(string localPath, string syncFolder,
        CancellationToken ct = default)
    {
        if (!File.Exists(localPath))
            return await PullAsync(Path.GetFileName(localPath), syncFolder,
                Path.GetDirectoryName(localPath)!, ct);

        string dest = Path.Combine(syncFolder, Path.GetFileName(localPath));

        if (!File.Exists(dest))
            return await PushAsync(localPath, syncFolder, ct);

        DateTime localTime  = File.GetLastWriteTimeUtc(localPath);
        DateTime remoteTime = File.GetLastWriteTimeUtc(dest);

        return localTime >= remoteTime
            ? await PushAsync(localPath, syncFolder, ct)
            : await PullAsync(Path.GetFileName(localPath), syncFolder,
                Path.GetDirectoryName(localPath)!, ct);
    }

    private static async Task CopyFileAsync(string src, string dest, CancellationToken ct)
    {
        const int bufferSize = 81920;
        await using var input  = new FileStream(src,  FileMode.Open,   FileAccess.Read,  FileShare.Read,  bufferSize, useAsync: true);
        await using var output = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
        await input.CopyToAsync(output, ct);
        File.SetLastWriteTimeUtc(dest, File.GetLastWriteTimeUtc(src));
    }
}

public enum SyncAction { Pushed, Pulled, Skipped, Error }

public sealed class SyncResult(SyncAction action, string path, string? message = null)
{
    public SyncAction Action  { get; } = action;
    public string     Path    { get; } = path;
    public string?    Message { get; } = message;
    public bool       Success => Action != SyncAction.Error;
}

public sealed class SyncedScoreInfo
{
    public string   RemotePath   { get; init; } = string.Empty;
    public string   FileName     { get; init; } = string.Empty;
    public DateTime LastModified { get; init; }
}
