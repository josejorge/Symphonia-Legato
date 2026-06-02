using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.PluginEngine;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed class PluginInfo
{
    public string Name    { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Author  { get; init; } = string.Empty;
    public Guid   Id      { get; init; }
    public bool   IsLoaded { get; init; } = true;
}

public sealed partial class PluginManagerViewModel : ViewModelBase
{
    private readonly PluginHost _host;

    [ObservableProperty] private string _pluginDirectory;
    [ObservableProperty] private string _statusText = "No plugins loaded";
    [ObservableProperty] private PluginInfo? _selectedPlugin;

    public ObservableCollection<PluginInfo> Plugins { get; } = [];

    public event EventHandler? CloseRequested;

    public PluginManagerViewModel(PluginHost host)
    {
        _host = host;
        _pluginDirectory = Path.Combine(AppContext.BaseDirectory, "plugins");
        Refresh();
    }

    [RelayCommand]
    private async Task LoadPluginsAsync()
    {
        Directory.CreateDirectory(PluginDirectory);
        await _host.LoadPluginsAsync(PluginDirectory);
        Refresh();
        StatusText = $"Loaded {Plugins.Count} plugin(s)";
    }

    [RelayCommand]
    private async Task UnloadSelectedAsync()
    {
        if (SelectedPlugin is null) return;
        await _host.UnloadPluginAsync(SelectedPlugin.Id);
        Refresh();
        StatusText = "Plugin unloaded";
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void Refresh()
    {
        Plugins.Clear();
        foreach (var p in _host.LoadedPlugins)
            Plugins.Add(new PluginInfo
            {
                Name    = p.Name,
                Version = p.Version,
                Author  = p.Author,
                Id      = p.PluginId,
                IsLoaded = true
            });

        StatusText = Plugins.Count == 0
            ? "No plugins loaded — place .dll files in the plugins folder"
            : $"{Plugins.Count} plugin(s) loaded";
    }
}
