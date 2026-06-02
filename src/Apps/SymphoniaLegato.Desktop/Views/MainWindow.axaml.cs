using Avalonia.Controls;
using Avalonia.Input;

namespace SymphoniaLegato.Desktop.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Global hotkeys not captured by menu
        if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.None)
        {
            // Routed to PlaybackViewModel via command binding in AXAML
            e.Handled = true;
        }
    }
}
