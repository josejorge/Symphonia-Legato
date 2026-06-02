using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SymphoniaLegato.Android.ViewModels;
using SymphoniaLegato.Android.Views;
using SymphoniaLegato.ImportExport;
using SymphoniaLegato.LayoutEngine;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.PlaybackEngine;

namespace SymphoniaLegato.Android;

public sealed class App : Application
{
    private IServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        _services = BuildServices();

        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainShellView
            {
                DataContext = _services.GetRequiredService<AndroidMainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider BuildServices()
    {
        var sc = new ServiceCollection();

        sc.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));

        sc.AddSingleton<ILayoutEngine, LayoutEngine>();
        sc.AddSingleton<MusicXmlImporter>();
        sc.AddSingleton<EnScoreRepository>();
        sc.AddSingleton<IScoreRepository>(sp => sp.GetRequiredService<EnScoreRepository>());
        sc.AddSingleton<MetronomeEngine>();

        sc.AddSingleton<AndroidMainViewModel>();
        sc.AddSingleton<ScoreViewerViewModel>();
        sc.AddSingleton<MobilePlaybackViewModel>();
        sc.AddSingleton<MobileMetronomeViewModel>();
        sc.AddSingleton<AnnotationViewModel>();
        sc.AddSingleton<FileBrowserViewModel>();

        return sc.BuildServiceProvider();
    }
}
