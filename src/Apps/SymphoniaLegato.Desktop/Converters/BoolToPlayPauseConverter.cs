// File: BoolToPlayPauseConverter.cs
// Description: Converts an IsPlaying bool to a Play or Pause symbol.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-01
// Last edit date: 2026-09-15
// Version: 1.0.0

using System.Globalization;
using Avalonia.Data.Converters;

namespace SymphoniaLegato.Desktop.Converters;

/// <summary>Converts IsPlaying bool to ▶ or ⏸ symbol.</summary>
public sealed class BoolToPlayPauseConverter : IValueConverter
{
    public static readonly BoolToPlayPauseConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "⏸" : "▶"; // ⏸ or ▶

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
