using Avalonia.Data.Converters;
using System.Globalization;

namespace SymphoniaLegato.Android.ViewModels;

/// <summary>Returns true when the bound int equals the converter parameter.</summary>
public sealed class IndexEqualsConverter(int index) : IValueConverter
{
    public static readonly IndexEqualsConverter Zero  = new(0);
    public static readonly IndexEqualsConverter One   = new(1);
    public static readonly IndexEqualsConverter Two   = new(2);
    public static readonly IndexEqualsConverter Three = new(3);

    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is int i && i == index;

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotSupportedException();
}
