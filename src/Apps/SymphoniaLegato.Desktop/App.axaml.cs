// File: App.axaml.cs
// Description: Application entry — receives the DI ServiceProvider, constructs MainWindow with
//   its resolved dependencies, and switches between Dark/Light/HighContrast style includes.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-09-15
// Last edit date: 2026-09-15
// Version: 1.2.0

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using SymphoniaLegato.Desktop.Services;
using SymphoniaLegato.Desktop.ViewModels;
using SymphoniaLegato.Desktop.Views;
using SymphoniaLegato.ImportExport;
using SymphoniaLegato.PdfEngine;
using SymphoniaLegato.PlaybackEngine;

namespace SymphoniaLegato.Desktop;

public sealed class App : Application
{
    private readonly IServiceProvider _services;
    private StyleInclude? _highContrastStyle;
    private StyleInclude? _lightStyle;

    public App(IServiceProvider services) => _services = services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = _services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow(
                _services.GetRequiredService<ScorePngExporter>(),
                _services.GetRequiredService<ScorePdfExporter>(),
                _services.GetRequiredService<ScoreSvgExporter>(),
                _services.GetRequiredService<MusicXmlExporter>(),
                _services.GetRequiredService<ScoreToMidiConverter>())
            {
                DataContext = mainVm
            };
        }
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Switches the active theme. Dark is the base layer (SymphoniaTheme.axaml,
    /// always loaded from App.axaml) — Light and HighContrast are alternate overlays that
    /// replace every one of its DynamicResource keys, so only one overlay (or none, for
    /// Dark) is ever active at a time.</summary>
    public void SetTheme(AppTheme theme)
    {
        _highContrastStyle ??= MakeStyleInclude("Themes/HighContrastTheme.axaml");
        _lightStyle        ??= MakeStyleInclude("Themes/LightTheme.axaml");

        SetOverlay(_highContrastStyle, active: theme == AppTheme.HighContrast);
        SetOverlay(_lightStyle,        active: theme == AppTheme.Light);
    }

    private static StyleInclude MakeStyleInclude(string relativePath) =>
        new(new Uri("avares://SymphoniaLegato.Desktop/"))
        {
            Source = new Uri($"avares://SymphoniaLegato.Desktop/{relativePath}")
        };

    private void SetOverlay(StyleInclude overlay, bool active)
    {
        bool present = Styles.Contains(overlay);
        if (active && !present) Styles.Add(overlay);
        else if (!active && present) Styles.Remove(overlay);
    }
}
