// File: BoolToPlayPauseConverter.cs
// Description: Converts an IsPlaying bool to a Play or Pause symbol (Android copy of the Desktop converter).
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-02
// Last edit date: 2026-09-15
// Version: 1.0.0

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
