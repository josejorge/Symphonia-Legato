using Avalonia.Controls;
using SymphoniaLegato.Desktop.Controls;
using SymphoniaLegato.Desktop.ViewModels;

namespace SymphoniaLegato.Desktop.Views;

public sealed partial class PianoKeyboardView : UserControl
{
    public PianoKeyboardView() => InitializeComponent();

    private async void OnKeyPressed(object? sender, PianoKeyPressedEventArgs e)
    {
        if (DataContext is PianoKeyboardViewModel vm)
            await vm.PressKeyCommand.ExecuteAsync(e.Key);
    }
}
