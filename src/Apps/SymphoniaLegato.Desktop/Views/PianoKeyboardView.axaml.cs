// File: PianoKeyboardView.axaml.cs
// Description: Code-behind for the on-screen piano keyboard view.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-01
// Last edit date: 2026-09-15
// Version: 1.0.0

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
