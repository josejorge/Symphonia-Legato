using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SymphoniaLegato.Desktop;
using SymphoniaLegato.Desktop.ViewModels;
using SymphoniaLegato.ImportExport;
using SymphoniaLegato.LayoutEngine;
using SymphoniaLegato.PlaybackEngine;

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

    sc.AddSingleton<ScoreToMidiConverter>();
    sc.AddSingleton<MidiPlaybackEngine>();
    sc.AddSingleton<SymphoniaLegato.Core.Interfaces.IPlaybackEngine>(
        sp => sp.GetRequiredService<MidiPlaybackEngine>());

    sc.AddSingleton<SymphoniaLegato.Core.Interfaces.ILayoutEngine, LayoutEngine>();

    sc.AddSingleton<MusicXmlImporter>();
    sc.AddSingleton<MusicXmlExporter>();
    sc.AddSingleton<EnScoreRepository>();
    sc.AddSingleton<SymphoniaLegato.Core.Interfaces.IScoreRepository>(
        sp => sp.GetRequiredService<EnScoreRepository>());

    sc.AddTransient<MainWindowViewModel>();
    sc.AddTransient<ScoreEditorViewModel>();
    sc.AddTransient<PlaybackViewModel>();
    sc.AddTransient<MixerViewModel>();
    sc.AddTransient<PianoKeyboardViewModel>();

    return sc.BuildServiceProvider();
}
