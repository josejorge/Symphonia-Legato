using Avalonia.Controls;
using Avalonia.Input;
using SymphoniaLegato.Desktop.ViewModels;

namespace SymphoniaLegato.Desktop.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.AboutRequested += OnAboutRequested;
    }

    private void OnAboutRequested(object? sender, AboutViewModel aboutVm)
    {
        new AboutWindow(aboutVm).ShowDialog(this);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.None)
            e.Handled = true;
    }
}
