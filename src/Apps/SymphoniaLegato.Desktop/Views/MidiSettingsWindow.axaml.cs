using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SymphoniaLegato.Desktop.ViewModels;

namespace SymphoniaLegato.Desktop.Views;

public sealed partial class MidiSettingsWindow : Window
{
    public MidiSettingsWindow() => InitializeComponent();

    public MidiSettingsWindow(MidiSettingsViewModel vm) : this()
    {
        DataContext = vm;
        vm.CloseRequested += (_, _) => Close();
        vm.SoundFontBrowseRequested += OnSoundFontBrowse;
    }

    private async void OnSoundFontBrowse(object? sender, EventArgs e)
    {
        var vm = (MidiSettingsViewModel)DataContext!;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select SoundFont File",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("SoundFont 2") { Patterns = ["*.sf2", "*.sf3"] }]
        });

        if (files.Count > 0)
            vm.SoundFontPath = files[0].Path.LocalPath;
    }
}
