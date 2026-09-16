// File: MainActivity.cs
// Description: The Android app's single Activity, hosting the Avalonia UI via AvaloniaMainActivity.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-02
// Last edit date: 2026-09-15
// Version: 1.0.0

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
