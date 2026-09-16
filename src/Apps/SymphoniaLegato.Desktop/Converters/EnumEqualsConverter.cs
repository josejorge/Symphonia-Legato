// File: EnumEqualsConverter.cs
// Description: Value converter that compares a bound value to ConverterParameter for equality,
//   used to drive RadioButton.IsChecked from a single VM property instead of one bool per option.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-09-15
// Last edit date: 2026-09-16
// Version: 1.0.1

using System.Globalization;
using Avalonia.Data.Converters;

namespace SymphoniaLegato.Desktop.Converters;

/// <summary>Compares a bound value to <c>ConverterParameter</c> for equality; used to drive
/// RadioButton.IsChecked from a single VM property (e.g. SelectedDuration.Value) instead of
/// duplicating one bool per option.</summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    public static readonly EnumEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Equals(value, parameter);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? parameter : Avalonia.Data.BindingOperations.DoNothing;
}
