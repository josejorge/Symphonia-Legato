using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace SymphoniaLegato.Android;

[Activity(
    Label = "Symphonia Legato",
    Theme = "@style/Theme.AppCompat.NoActionBar",
    MainLauncher = true,
    ConfigurationChanges =
        ConfigChanges.Orientation | ConfigChanges.ScreenSize |
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize |
        ConfigChanges.Locale | ConfigChanges.FontScale |
        ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
public sealed class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder)
            .WithInterFont();
}
