using Android.App;
using Android.Runtime;

namespace SymphoniaLegato.Android;

[Application]
public sealed class MainApplication(IntPtr handle, JniHandleOwnership ownership)
    : Application(handle, ownership);
