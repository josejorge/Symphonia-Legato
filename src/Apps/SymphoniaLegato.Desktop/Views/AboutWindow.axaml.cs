using Avalonia.Controls;
using SymphoniaLegato.Desktop.ViewModels;

namespace SymphoniaLegato.Desktop.Views;

public sealed partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    public AboutWindow(AboutViewModel vm) : this()
    {
        DataContext = vm;
        vm.CloseRequested += (_, _) => Close();
    }
}
