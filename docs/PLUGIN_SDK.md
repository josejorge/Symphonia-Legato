# Plugin SDK

## Overview

Plugins are .NET 9 assemblies placed in the `plugins/` subdirectory of the Symphonia Legato application folder. The host discovers them at startup and loads each class that implements `IPlugin`.

---

## Minimal Plugin

```csharp
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

[assembly: AssemblyTitle("My Symphonia Plugin")]

public sealed class MyPlugin : IPlugin
{
    public Guid PluginId => new("XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX");
    public string Name => "My Plugin";
    public string Author => "Your Name";
    public string Version => "1.0.0";
    public string Description => "Does something useful.";
    public IReadOnlyList<PluginCapability> Capabilities => [PluginCapability.Instrument];

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        context.RegisterInstrument(new Instrument
        {
            Name = "My Synth",
            ShortName = "Syn.",
            MidiProgram = 81,
            Family = InstrumentFamily.Other
        });
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct) => Task.CompletedTask;
}
```

---

## IPluginContext API

| Member | Description |
|---|---|
| `ActiveScore` | The currently open score (read-only) |
| `RegisterInstrument(Instrument)` | Add a custom instrument to the instrument list |
| `RegisterExporter(IScoreExporter)` | Add a custom export format |
| `ShowMessage(string)` | Display a notification to the user |
| `Services` | Access registered application services |

---

## Custom Exporter

```csharp
public sealed class MyExporter : IScoreExporter
{
    public ExportFormat Format => ExportFormat.MusicXml; // use ExportFormat.Custom when available
    public string DefaultFileExtension => ".myformat";

    public Task ExportAsync(Score score, string filePath,
        ExportOptions? options, CancellationToken ct)
    {
        // Write score to filePath in your format
        return Task.CompletedTask;
    }

    public Task ExportToStreamAsync(Score score, Stream stream,
        ExportOptions? options, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
```

---

## Plugin Directory

```
AppDir/
└── plugins/
    ├── MyPlugin.dll
    └── MyPlugin.deps.json
```

All dependency assemblies must be present alongside the plugin DLL.

---

## Sandboxing

Plugins run in the same process but with limited API surface via `IPluginContext`. Direct access to internal score mutation is not permitted; plugins should request operations through the context interface. Full sandboxing (separate AppDomain/process) is planned for Phase 3.
