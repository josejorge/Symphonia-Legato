using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace SymphoniaLegato.GitIntegration;

/// <summary>
/// Git-based version control for .enscore projects.
/// Each saved score file lives inside a git repository;
/// this service wraps libgit2sharp to provide score-aware operations.
/// </summary>
public sealed class ScoreVersionControl : IDisposable
{
    private readonly ILogger<ScoreVersionControl> _logger;
    private Repository? _repo;

    public bool IsInitialized => _repo is not null;

    public ScoreVersionControl(ILogger<ScoreVersionControl> logger) => _logger = logger;

    /// <summary>Opens or initialises a git repository at <paramref name="repoPath"/>.</summary>
    public void Open(string repoPath)
    {
        if (Repository.IsValid(repoPath))
        {
            _repo = new Repository(repoPath);
            _logger.LogInformation("Opened git repo at {Path}", repoPath);
        }
        else
        {
            Repository.Init(repoPath);
            _repo = new Repository(repoPath);
            _logger.LogInformation("Initialised new git repo at {Path}", repoPath);
        }
    }

    /// <summary>Stages all changes and creates a commit with <paramref name="message"/>.</summary>
    public void Commit(string message, string authorName = "Symphonia User", string authorEmail = "user@symphonia.local")
    {
        if (_repo is null) throw new InvalidOperationException("Repository not open");

        Commands.Stage(_repo, "*");

        var status = _repo.RetrieveStatus();
        if (!status.IsDirty)
        {
            _logger.LogDebug("Nothing to commit");
            return;
        }

        var sig = new Signature(authorName, authorEmail, DateTimeOffset.Now);
        _repo.Commit(message, sig, sig);
        _logger.LogInformation("Committed: {Message}", message);
    }

    /// <summary>Returns ordered list of recent commits (newest first).</summary>
    public IReadOnlyList<CommitInfo> GetHistory(int maxCount = 50)
    {
        if (_repo is null) return [];

        return _repo.Commits
            .QueryBy(new CommitFilter { SortBy = CommitSortStrategies.Time })
            .Take(maxCount)
            .Select(c => new CommitInfo
            {
                Sha     = c.Sha[..8],
                Message = c.Message.Trim(),
                Author  = c.Author.Name,
                When    = c.Author.When.LocalDateTime
            })
            .ToList();
    }

    /// <summary>Checks out a specific commit (detached HEAD), restoring score files.</summary>
    public void CheckoutCommit(string sha)
    {
        if (_repo is null) throw new InvalidOperationException("Repository not open");
        var commit = _repo.Lookup<Commit>(sha);
        Commands.Checkout(_repo, commit);
        _logger.LogInformation("Checked out commit {SHA}", sha[..8]);
    }

    /// <summary>Returns the branches in the repository.</summary>
    public IReadOnlyList<string> GetBranches() =>
        _repo?.Branches.Select(b => b.FriendlyName).ToList() ?? [];

    /// <summary>Creates a new branch at HEAD.</summary>
    public void CreateBranch(string branchName)
    {
        if (_repo is null) throw new InvalidOperationException("Repository not open");
        _repo.CreateBranch(branchName);
        _logger.LogInformation("Created branch: {Branch}", branchName);
    }

    public void Dispose()
    {
        _repo?.Dispose();
        _repo = null;
    }
}

public sealed class CommitInfo
{
    public string Sha { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public DateTime When { get; init; }
}
