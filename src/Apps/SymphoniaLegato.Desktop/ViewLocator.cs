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
