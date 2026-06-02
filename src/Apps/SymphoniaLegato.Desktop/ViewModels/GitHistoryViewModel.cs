using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SymphoniaLegato.GitIntegration;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class GitHistoryViewModel : ViewModelBase
{
    private readonly ScoreVersionControl _vcs;

    [ObservableProperty] private bool _isInitialized;
    [ObservableProperty] private string _repoPath = string.Empty;
    [ObservableProperty] private string _commitMessage = string.Empty;
    [ObservableProperty] private CommitInfo? _selectedCommit;
    [ObservableProperty] private string _statusText = "No repository open";
    [ObservableProperty] private bool _hasUncommittedChanges;

    public ObservableCollection<CommitInfo> History { get; } = [];

    public GitHistoryViewModel(ScoreVersionControl vcs) => _vcs = vcs;

    [RelayCommand]
    private void OpenRepository(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            _vcs.Open(path);
            RepoPath = path;
            IsInitialized = true;
            StatusText = $"Repository: {Path.GetFileName(path)}";
            Refresh();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Commit()
    {
        if (!IsInitialized || string.IsNullOrWhiteSpace(CommitMessage)) return;

        try
        {
            _vcs.Commit(CommitMessage);
            CommitMessage = string.Empty;
            StatusText = "Committed successfully";
            Refresh();
        }
        catch (Exception ex)
        {
            StatusText = $"Commit failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Restore()
    {
        if (SelectedCommit is null || !IsInitialized) return;

        try
        {
            _vcs.CheckoutCommit(SelectedCommit.Sha);
            StatusText = $"Restored to commit {SelectedCommit.Sha}";
            Refresh();
        }
        catch (Exception ex)
        {
            StatusText = $"Restore failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        if (!IsInitialized) return;

        History.Clear();
        foreach (var commit in _vcs.GetHistory(100))
            History.Add(commit);

        HasUncommittedChanges = History.Count > 0;
    }
}
