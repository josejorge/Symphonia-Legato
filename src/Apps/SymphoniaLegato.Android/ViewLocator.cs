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
