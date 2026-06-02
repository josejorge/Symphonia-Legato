using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class AboutViewModel : ViewModelBase
{
    public string AppName    => "Symphonia Legato";
    public string Version    => Assembly.GetExecutingAssembly()
                                        .GetName().Version?.ToString(3) ?? "1.0.0";
    public string Author     => "Jose Jorge Hernandez";
    public string License    => "MIT License";
    public string Copyright  => $"© 2026 {Author}";
    public string Website    => "https://github.com/josejorgehz/symphonia-legato";
    public string Description =>
        "A modern, open-source music notation editor inspired by the classic Encore application.\n" +
        "Piano-first, lightweight, and git-friendly — built for composers, educators, and hobbyists.";

    public string TechStack =>
        "C# (.NET 9)  •  Avalonia UI 11  •  CommunityToolkit.Mvvm\n" +
        "DryWetMidi  •  QuestPDF  •  LibGit2Sharp  •  MusicXML 4.0";

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    public event EventHandler? CloseRequested;
}
