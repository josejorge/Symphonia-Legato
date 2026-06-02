using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Android.ViewModels;

public sealed partial class ScoreViewerViewModel : ViewModelBase
{
    private readonly ILayoutEngine    _layout;
    private readonly IScoreRepository _repo;

    [ObservableProperty] private LayoutResult? _layoutResult;
    [ObservableProperty] private double  _zoom = 0.7;
    [ObservableProperty] private int     _currentPage = 1;
    [ObservableProperty] private int     _totalPages  = 1;
    [ObservableProperty] private Score?  _score;
    [ObservableProperty] private bool    _isLoading;
    [ObservableProperty] private string  _statusText = "Open a score from the Library";

    public ScoreViewerViewModel(ILayoutEngine layout, IScoreRepository repo)
    {
        _layout = layout;
        _repo   = repo;
    }

    public async Task LoadAsync(string path)
    {
        IsLoading = true;
        StatusText = "Loading…";
        try
        {
            Score = await _repo.LoadAsync(path);
            if (Score is null) { StatusText = "Could not open score"; return; }
            RefreshLayout();
            StatusText = Score.Title;
        }
        catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void ZoomIn()  { Zoom = Math.Min(4.0, Zoom * 1.25); RefreshLayout(); }
    [RelayCommand]
    private void ZoomOut() { Zoom = Math.Max(0.2, Zoom / 1.25); RefreshLayout(); }
    [RelayCommand]
    private void PrevPage() { if (CurrentPage > 1) { CurrentPage--; RefreshLayout(); } }
    [RelayCommand]
    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; RefreshLayout(); } }

    private void RefreshLayout()
    {
        if (Score is null) return;
        var opts = new LayoutOptions { Zoom = Zoom };
        LayoutResult = _layout.ComputePageLayout(Score, opts, CurrentPage);
        TotalPages = _layout.ComputeLayout(Score, opts).Pages.Count;
    }
}
