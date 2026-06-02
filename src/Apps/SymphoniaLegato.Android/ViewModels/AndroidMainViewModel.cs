using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SymphoniaLegato.Android.ViewModels;

public sealed partial class AndroidMainViewModel : ViewModelBase
{
    [ObservableProperty] private int    _selectedTabIndex = 0;
    [ObservableProperty] private string _appTitle = "Symphonia Legato";

    public FileBrowserViewModel   FileBrowser { get; }
    public ScoreViewerViewModel   ScoreViewer { get; }
    public MobilePlaybackViewModel Playback   { get; }
    public MobileMetronomeViewModel Metronome { get; }
    public AnnotationViewModel    Annotations { get; }

    public AndroidMainViewModel(
        FileBrowserViewModel fileBrowser,
        ScoreViewerViewModel scoreViewer,
        MobilePlaybackViewModel playback,
        MobileMetronomeViewModel metronome,
        AnnotationViewModel annotations)
    {
        FileBrowser = fileBrowser;
        ScoreViewer = scoreViewer;
        Playback    = playback;
        Metronome   = metronome;
        Annotations = annotations;

        FileBrowser.ScoreOpened += OnScoreOpened;
    }

    private void OnScoreOpened(object? sender, string scorePath)
    {
        AppTitle = $"Symphonia Legato — {Path.GetFileNameWithoutExtension(scorePath)}";
        SelectedTabIndex = 1; // switch to Score Viewer tab
    }

    [RelayCommand]
    private void NavigateTo(int tabIndex) => SelectedTabIndex = tabIndex;
}
