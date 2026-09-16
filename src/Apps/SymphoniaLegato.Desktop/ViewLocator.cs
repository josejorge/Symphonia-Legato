// File: ViewLocator.cs
// Description: Maps ViewModel types to their View counterparts by naming convention.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-01
// Last edit date: 2026-09-15
// Version: 1.0.0

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using SymphoniaLegato.Desktop.ViewModels;

namespace SymphoniaLegato.Desktop;

/// <summary>Maps ViewModel types to their View counterparts by convention.</summary>
public sealed class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null) return null;
        string name = param.GetType().FullName!.Replace("ViewModel", "View");
        var type = System.Type.GetType(name);
        return type is not null
            ? (Control)Activator.CreateInstance(type)!
            : new TextBlock { Text = $"View not found: {name}" };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
