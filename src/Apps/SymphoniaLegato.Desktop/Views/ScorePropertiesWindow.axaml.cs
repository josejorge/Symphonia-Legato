using Avalonia.Controls;
using SymphoniaLegato.Desktop.ViewModels;

namespace SymphoniaLegato.Desktop.Views;

public sealed partial class ScorePropertiesWindow : Window
{
    public ScorePropertiesWindow() => InitializeComponent();

    public ScorePropertiesWindow(ScorePropertiesViewModel vm) : this()
    {
        DataContext = vm;
        vm.CloseRequested += (_, _) => Close();
    }
}
