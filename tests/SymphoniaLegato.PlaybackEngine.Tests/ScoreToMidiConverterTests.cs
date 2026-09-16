// File: ScoreToMidiConverterTests.cs
// Description: Unit tests for ScoreToMidiConverter — tick conversion, tempo, GM percussion
//   channel reservation, chord simultaneity, and metronome track generation.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-09-15
// Last edit date: 2026-09-16
// Version: 1.2.1

using FluentAssertions;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Extensions.Logging.Abstractions;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.PlaybackEngine;
using Xunit;
// Melanchall.DryWetMidi.Interaction also exports Note/TimeSignature; alias the domain types
// to avoid CS0104 (see the project's own CLAUDE.md pitfall #4).
using DomainNote = SymphoniaLegato.Core.Models.Note;
using DomainTimeSignature = SymphoniaLegato.Core.Models.TimeSignature;

namespace SymphoniaLegato.PlaybackEngine.Tests;

public sealed class ScoreToMidiConverterTests
{
    private static ScoreToMidiConverter CreateConverter() =>
        new(NullLogger<ScoreToMidiConverter>.Instance);

    private static Score SingleNoteScore(NoteName name = NoteName.C, int octave = 4)
    {
        var score = new Score { InitialTempo = 120 };
        var part = new Part { Name = "Piano" };
        var staff = new Staff { Instrument = Instrument.GrandPiano };
        var measure = new Measure { Number = 1, TimeSignature = DomainTimeSignature.Common };
        measure.AddNote(new DomainNote
        {
            Duration = Duration.Quarter,
            Pitch = new Pitch(name, Accidental.Natural, octave)
        });
        staff.Measures.Add(measure);
        part.Staves.Add(staff);
        score.Parts.Add(part);
        return score;
    }

    [Fact]
    public void Convert_SingleQuarterNote_EmitsNoteOnAndNoteOffAtCorrectMidiTicks()
    {
        // Domain: 1 quarter = 1024 ticks; MIDI PPQ = 480, so a quarter note is 480 MIDI ticks.
        var score = SingleNoteScore(NoteName.C, 4); // middle C = MIDI 60
        var midiFile = CreateConverter().Convert(score);

        var trackChunk = midiFile.GetTrackChunks().Single();
        var noteEvents = trackChunk.GetTimedEvents()
            .Where(e => e.Event is NoteOnEvent or NoteOffEvent)
            .ToList();

        noteEvents.Should().HaveCount(2);
        var noteOn = (NoteOnEvent)noteEvents[0].Event;
        var noteOff = (NoteOffEvent)noteEvents[1].Event;

        noteOn.NoteNumber.Should().Be((SevenBitNumber)60);
        noteEvents[0].Time.Should().Be(0);
        noteOff.NoteNumber.Should().Be((SevenBitNumber)60);
        noteEvents[1].Time.Should().Be(480);
    }

    [Fact]
    public void Convert_HonoursInitialTempo_EmitsMatchingSetTempoEvent()
    {
        var score = SingleNoteScore();
        score.InitialTempo = 90;

        var midiFile = CreateConverter().Convert(score);
        var tempoEvent = midiFile.GetTrackChunks().Single().Events
            .OfType<SetTempoEvent>().Single();

        tempoEvent.MicrosecondsPerQuarterNote.Should().Be(60_000_000L / 90);
    }

    [Fact]
    public void Convert_TenStaves_NeverAssignsGmPercussionChannelToAnInstrument()
    {
        var score = new Score();
        var part = new Part { Name = "Ensemble" };
        for (int i = 0; i < 10; i++)
        {
            var staff = new Staff { Instrument = Instrument.GrandPiano };
            staff.Measures.Add(new Measure { Number = 1, TimeSignature = DomainTimeSignature.Common });
            part.Staves.Add(staff);
        }
        score.Parts.Add(part);

        var midiFile = CreateConverter().Convert(score);
        var programChangeChannels = midiFile.GetTrackChunks()
            .SelectMany(t => t.Events.OfType<ProgramChangeEvent>())
            .Select(e => (int)e.Channel)
            .ToList();

        programChangeChannels.Should().NotContain(9, "channel 9 (MIDI ch 10) is reserved for GM percussion");
        programChangeChannels.Should().HaveCount(10);
    }

    [Fact]
    public void Convert_StaffMarkedMuted_ZeroesItsNoteVelocity()
    {
        var score = SingleNoteScore();
        score.Parts[0].Staves[0].IsMuted = true;

        var midiFile = CreateConverter().Convert(score);
        var noteOn = midiFile.GetTrackChunks().Single().Events.OfType<NoteOnEvent>().Single();

        noteOn.Velocity.Should().Be((SevenBitNumber)0);
    }

    [Fact]
    public void Convert_OneStaffSoloed_MutesEveryOtherStaffEvenIfNotIndividuallyMuted()
    {
        var score = new Score();
        var part = new Part { Name = "Duet" };
        var soloed = new Staff { Instrument = Instrument.GrandPiano, IsSolo = true };
        var other  = new Staff { Instrument = Instrument.GrandPiano };
        foreach (var staff in new[] { soloed, other })
        {
            var measure = new Measure { Number = 1, TimeSignature = DomainTimeSignature.Common };
            measure.AddNote(new DomainNote { Duration = Duration.Quarter, Pitch = new Pitch(NoteName.C, Accidental.Natural, 4) });
            staff.Measures.Add(measure);
            part.Staves.Add(staff);
        }
        score.Parts.Add(part);

        var midiFile = CreateConverter().Convert(score);
        var noteOns = midiFile.GetTrackChunks().SelectMany(t => t.Events.OfType<NoteOnEvent>()).ToList();

        noteOns.Should().HaveCount(2);
        noteOns[0].Velocity.Should().NotBe((SevenBitNumber)0, "the soloed staff should still sound");
        noteOns[1].Velocity.Should().Be((SevenBitNumber)0, "every non-soloed staff is muted once any staff is soloed");
    }

    [Fact]
    public void Convert_WithCountIn_ShiftsMusicByOneBarAndPrependsFourClicks()
    {
        // Common time at 120 BPM: one bar = 4 quarters = 480 MIDI ticks/quarter * 4 = 1920.
        var score = SingleNoteScore();

        var midiFile = CreateConverter().Convert(score, includeMetronome: false, includeCountIn: true);

        var noteOn = midiFile.GetTrackChunks()
            .SelectMany(t => t.GetTimedEvents())
            .Single(e => e.Event is NoteOnEvent on && on.Channel != (FourBitNumber)9);
        noteOn.Time.Should().Be(1920, "the whole piece must be pushed later by exactly one bar");

        var countInClicks = midiFile.GetTrackChunks()
            .SelectMany(t => t.Events)
            .OfType<NoteOnEvent>()
            .Where(e => e.Channel == (FourBitNumber)9)
            .ToList();
        countInClicks.Should().HaveCount(4, "one click per beat in a 4/4 count-in bar");
        countInClicks.Count(e => e.NoteNumber == (SevenBitNumber)76).Should().Be(1, "beat 1 is accented");
    }

    [Fact]
    public void Convert_WithoutCountIn_MusicStartsAtTickZero()
    {
        var score = SingleNoteScore();

        var midiFile = CreateConverter().Convert(score, includeMetronome: false, includeCountIn: false);

        var noteOn = midiFile.GetTrackChunks().Single().GetTimedEvents()
            .Single(e => e.Event is NoteOnEvent);
        noteOn.Time.Should().Be(0, "no count-in requested — nothing should shift the music");
    }

    [Fact]
    public void ComputeCountInDuration_CommonTimeAt120Bpm_IsTwoSeconds()
    {
        var score = SingleNoteScore(); // 4/4 at 120 BPM

        var duration = ScoreToMidiConverter.ComputeCountInDuration(score);

        duration.Should().Be(TimeSpan.FromSeconds(2.0), "4 beats at 120 BPM = 4 * 0.5s");
    }

    [Fact]
    public void Convert_ChordNote_EmitsAllPitchesSimultaneously()
    {
        var score = new Score();
        var part = new Part { Name = "Piano" };
        var staff = new Staff { Instrument = Instrument.GrandPiano };
        var measure = new Measure { Number = 1, TimeSignature = DomainTimeSignature.Common };
        var chordRoot = new DomainNote
        {
            Duration = Duration.Quarter,
            Pitch = new Pitch(NoteName.C, Accidental.Natural, 4)
        };
        chordRoot.ChordNotes.Add(new Pitch(NoteName.E, Accidental.Natural, 4));
        chordRoot.ChordNotes.Add(new Pitch(NoteName.G, Accidental.Natural, 4));
        measure.AddNote(chordRoot);
        staff.Measures.Add(measure);
        part.Staves.Add(staff);
        score.Parts.Add(part);

        var midiFile = CreateConverter().Convert(score);
        var noteOns = midiFile.GetTrackChunks().Single().GetTimedEvents()
            .Where(e => e.Event is NoteOnEvent)
            .ToList();

        noteOns.Should().HaveCount(3);
        noteOns.Select(e => e.Time).Should().AllBeEquivalentTo(0L);
        noteOns.Select(e => (int)((NoteOnEvent)e.Event).NoteNumber)
            .Should().BeEquivalentTo([60, 64, 67]);
    }

    [Fact]
    public void Convert_WithMetronome_AddsOneAccentedAndThreeUnaccentedClicksPerCommonTimeMeasure()
    {
        var score = SingleNoteScore();

        var midiFile = CreateConverter().Convert(score, includeMetronome: true);
        var clickChannelEvents = midiFile.GetTrackChunks()
            .SelectMany(t => t.Events)
            .OfType<NoteOnEvent>()
            .Where(e => e.Channel == (FourBitNumber)9)
            .ToList();

        // 4/4 time: one accented (beat 1) + three unaccented wood-block clicks.
        clickChannelEvents.Should().HaveCount(4);
        clickChannelEvents.Count(e => e.NoteNumber == (SevenBitNumber)76).Should().Be(1);
        clickChannelEvents.Count(e => e.NoteNumber == (SevenBitNumber)77).Should().Be(3);
    }

    [Fact]
    public void Convert_WithoutMetronome_EmitsNoPercussionChannelEvents()
    {
        var score = SingleNoteScore();

        var midiFile = CreateConverter().Convert(score, includeMetronome: false);
        var percussionEvents = midiFile.GetTrackChunks()
            .SelectMany(t => t.Events)
            .OfType<NoteOnEvent>()
            .Where(e => e.Channel == (FourBitNumber)9);

        percussionEvents.Should().BeEmpty();
    }
}
