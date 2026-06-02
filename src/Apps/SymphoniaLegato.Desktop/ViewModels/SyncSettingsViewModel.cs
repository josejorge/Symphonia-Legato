using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SymphoniaLegato.ImportExport;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class SyncSettingsViewModel : ViewModelBase
{
    private readonly ScoreSyncService _sync;
    private readonly ILogger<SyncSettingsViewModel> _logger;

    [ObservableProperty] private string _syncFolder = string.Empty;
    [ObservableProperty] private string _statusText = "No sync folder configured";
    [ObservableProperty] private bool   _isBusy;

    public ObservableCollection<SyncedScoreInfo> RemoteScores { get; } = [];
    public event EventHandler? CloseRequested;
    public event EventHandler? BrowseSyncFolderRequested;
    public event EventHandler<string>? PullScoreRequested; // payload: remote path

    public SyncSettingsViewModel(ScoreSyncService sync, ILogger<SyncSettingsViewModel> logger)
    {
        _sync   = sync;
        _logger = logger;
    }

    [RelayCommand]
    private void BrowseSyncFolder() => BrowseSyncFolderRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void RefreshRemoteList()
    {
        RemoteScores.Clear();
        if (string.IsNullOrWhiteSpace(SyncFolder)) return;

        foreach (var info in _sync.ListRemoteScores(SyncFolder))
            RemoteScores.Add(info);

        StatusText = RemoteScores.Count == 0
            ? "No .enscore files found in sync folder"
            : $"{RemoteScores.Count} score(s) in sync folder";
    }

    [RelayCommand]
    private async Task PushCurrentScoreAsync(string? localPath)
    {
        if (string.IsNullOrWhiteSpace(localPath) || string.IsNullOrWhiteSpace(SyncFolder)) return;

        IsBusy = true;
        try
        {
            var result = await _sync.PushAsync(localPath, SyncFolder);
            StatusText = result.Success ? $"Pushed: {result.Path}" : $"Error: {result.Message}";
            RefreshRemoteList();
        }
        catch (Exception ex)
        {
            StatusText = $"Push failed: {ex.Message}";
            _logger.LogError(ex, "Push failed");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void PullSelected(SyncedScoreInfo? info)
    {
        if (info is null) return;
        PullScoreRequested?.Invoke(this, info.RemotePath);
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
