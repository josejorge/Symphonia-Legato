using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SymphoniaLegato.Desktop.ViewModels;
using SymphoniaLegato.Desktop.Views;

namespace SymphoniaLegato.Desktop;

public sealed class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services) => _services = services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainWindowViewModel>()
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
