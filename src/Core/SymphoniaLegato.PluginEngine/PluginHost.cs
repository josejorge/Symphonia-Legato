using System.Reflection;
using Microsoft.Extensions.Logging;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.PluginEngine;

/// <summary>
/// Discovers, loads, and manages plugin lifecycle.
/// Plugins are .NET assemblies placed in the plugins/ directory.
/// Each must export a public class implementing <see cref="IPlugin"/>.
/// </summary>
public sealed class PluginHost : IPluginHost
{
    private readonly ILogger<PluginHost> _logger;
    private readonly IServiceProvider _services;
    private readonly List<IPlugin> _plugins = [];
    private readonly PluginContext _context;

    public IReadOnlyList<IPlugin> LoadedPlugins => _plugins;

    public PluginHost(ILogger<PluginHost> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
        _context = new PluginContext(services);
    }

    public async Task LoadPluginsAsync(string pluginDirectory, CancellationToken ct = default)
    {
        if (!Directory.Exists(pluginDirectory))
        {
            _logger.LogDebug("Plugin directory not found: {Dir}", pluginDirectory);
            return;
        }

        foreach (var dll in Directory.GetFiles(pluginDirectory, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(IPlugin)));

                foreach (var type in pluginTypes)
                {
                    if (Activator.CreateInstance(type) is not IPlugin plugin) continue;
                    await plugin.InitializeAsync(_context, ct);
                    _plugins.Add(plugin);
                    _logger.LogInformation("Loaded plugin: {Name} v{Version}", plugin.Name, plugin.Version);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load plugin from {Dll}", dll);
            }
        }
    }

    public async Task UnloadPluginAsync(Guid pluginId, CancellationToken ct = default)
    {
        var plugin = _plugins.FirstOrDefault(p => p.PluginId == pluginId);
        if (plugin is null) return;

        await plugin.ShutdownAsync(ct);
        _plugins.Remove(plugin);
        _logger.LogInformation("Unloaded plugin: {Name}", plugin.Name);
    }
}

internal sealed class PluginContext : IPluginContext
{
    private readonly IServiceProvider _services;
    private readonly List<Instrument> _registeredInstruments = [];
    private readonly List<IScoreExporter> _registeredExporters = [];

    public Score? ActiveScore { get; set; }
    public IServiceProvider Services => _services;

    public PluginContext(IServiceProvider services) => _services = services;

    public void RegisterInstrument(Instrument instrument) =>
        _registeredInstruments.Add(instrument);

    public void RegisterExporter(IScoreExporter exporter) =>
        _registeredExporters.Add(exporter);

    public void ShowMessage(string message) =>
        // Hooked by desktop app via event / service
        Console.WriteLine($"[Plugin] {message}");
}
