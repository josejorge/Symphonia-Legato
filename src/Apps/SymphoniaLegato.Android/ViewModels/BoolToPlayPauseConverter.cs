using Avalonia.Data.Converters;
using System.Globalization;

namespace SymphoniaLegato.Android.ViewModels;

public sealed class BoolToPlayPauseConverter : IValueConverter
{
    public static readonly BoolToPlayPauseConverter Instance = new();

    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is true ? "⏸" : "▶";

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}
