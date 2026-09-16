// File: SyncSettingsWindow.axaml.cs
// Description: Code-behind for the Cloud Sync Settings dialog.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-02
// Last edit date: 2026-09-15
// Version: 1.0.0

using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SymphoniaLegato.Desktop.ViewModels;

namespace SymphoniaLegato.Desktop.Views;

public sealed partial class SyncSettingsWindow : Window
{
    public SyncSettingsWindow() => InitializeComponent();

    public SyncSettingsWindow(SyncSettingsViewModel vm) : this()
    {
        DataContext = vm;
        vm.CloseRequested             += (_, _) => Close();
        vm.BrowseSyncFolderRequested  += OnBrowseSyncFolder;
        vm.PullScoreRequested         += OnPullScore;
    }

    private async void OnBrowseSyncFolder(object? sender, EventArgs e)
    {
        var vm = (SyncSettingsViewModel)DataContext!;
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Cloud Sync Folder",
            AllowMultiple = false
        });
        if (folders.Count > 0)
        {
            vm.SyncFolder = folders[0].Path.LocalPath;
            vm.RefreshRemoteListCommand.Execute(null);
        }
    }

    private void OnPullScore(object? sender, string remotePath)
    {
        // Notify the main window to open the pulled file
        Close();
    }
}
