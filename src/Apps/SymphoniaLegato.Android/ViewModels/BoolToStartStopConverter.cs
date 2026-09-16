// File: BoolToStartStopConverter.cs
// Description: Converts a bool to a Start/Stop label (Android copy of the Desktop converter).
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-02
// Last edit date: 2026-09-15
// Version: 1.0.0

using Avalonia.Data.Converters;
using System.Globalization;

namespace SymphoniaLegato.Android.ViewModels;

public sealed class BoolToStartStopConverter : IValueConverter
{
    public static readonly BoolToStartStopConverter Instance = new();

    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is true ? "■ Stop" : "▶ Start";

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}
