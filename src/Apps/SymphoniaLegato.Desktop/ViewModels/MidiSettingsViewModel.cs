// File: MidiSettingsViewModel.cs
// Description: View model for the MIDI/Audio Settings dialog — output device and SoundFont path selection.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-02
// Last edit date: 2026-09-15
// Version: 1.1.0

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melanchall.DryWetMidi.Multimedia;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Desktop.Services;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class MidiSettingsViewModel : ViewModelBase
{
    private readonly IPlaybackEngine _engine;
    private readonly AppSettingsService _settings;

    [ObservableProperty] private string _selectedDeviceName = string.Empty;
    [ObservableProperty] private string _soundFontPath = string.Empty;
    [ObservableProperty] private string _statusText = "Changes take effect on next playback.";
    [ObservableProperty] private int _selectedDeviceIndex = 0;

    public ObservableCollection<string> OutputDevices { get; } = [];
    public event EventHandler? CloseRequested;

    public MidiSettingsViewModel(IPlaybackEngine engine, AppSettingsService settings)
    {
        _engine   = engine;
        _settings = settings;
        RefreshDevices();

        // Restore the persisted device (if it's still present on this machine) and
        // apply it to the engine immediately, so playback works without the user
        // having to reopen this dialog and hit Apply every session.
        var saved = _settings.Current.MidiOutputDeviceName;
        if (saved is not null && OutputDevices.Contains(saved))
        {
            SelectedDeviceName  = saved;
            SelectedDeviceIndex = OutputDevices.IndexOf(saved);
            _engine.SetOutputDevice(saved);
        }
    }

    [RelayCommand]
    private void RefreshDevices()
    {
        OutputDevices.Clear();
        try
        {
            foreach (var device in OutputDevice.GetAll())
                OutputDevices.Add(device.Name);

            if (OutputDevices.Count == 0)
                OutputDevices.Add("(no MIDI output devices found)");

            SelectedDeviceIndex = 0;
            SelectedDeviceName = OutputDevices[0];
        }
        catch
        {
            OutputDevices.Add("(error enumerating MIDI devices)");
        }
    }

    [RelayCommand]
    private void BrowseSoundFont()
    {
        // File picker opened from MainWindow via event
        SoundFontBrowseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Apply()
    {
        if (SelectedDeviceIndex >= 0 && SelectedDeviceIndex < OutputDevices.Count)
            SelectedDeviceName = OutputDevices[SelectedDeviceIndex];

        _engine.SetOutputDevice(SelectedDeviceName);
        _settings.Current.MidiOutputDeviceName = SelectedDeviceName;
        _settings.Save();

        StatusText = "Settings applied and saved.";
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    public event EventHandler? SoundFontBrowseRequested;
}
