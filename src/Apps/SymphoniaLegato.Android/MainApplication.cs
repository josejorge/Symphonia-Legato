// File: MainApplication.cs
// Description: Android Application subclass required by the Android runtime; initializes the Avalonia/Android integration.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-02
// Last edit date: 2026-09-15
// Version: 1.0.0

using Android.App;
using Android.Runtime;

namespace SymphoniaLegato.Android;

[Application]
public sealed class MainApplication(IntPtr handle, JniHandleOwnership ownership)
    : Application(handle, ownership);
