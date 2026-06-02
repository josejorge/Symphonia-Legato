using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Core.Interfaces;

/// <summary>Category of plugin capability.</summary>
public enum PluginCapability
{
    Instrument, ExportFormat, NotationSymbol, AiFeature, Tool
}

/// <summary>Descriptor returned by every plugin.</summary>
public interface IPluginDescriptor
{
    Guid PluginId { get; }
    string Name { get; }
    string Author { get; }
    string Version { get; }
    string Description { get; }
    IReadOnlyList<PluginCapability> Capabilities { get; }
}

/// <summary>Entry point that every plugin assembly must implement.</summary>
public interface IPlugin : IPluginDescriptor
{
    Task InitializeAsync(IPluginContext context, CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);
}

/// <summary>Services exposed to plugins (sandboxed subset of the host API).</summary>
public interface IPluginContext
{
    Score? ActiveScore { get; }
    void RegisterInstrument(Instrument instrument);
    void RegisterExporter(IScoreExporter exporter);
    void ShowMessage(string message);
    IServiceProvider Services { get; }
}

/// <summary>Manages plugin discovery, loading, and lifecycle.</summary>
public interface IPluginHost
{
    IReadOnlyList<IPlugin> LoadedPlugins { get; }
    Task LoadPluginsAsync(string pluginDirectory, CancellationToken ct = default);
    Task UnloadPluginAsync(Guid pluginId, CancellationToken ct = default);
}
