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
