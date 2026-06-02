using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melanchall.DryWetMidi.Multimedia;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class MidiSettingsViewModel : ViewModelBase
{
    [ObservableProperty] private string _selectedDeviceName = string.Empty;
    [ObservableProperty] private string _soundFontPath = string.Empty;
    [ObservableProperty] private string _statusText = "Changes take effect on next playback.";
    [ObservableProperty] private int _selectedDeviceIndex = 0;

    public ObservableCollection<string> OutputDevices { get; } = [];
    public event EventHandler? CloseRequested;

    public MidiSettingsViewModel()
    {
        RefreshDevices();
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

        StatusText = "Settings applied. Restart playback to use the new device.";
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    public event EventHandler? SoundFontBrowseRequested;
}
