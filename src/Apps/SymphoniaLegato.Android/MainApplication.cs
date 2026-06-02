using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace SymphoniaLegato.Android;

[Application]
public sealed class MainApplication(IntPtr handle, JniHandleOwnership ownership)
    : AvaloniaAndroidApplication<App>(handle, ownership)
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder)
            .WithInterFont();
}
