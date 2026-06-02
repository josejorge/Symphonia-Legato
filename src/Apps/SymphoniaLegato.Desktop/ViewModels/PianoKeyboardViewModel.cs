using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed class PianoKey
{
    public Pitch Pitch { get; init; }
    public bool IsBlack { get; init; }
    public bool IsPressed { get; set; }
    public bool IsHighlighted { get; set; }
}

public sealed partial class PianoKeyboardViewModel : ViewModelBase
{
    private readonly IPlaybackEngine _engine;

    [ObservableProperty] private int _startOctave = 2;
    [ObservableProperty] private int _octaveCount = 5;

    public ObservableCollection<PianoKey> Keys { get; } = [];

    public PianoKeyboardViewModel(IPlaybackEngine engine)
    {
        _engine = engine;
        _engine.NotePlayed += OnNotePlayed;
        BuildKeys();
    }

    partial void OnStartOctaveChanged(int value) => BuildKeys();
    partial void OnOctaveCountChanged(int value) => BuildKeys();

    private void BuildKeys()
    {
        Keys.Clear();
        for (int oct = StartOctave; oct < StartOctave + OctaveCount; oct++)
        {
            foreach (NoteName name in Enum.GetValues<NoteName>())
            {
                bool isBlack = name is NoteName.C or NoteName.D or NoteName.F or NoteName.G or NoteName.A
                    ? false
                    : false; // Recalculate properly below

                // Black keys: C#, D#, F#, G#, A#
                bool needsSharp = name is NoteName.C or NoteName.D or NoteName.F or NoteName.G or NoteName.A;
                Keys.Add(new PianoKey
                {
                    Pitch = new Pitch(name, Accidental.Natural, oct),
                    IsBlack = false
                });
                if (needsSharp && !(name == NoteName.E || name == NoteName.B))
                {
                    Keys.Add(new PianoKey
                    {
                        Pitch = new Pitch(name, Accidental.Sharp, oct),
                        IsBlack = true
                    });
                }
            }
        }
    }

    [RelayCommand]
    private async Task PressKeyAsync(PianoKey key)
    {
        key.IsPressed = true;
        await _engine.PreviewNoteAsync(key.Pitch);
        key.IsPressed = false;
    }

    private void OnNotePlayed(object? sender, NotePlayedEventArgs e)
    {
        var key = Keys.FirstOrDefault(k => k.Pitch.MidiNumber == e.Pitch.MidiNumber);
        if (key is not null)
        {
            key.IsHighlighted = true;
            // Reset after short delay (fire-and-forget)
            _ = ResetHighlightAsync(key);
        }
    }

    private static async Task ResetHighlightAsync(PianoKey key)
    {
        await Task.Delay(200);
        key.IsHighlighted = false;
    }
}
