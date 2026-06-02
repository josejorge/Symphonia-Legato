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

namespace SymphoniaLegato.Desktop;

public sealed class App : Application
{
    private readonly IServiceProvider _services;
    private StyleInclude? _highContrastStyle;

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
                _services.GetRequiredService<MusicXmlExporter>())
            {
                DataContext = mainVm
            };
        }
        base.OnFrameworkInitializationCompleted();
    }

    public void SetHighContrast(bool on)
    {
        if (on)
        {
            _highContrastStyle ??= new StyleInclude(
                new Uri("avares://SymphoniaLegato.Desktop/"))
            {
                Source = new Uri("avares://SymphoniaLegato.Desktop/Themes/HighContrastTheme.axaml")
            };

            if (!Styles.Contains(_highContrastStyle))
                Styles.Add(_highContrastStyle);
        }
        else
        {
            if (_highContrastStyle is not null && Styles.Contains(_highContrastStyle))
                Styles.Remove(_highContrastStyle);
        }
    }
}
