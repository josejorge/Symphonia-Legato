using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using SymphoniaLegato.Desktop.Services;
using SymphoniaLegato.Desktop.ViewModels;
using SymphoniaLegato.ImportExport;
using SymphoniaLegato.PdfEngine;

namespace SymphoniaLegato.Desktop.Views;

public sealed partial class MainWindow : Window
{
    private readonly ScorePngExporter? _pngExporter;
    private readonly ScorePdfExporter? _pdfExporter;
    private readonly ScoreSvgExporter? _svgExporter;
    private readonly SymphoniaLegato.ImportExport.MusicXmlExporter? _xmlExporter;

    public MainWindow() => InitializeComponent();

    public MainWindow(
        ScorePngExporter pngExporter,
        ScorePdfExporter pdfExporter,
        ScoreSvgExporter svgExporter,
        MusicXmlExporter xmlExporter) : this()
    {
        _pngExporter = pngExporter;
        _pdfExporter = pdfExporter;
        _svgExporter = svgExporter;
        _xmlExporter = xmlExporter;

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        vm.ExitRequested            += (_, _)         => Close();
        vm.AboutRequested           += (_, aboutVm)  => new AboutWindow(aboutVm).ShowDialog(this);
        vm.ScorePropertiesRequested += (_, propsVm)  => new ScorePropertiesWindow(propsVm).ShowDialog(this);
        vm.PluginManagerRequested   += (_, pluginVm) => new PluginManagerWindow(pluginVm).ShowDialog(this);
        vm.MidiSettingsRequested    += (_, midiVm)   => new MidiSettingsWindow(midiVm).ShowDialog(this);
        vm.SyncSettingsRequested    += (_, syncVm)   => new SyncSettingsWindow(syncVm).ShowDialog(this);
        vm.OpenFileRequested        += async (_, __) => await OnOpenFileAsync(vm);
        vm.SaveAsRequested          += async (_, __) => await OnSaveAsAsync(vm);
        vm.ExportRequested          += async (_, fmt) => await OnExportAsync(vm, fmt);
        vm.ThemeChangeRequested     += OnThemeChange;
    }

    private async Task OnOpenFileAsync(MainWindowViewModel vm)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Score",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Symphonia Score") { Patterns = ["*.enscore"] },
                new FilePickerFileType("MusicXML")        { Patterns = ["*.xml", "*.musicxml", "*.mxl"] },
                new FilePickerFileType("All files")        { Patterns = ["*.*"] }
            ]
        });

        if (files.Count > 0)
            await vm.OpenFromPathAsync(files[0].Path.LocalPath);
    }

    private async Task OnSaveAsAsync(MainWindowViewModel vm)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Score As",
            DefaultExtension = ".enscore",
            FileTypeChoices =
            [
                new FilePickerFileType("Symphonia Score") { Patterns = ["*.enscore"] }
            ]
        });

        if (file is not null)
            await vm.SaveToPathAsync(file.Path.LocalPath);
    }

    private async Task OnExportAsync(MainWindowViewModel vm, string format)
    {
        var score = vm.CurrentScore;
        if (score is null) return;

        switch (format)
        {
            case "png":
                await ExportPngAsync(score, vm);
                break;

            case "pdf":
                await ExportPdfAsync(score, vm);
                break;

            case "svg":
                await ExportSvgAsync(score, vm);
                break;

            case "musicxml":
                await ExportMusicXmlAsync(score, vm);
                break;

            case "midi":
                vm.StatusMessage = "MIDI export: use File > Export > MIDI from playback controls.";
                break;
        }
    }

    private async Task ExportPngAsync(SymphoniaLegato.Core.Models.Score score, MainWindowViewModel vm)
    {
        if (_pngExporter is null) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export PNG",
            DefaultExtension = ".png",
            FileTypeChoices = [new FilePickerFileType("PNG Image") { Patterns = ["*.png"] }]
        });
        if (file is null) return;

        vm.StatusMessage = "Rendering PNG…";
        try
        {
            var pages = _pngExporter.ExportAllPages(score, dpi: 150, zoom: 1.0);
            string dir = Path.GetDirectoryName(file.Path.LocalPath)!;
            string name = Path.GetFileNameWithoutExtension(file.Path.LocalPath);

            if (pages.Count == 1)
            {
                await File.WriteAllBytesAsync(file.Path.LocalPath, pages[0]);
            }
            else
            {
                for (int i = 0; i < pages.Count; i++)
                    await File.WriteAllBytesAsync(Path.Combine(dir, $"{name}_p{i + 1}.png"), pages[i]);
            }
            vm.StatusMessage = $"Exported {pages.Count} PNG page(s)";
        }
        catch (Exception ex)
        {
            vm.StatusMessage = $"PNG export failed: {ex.Message}";
        }
    }

    private async Task ExportPdfAsync(SymphoniaLegato.Core.Models.Score score, MainWindowViewModel vm)
    {
        if (_pngExporter is null || _pdfExporter is null) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export PDF",
            DefaultExtension = ".pdf",
            FileTypeChoices = [new FilePickerFileType("PDF Document") { Patterns = ["*.pdf"] }]
        });
        if (file is null) return;

        vm.StatusMessage = "Generating PDF…";
        try
        {
            var pages = _pngExporter.ExportAllPages(score, dpi: 150);
            var pdfBytes = _pdfExporter.GenerateFromImages(score, pages);
            await File.WriteAllBytesAsync(file.Path.LocalPath, pdfBytes);
            vm.StatusMessage = $"PDF exported ({pages.Count} page(s))";
        }
        catch (Exception ex)
        {
            vm.StatusMessage = $"PDF export failed: {ex.Message}";
        }
    }

    private async Task ExportSvgAsync(SymphoniaLegato.Core.Models.Score score, MainWindowViewModel vm)
    {
        if (_svgExporter is null) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export SVG",
            DefaultExtension = ".svg",
            FileTypeChoices = [new FilePickerFileType("SVG Vector") { Patterns = ["*.svg"] }]
        });
        if (file is null) return;

        vm.StatusMessage = "Generating SVG…";
        try
        {
            string svg = _svgExporter.ExportPage(score);
            await File.WriteAllTextAsync(file.Path.LocalPath, svg);
            vm.StatusMessage = "SVG exported";
        }
        catch (Exception ex)
        {
            vm.StatusMessage = $"SVG export failed: {ex.Message}";
        }
    }

    private async Task ExportMusicXmlAsync(SymphoniaLegato.Core.Models.Score score, MainWindowViewModel vm)
    {
        if (_xmlExporter is null) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export MusicXML",
            DefaultExtension = ".xml",
            FileTypeChoices = [new FilePickerFileType("MusicXML") { Patterns = ["*.xml", "*.musicxml"] }]
        });
        if (file is null) return;

        try
        {
            await _xmlExporter.ExportAsync(score, file.Path.LocalPath);
            vm.StatusMessage = "MusicXML exported";
        }
        catch (Exception ex)
        {
            vm.StatusMessage = $"MusicXML export failed: {ex.Message}";
        }
    }

    private void OnThemeChange(object? sender, bool isHighContrast)
    {
        if (Avalonia.Application.Current is App app)
            app.SetHighContrast(isHighContrast);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.None)
            e.Handled = true;
    }
}
