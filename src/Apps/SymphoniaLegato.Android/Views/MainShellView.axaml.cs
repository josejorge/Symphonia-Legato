using Avalonia.Controls;
using SymphoniaLegato.Android.ViewModels;

namespace SymphoniaLegato.Android.Views;

public sealed partial class MainShellView : UserControl
{
    private Panel? _pageHost;
    private AndroidMainViewModel? _vm;

    private readonly FileBrowserView   _libraryPage   = new();
    private readonly ScoreViewerView   _scorePage     = new();
    private readonly PlayerView        _playerPage    = new();
    private readonly MetronomeView     _metronomePage = new();

    public MainShellView()
    {
        InitializeComponent();
        _pageHost = this.FindControl<Panel>("PageHost");
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not AndroidMainViewModel vm) return;
        _vm = vm;

        _libraryPage.DataContext   = vm.FileBrowser;
        _scorePage.DataContext     = vm.ScoreViewer;
        _playerPage.DataContext    = vm.Playback;
        _metronomePage.DataContext = vm.Metronome;

        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.SelectedTabIndex))
                ShowPage(vm.SelectedTabIndex);
        };

        ShowPage(0);
    }

    private void ShowPage(int index)
    {
        if (_pageHost is null) return;
        _pageHost.Children.Clear();
        Control page = index switch
        {
            1 => _scorePage,
            2 => _playerPage,
            3 => _metronomePage,
            _ => _libraryPage
        };
        _pageHost.Children.Add(page);
    }
}
