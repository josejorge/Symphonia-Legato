// File: ViewLocator.cs
// Description: Maps ViewModel types to their View counterparts by naming convention (Android).
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-02
// Last edit date: 2026-09-15
// Version: 1.0.0

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using SymphoniaLegato.Android.ViewModels;

namespace SymphoniaLegato.Android;

public sealed class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null) return null;
        string name = data.GetType().FullName!.Replace("ViewModel", "View")
                          .Replace(".ViewModels.", ".Views.");
        var type = Type.GetType(name);
        return type is not null
            ? (Control)Activator.CreateInstance(type)!
            : new TextBlock { Text = $"View not found: {name}" };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
