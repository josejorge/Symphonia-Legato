using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SymphoniaLegato.Desktop;
using SymphoniaLegato.Desktop.Services;
using SymphoniaLegato.Desktop.ViewModels;
using SymphoniaLegato.GitIntegration;
using SymphoniaLegato.ImportExport;
using SymphoniaLegato.LayoutEngine;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.PdfEngine;
using SymphoniaLegato.PlaybackEngine;
using SymphoniaLegato.PluginEngine;

var services = BuildServices();

AppBuilder
    .Configure(() => new App(services))
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);

static IServiceProvider BuildServices()
{
    var sc = new ServiceCollection();

    sc.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));

    // ── Playback ──────────────────────────────────────────────────
    sc.AddSingleton<ScoreToMidiConverter>();
    sc.AddSingleton<MidiPlaybackEngine>();
    sc.AddSingleton<IPlaybackEngine>(sp => sp.GetRequiredService<MidiPlaybackEngine>());

    // ── Layout ────────────────────────────────────────────────────
    sc.AddSingleton<ILayoutEngine, LayoutEngine>();

    // ── Import / Export ───────────────────────────────────────────
    sc.AddSingleton<MusicXmlImporter>();
    sc.AddSingleton<MusicXmlExporter>();
    sc.AddSingleton<MidiImporter>();
    sc.AddSingleton<ScoreSvgExporter>();
    sc.AddSingleton<EnScoreRepository>();
    sc.AddSingleton<IScoreRepository>(sp => sp.GetRequiredService<EnScoreRepository>());

    // ── PDF / PNG ─────────────────────────────────────────────────
    sc.AddSingleton<ScorePdfExporter>();
    sc.AddSingleton<ScorePngExporter>();

    // ── Phase 4: Metronome + Sync ─────────────────────────────────
    sc.AddSingleton<MetronomeEngine>();
    sc.AddSingleton<ScoreSyncService>();

    // ── Plugin host ───────────────────────────────────────────────
    sc.AddSingleton<PluginHost>();

    // ── Git integration ───────────────────────────────────────────
    sc.AddSingleton<ScoreVersionControl>();

    // ── ViewModels ────────────────────────────────────────────────
    sc.AddSingleton<AboutViewModel>();
    sc.AddSingleton<ScorePropertiesViewModel>();
    sc.AddSingleton<GitHistoryViewModel>();
    sc.AddSingleton<PluginManagerViewModel>();
    sc.AddSingleton<MidiSettingsViewModel>();
    sc.AddSingleton<SyncSettingsViewModel>();
    sc.AddSingleton<MetronomeViewModel>();
    sc.AddTransient<MainWindowViewModel>();
    sc.AddTransient<ScoreEditorViewModel>();
    sc.AddTransient<PlaybackViewModel>();
    sc.AddTransient<MixerViewModel>();
    sc.AddTransient<PianoKeyboardViewModel>();

    return sc.BuildServiceProvider();
}
