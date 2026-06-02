using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Android.ViewModels;

public sealed class ScoreFileInfo
{
    public string Path        { get; init; } = string.Empty;
    public string FileName    { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public DateTime Modified  { get; init; }
}

public sealed partial class FileBrowserViewModel : ViewModelBase
{
    private readonly IScoreRepository _repo;

    [ObservableProperty] private string _searchFolder = string.Empty;
    [ObservableProperty] private string _statusText   = "Tap Scan to find scores";
    [ObservableProperty] private bool   _isScanning;
    [ObservableProperty] private ScoreFileInfo? _selectedScore;

    public ObservableCollection<ScoreFileInfo> Scores { get; } = [];

    public event EventHandler<string>? ScoreOpened;

    public FileBrowserViewModel(IScoreRepository repo)
    {
        _repo = repo;
        // Default to the app's external storage folder
        SearchFolder = System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.MyDocuments);
    }

    [RelayCommand]
    private async Task ScanFolderAsync()
    {
        IsScanning = true;
        Scores.Clear();
        StatusText = "Scanning…";

        try
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(SearchFolder)) return;
                foreach (var file in Directory.GetFiles(SearchFolder, "*.enscore", SearchOption.AllDirectories))
                {
                    var info = new ScoreFileInfo
                    {
                        Path        = file,
                        FileName    = System.IO.Path.GetFileName(file),
                        DisplayName = System.IO.Path.GetFileNameWithoutExtension(file),
                        Modified    = File.GetLastWriteTime(file)
                    };
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => Scores.Add(info));
                }
            });
            StatusText = Scores.Count == 0 ? "No scores found" : $"{Scores.Count} score(s) found";
        }
        catch (Exception ex)
        {
            StatusText = $"Scan error: {ex.Message}";
        }
        finally { IsScanning = false; }
    }

    [RelayCommand]
    private async Task OpenSelectedAsync()
    {
        if (SelectedScore is null) return;
        var score = await _repo.LoadAsync(SelectedScore.Path);
        if (score is not null)
            ScoreOpened?.Invoke(this, SelectedScore.Path);
    }
}
