// File: BoolToStartStopConverter.cs
// Description: Converts a bool to a Start/Stop label (also hosts BoolToColorConverter for beat-accent highlighting).
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-02
// Last edit date: 2026-09-15
// Version: 1.0.0

using Avalonia.Data.Converters;
using System.Globalization;

namespace SymphoniaLegato.Desktop.Converters;

public sealed class BoolToStartStopConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "■ Stop" : "▶ Start";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Returns a SkiaSharp/Avalonia color string for beat-accent highlighting.</summary>
public static class BoolToColorConverter
{
    // Used as a static resource in AXAML for accent beat color
    public static readonly Avalonia.Data.Converters.FuncValueConverter<bool, Avalonia.Media.Color> AccentToGold =
        new(isAccent => isAccent
            ? Avalonia.Media.Color.FromRgb(0xE0, 0xA0, 0x10)   // gold
            : Avalonia.Media.Color.FromRgb(0x3A, 0x3A, 0x3A));  // card bg
}
