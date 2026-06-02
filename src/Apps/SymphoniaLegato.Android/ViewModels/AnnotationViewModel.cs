using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Android.ViewModels;

public sealed partial class AnnotationViewModel : ViewModelBase
{
    [ObservableProperty] private Score?  _activeScore;
    [ObservableProperty] private int     _activePage    = 1;
    [ObservableProperty] private string  _strokeColor   = "#FF4444";
    [ObservableProperty] private double  _strokeThickness = 3.0;
    [ObservableProperty] private bool    _isEraser;
    [ObservableProperty] private bool    _isAnnotating;

    public static IReadOnlyList<string> ColorOptions { get; } =
        ["#FF4444", "#44AAFF", "#44CC44", "#FFAA00", "#FFFFFF"];

    public ScoreAnnotation? CurrentPageAnnotation =>
        ActiveScore?.GetOrCreateAnnotation(ActivePage);

    [RelayCommand]
    private void ClearPage()
    {
        CurrentPageAnnotation?.ClearStrokes();
        OnPropertyChanged(nameof(CurrentPageAnnotation));
    }

    [RelayCommand]
    private void ClearAll()
    {
        ActiveScore?.Annotations.ForEach(a => a.ClearStrokes());
        OnPropertyChanged(nameof(CurrentPageAnnotation));
    }

    [RelayCommand]
    private void ToggleEraser() => IsEraser = !IsEraser;

    public void AddStroke(List<(double X, double Y)> points)
    {
        if (ActiveScore is null || points.Count < 2) return;
        var ann = ActiveScore.GetOrCreateAnnotation(ActivePage);
        ann.AddStroke(new AnnotationStroke
        {
            Color     = IsEraser ? string.Empty : StrokeColor,
            Thickness = StrokeThickness,
            Points    = new List<(double, double)>(points)
        });
        OnPropertyChanged(nameof(CurrentPageAnnotation));
    }
}
