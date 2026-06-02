using Avalonia.Controls;
using SymphoniaLegato.Desktop.ViewModels;

namespace SymphoniaLegato.Desktop.Views;

public sealed partial class PluginManagerWindow : Window
{
    public PluginManagerWindow() => InitializeComponent();

    public PluginManagerWindow(PluginManagerViewModel vm) : this()
    {
        DataContext = vm;
        vm.CloseRequested += (_, _) => Close();
    }
}
