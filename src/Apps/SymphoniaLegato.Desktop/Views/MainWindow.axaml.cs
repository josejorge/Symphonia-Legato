// File: MainWindow.axaml.cs
// Description: Main window code-behind — wires ViewModel events (open/save/export requests) to
//   Avalonia file pickers and dialogs, and drives the per-format export pipeline.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-09-15
// Last edit date: 2026-09-15
// Version: 1.4.0

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.Desktop.Services;
using SymphoniaLegato.Desktop.ViewModels;
using SymphoniaLegato.ImportExport;
using SymphoniaLegato.PdfEngine;
using SymphoniaLegato.PlaybackEngine;

namespace SymphoniaLegato.Desktop.Views;

public sealed partial class MainWindow : Window
{
    private readonly ScorePngExporter? _pngExporter;
    private readonly ScorePdfExporter? _pdfExporter;
    private readonly ScoreSvgExporter? _svgExporter;
    private readonly SymphoniaLegato.ImportExport.MusicXmlExporter? _xmlExporter;
    private readonly ScoreToMidiConverter? _midiConverter;

    public MainWindow() => InitializeComponent();

    public MainWindow(
        ScorePngExporter pngExporter,
        ScorePdfExporter pdfExporter,
        ScoreSvgExporter svgExporter,
        MusicXmlExporter xmlExporter,
        ScoreToMidiConverter midiConverter) : this()
    {
        _pngExporter = pngExporter;
        _pdfExporter = pdfExporter;
        _svgExporter = svgExporter;
        _xmlExporter = xmlExporter;
        _midiConverter = midiConverter;

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        vm.ExitRequested            += (_, _)         => Close();
        vm.AboutRequested           += (_, aboutVm)  => new AboutWindow(aboutVm).ShowDialog(this);
        vm.ShortcutsRequested       += (_, shortcutsVm) => new ShortcutsWindow(shortcutsVm).ShowDialog(this);
        vm.ScorePropertiesRequested += (_, propsVm)  => new ScorePropertiesWindow(propsVm).ShowDialog(this);
        vm.PluginManagerRequested   += (_, pluginVm) => new PluginManagerWindow(pluginVm).ShowDialog(this);
        vm.MidiSettingsRequested    += (_, midiVm)   => new MidiSettingsWindow(midiVm).ShowDialog(this);
        vm.SyncSettingsRequested    += (_, syncVm)   => new SyncSettingsWindow(syncVm).ShowDialog(this);
        vm.OpenFileRequested        += async (_, __) => await OnOpenFileAsync(vm);
        vm.SaveAsRequested          += async (_, __) => await OnSaveAsAsync(vm);
        vm.ExportRequested          += async (_, fmt) => await OnExportAsync(vm, fmt);
        vm.ThemeChangeRequested     += OnThemeChange;

        // Apply the theme MainWindowViewModel already restored from settings into
        // CurrentTheme — it couldn't raise ThemeChangeRequested for this before now,
        // since nothing was subscribed yet during its own construction.
        OnThemeChange(vm, vm.CurrentTheme);
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
                await ExportMidiAsync(score, vm);
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

    private async Task ExportMidiAsync(SymphoniaLegato.Core.Models.Score score, MainWindowViewModel vm)
    {
        if (_midiConverter is null) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export MIDI",
            DefaultExtension = ".mid",
            FileTypeChoices = [new FilePickerFileType("MIDI File") { Patterns = ["*.mid", "*.midi"] }]
        });
        if (file is null) return;

        try
        {
            var midiFile = _midiConverter.Convert(score, includeMetronome: false);
            midiFile.Write(file.Path.LocalPath, overwriteFile: true);
            vm.StatusMessage = "MIDI exported";
        }
        catch (Exception ex)
        {
            vm.StatusMessage = $"MIDI export failed: {ex.Message}";
        }
    }

    private void OnThemeChange(object? sender, AppTheme theme)
    {
        if (Avalonia.Application.Current is App app)
            app.SetTheme(theme);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled) return;
        // Leave menu accelerators (Ctrl/Alt) and dialogs alone.
        if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) != 0) return;
        if (DataContext is not MainWindowViewModel vm || vm.ScoreEditor is null) return;
        // Don't steal typing from text inputs (e.g. the AI API-key field).
        if (FocusManager?.GetFocusedElement() is TextBox) return;

        var editor = vm.ScoreEditor;
        bool chordEntry = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        void NoteKey(NoteName name)
        {
            if (chordEntry) editor.AddPitchToSelectedNote(name);
            else editor.EnterNoteByName(name);
        }

        // Letter note entry — chooses the octave nearest the previous note. Holding
        // Shift stacks the pitch onto the currently selected note (chord entry)
        // instead of creating a new note.
        switch (e.Key)
        {
            case Key.A: NoteKey(NoteName.A); break;
            case Key.B: NoteKey(NoteName.B); break;
            case Key.C: NoteKey(NoteName.C); break;
            case Key.D: NoteKey(NoteName.D); break;
            case Key.E: NoteKey(NoteName.E); break;
            case Key.F: NoteKey(NoteName.F); break;
            case Key.G: NoteKey(NoteName.G); break;

            // Duration selection (whole … 32nd).
            case Key.D1: case Key.NumPad1: editor.SetDurationByIndex(0); break;
            case Key.D2: case Key.NumPad2: editor.SetDurationByIndex(1); break;
            case Key.D3: case Key.NumPad3: editor.SetDurationByIndex(2); break;
            case Key.D4: case Key.NumPad4: editor.SetDurationByIndex(3); break;
            case Key.D5: case Key.NumPad5: editor.SetDurationByIndex(4); break;
            case Key.D6: case Key.NumPad6: editor.SetDurationByIndex(5); break;

            case Key.R:         editor.ToggleRestCommand.Execute(null); break;
            case Key.OemPeriod: editor.ToggleDotCommand.Execute(null); break;
            case Key.Delete:
            case Key.Back:      editor.DeleteSelectedNoteCommand.Execute(null); break;

            // Transport.
            case Key.Space:  vm.PlaybackVm?.PlayPauseCommand.Execute(null); break;
            case Key.Escape: vm.PlaybackVm?.StopCommand.Execute(null); break;
            case Key.L:      vm.PlaybackVm?.ToggleLoopCommand.Execute(null); break;

            default: return; // not ours — leave unhandled
        }
        e.Handled = true;
    }
}
