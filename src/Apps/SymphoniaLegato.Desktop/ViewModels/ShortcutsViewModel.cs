// File: ShortcutsViewModel.cs
// Description: View model for the Keyboard Shortcuts dialog (Help menu) — a static, grouped list of every shortcut actually wired in the app, cross-checked against MainWindow.OnKeyDown and MainWindow.axaml's Command bindings rather than just copied from InputGesture labels (one of which, Loop, turned out to be decorative-only — see docs/BUGS.md).
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-09-15
// Last edit date: 2026-09-15
// Version: 1.0.0

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed record ShortcutEntry(string Keys, string Description);

public sealed record ShortcutGroup(string Title, IReadOnlyList<ShortcutEntry> Items);

public sealed partial class ShortcutsViewModel : ViewModelBase
{
    public IReadOnlyList<ShortcutGroup> Groups { get; } =
    [
        new ShortcutGroup("File", [
            new("Ctrl+N", "New score"),
            new("Ctrl+O", "Open..."),
            new("Ctrl+S", "Save"),
            new("Ctrl+Shift+S", "Save As..."),
            new("Alt+F4", "Exit"),
        ]),
        new ShortcutGroup("Edit", [
            new("Ctrl+Z", "Undo"),
            new("Ctrl+Y", "Redo"),
            new("Ctrl+Shift+P", "Score Properties..."),
        ]),
        new ShortcutGroup("View", [
            new("Ctrl+=", "Zoom in"),
            new("Ctrl+-", "Zoom out"),
            new("Ctrl+0", "Zoom reset"),
            new("Ctrl+Shift+G", "Toggle Git History panel"),
            new("Ctrl+Shift+M", "Toggle Metronome panel"),
            new("Ctrl+Shift+A", "Toggle AI Assistant panel"),
            new("Ctrl+Shift+Y", "Cloud Sync Settings..."),
        ]),
        new ShortcutGroup("Note entry (anywhere except a text box, e.g. the API key field)", [
            new("A – G", "Enter a note (octave nearest the previous note)"),
            new("Shift+A – G", "Add a pitch to the selected note (chord entry)"),
            new("1 – 6", "Set duration: whole, half, quarter, eighth, 16th, 32nd"),
            new("R", "Toggle rest"),
            new(".", "Toggle dot"),
            new("Delete / Backspace", "Delete the selected note"),
        ]),
        new ShortcutGroup("Playback", [
            new("Space", "Play / Pause"),
            new("Escape", "Stop"),
            new("L", "Toggle loop"),
        ]),
    ];

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    public event EventHandler? CloseRequested;
}
