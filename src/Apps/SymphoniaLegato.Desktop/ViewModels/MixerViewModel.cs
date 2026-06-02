using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class MixerChannelViewModel : ViewModelBase
{
    private readonly IPlaybackEngine _engine;

    public Guid StaffId { get; init; }

    [ObservableProperty] private string _name = "Staff";
    [ObservableProperty] private int _volume = 100;
    [ObservableProperty] private int _pan = 64;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private bool _isSolo;

    public MixerChannelViewModel(IPlaybackEngine engine) => _engine = engine;

    partial void OnVolumeChanged(int value)  => _engine.SetStaffVolume(StaffId, value);
    partial void OnPanChanged(int value)     => _engine.SetStaffPan(StaffId, value);
    partial void OnIsMutedChanged(bool value) => _engine.SetStaffMuted(StaffId, value);
    partial void OnIsSoloChanged(bool value)  => _engine.SetStaffSolo(StaffId, value);
}

public sealed partial class MixerViewModel : ViewModelBase
{
    private readonly IPlaybackEngine _engine;

    public ObservableCollection<MixerChannelViewModel> Channels { get; } = [];

    public MixerViewModel(IPlaybackEngine engine) => _engine = engine;

    public void LoadScore(Score score)
    {
        Channels.Clear();
        foreach (var staff in score.Parts.SelectMany(p => p.Staves))
        {
            Channels.Add(new MixerChannelViewModel(_engine)
            {
                StaffId = staff.Id,
                Name    = $"{staff.Instrument.Name} ({staff.DefaultClef.Type})",
                Volume  = staff.Volume,
                Pan     = staff.Pan,
                IsMuted = staff.IsMuted,
                IsSolo  = staff.IsSolo
            });
        }
    }
}
