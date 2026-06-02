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
