// File: MixerViewModel.cs
// Description: View models for the per-staff mixer panel — MixerChannelViewModel (one row) and MixerViewModel (the collection).
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-01
// Last edit date: 2026-09-15
// Version: 1.1.0

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class MixerChannelViewModel : ViewModelBase
{
    private readonly IPlaybackEngine _engine;
    private readonly Staff _staff;
    private bool _loading;

    public Guid StaffId { get; init; }

    [ObservableProperty] private string _name = "Staff";
    [ObservableProperty] private int _volume = 100;
    [ObservableProperty] private int _pan = 64;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private bool _isSolo;

    /// <summary>Raised after a change is written back to the underlying <see cref="Staff"/>,
    /// so the owning score can be marked dirty (see <see cref="MixerViewModel.MixerChanged"/>).</summary>
    public event EventHandler? Changed;

    public MixerChannelViewModel(IPlaybackEngine engine, Staff staff)
    {
        _engine = engine;
        _staff  = staff;
    }

    // Mixer tweaks write straight back into the Staff (so the next Play — which rebuilds
    // the MIDI from the Score — actually reflects them) and send live CC via the engine.
    // They deliberately skip ScoreEditor.Execute/undo: these are continuous mixing
    // controls, not discrete notation edits, the same treatment Zoom already gets.
    partial void OnVolumeChanged(int value)
    {
        _staff.Volume = value;
        _engine.SetStaffVolume(StaffId, value);
        NotifyChanged();
    }

    partial void OnPanChanged(int value)
    {
        _staff.Pan = value;
        _engine.SetStaffPan(StaffId, value);
        NotifyChanged();
    }

    partial void OnIsMutedChanged(bool value)
    {
        _staff.IsMuted = value;
        _engine.SetStaffMuted(StaffId, value);
        NotifyChanged();
    }

    partial void OnIsSoloChanged(bool value)
    {
        _staff.IsSolo = value;
        _engine.SetStaffSolo(StaffId, value);
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        if (!_loading) Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Sets the four mixer values from the Staff without treating it as a user
    /// edit (no write-back, no <see cref="Changed"/>) — used when first populating the panel.</summary>
    internal void LoadFromStaff()
    {
        _loading = true;
        Volume  = _staff.Volume;
        Pan     = _staff.Pan;
        IsMuted = _staff.IsMuted;
        IsSolo  = _staff.IsSolo;
        _loading = false;
    }
}

public sealed partial class MixerViewModel : ViewModelBase
{
    private readonly IPlaybackEngine _engine;

    public ObservableCollection<MixerChannelViewModel> Channels { get; } = [];

    /// <summary>Raised whenever a mixer channel writes a change back into its Staff —
    /// lets the owner mark the current score dirty without MixerViewModel depending on
    /// ScoreEditor directly.</summary>
    public event EventHandler? MixerChanged;

    public MixerViewModel(IPlaybackEngine engine) => _engine = engine;

    public void LoadScore(Score score)
    {
        Channels.Clear();
        foreach (var staff in score.Parts.SelectMany(p => p.Staves))
        {
            var channel = new MixerChannelViewModel(_engine, staff)
            {
                StaffId = staff.Id,
                Name    = $"{staff.Instrument.Name} ({staff.DefaultClef.Type})"
            };
            channel.LoadFromStaff();
            channel.Changed += (_, _) => MixerChanged?.Invoke(this, EventArgs.Empty);
            Channels.Add(channel);
        }
    }
}
