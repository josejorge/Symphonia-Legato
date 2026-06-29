using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Extensions.Logging;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.PlaybackEngine;

/// <summary>Converts a <see cref="Score"/> domain object to a <see cref="MidiFile"/>.</summary>
public sealed class ScoreToMidiConverter
{
    private readonly ILogger<ScoreToMidiConverter> _logger;

    // 1 quarter = 1024 ticks in our domain; map to MIDI PPQ
    private const int MidiPPQ = 480;
    private const int DomainPPQ = 1024;

    public ScoreToMidiConverter(ILogger<ScoreToMidiConverter> logger) => _logger = logger;

    public MidiFile Convert(Score score)
    {
        var file = new MidiFile { TimeDivision = new TicksPerQuarterNoteTimeDivision(MidiPPQ) };

        int channelIndex = 0;
        foreach (var part in score.Parts)
        foreach (var staff in part.Staves)
        {
            if (channelIndex >= 16) break;
            if (channelIndex == 9) channelIndex++; // skip percussion channel

            // Emit the tempo on the first track so the score's InitialTempo is
            // honoured (otherwise DryWetMidi defaults every score to 120 BPM).
            var track = BuildTrack(staff, channelIndex, score.InitialTempo,
                includeTempo: file.Chunks.Count == 0);
            file.Chunks.Add(track);
            channelIndex++;
        }

        _logger.LogDebug("Converted score '{Title}' to MIDI ({Tracks} tracks)", score.Title, file.Chunks.Count);
        return file;
    }

    private TrackChunk BuildTrack(Staff staff, int channel, int bpm, bool includeTempo)
    {
        var events = new List<MidiEvent>();

        // Tempo (microseconds per quarter note) — only on the first track.
        if (includeTempo)
        {
            long microsecondsPerQuarter = 60_000_000L / Math.Max(1, bpm);
            events.Add(new SetTempoEvent(microsecondsPerQuarter) { DeltaTime = 0 });
        }

        // Program change
        events.Add(new ProgramChangeEvent((SevenBitNumber)staff.Instrument.MidiProgram)
            { Channel = (FourBitNumber)channel, DeltaTime = 0 });

        // Volume & pan
        events.Add(new ControlChangeEvent((SevenBitNumber)7, (SevenBitNumber)staff.Volume)
            { Channel = (FourBitNumber)channel, DeltaTime = 0 });
        events.Add(new ControlChangeEvent((SevenBitNumber)10, (SevenBitNumber)staff.Pan)
            { Channel = (FourBitNumber)channel, DeltaTime = 0 });

        long absoluteTick = 0;
        long lastEventTick = 0;

        foreach (var measure in staff.Measures.OrderBy(m => m.Number))
        {
            foreach (var note in measure.Notes.Where(n => !n.IsRest).OrderBy(n => n.TickOffset))
            {
                long noteStart = absoluteTick + DomainToMidi(note.TickOffset);
                long noteEnd   = noteStart + DomainToMidi(note.Duration.Ticks);

                // NoteOn
                events.Add(new NoteOnEvent(
                    (SevenBitNumber)(note.Pitch?.MidiNumber ?? 60),
                    (SevenBitNumber)(staff.IsMuted ? 0 : note.Velocity))
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = noteStart - lastEventTick
                });
                lastEventTick = noteStart;

                // Chord notes
                foreach (var chordPitch in note.ChordNotes)
                {
                    events.Add(new NoteOnEvent(
                        (SevenBitNumber)chordPitch.MidiNumber,
                        (SevenBitNumber)(staff.IsMuted ? 0 : note.Velocity))
                    {
                        Channel = (FourBitNumber)channel,
                        DeltaTime = 0
                    });
                }

                // NoteOff
                events.Add(new NoteOffEvent(
                    (SevenBitNumber)(note.Pitch?.MidiNumber ?? 60),
                    (SevenBitNumber)0)
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = noteEnd - lastEventTick
                });
                lastEventTick = noteEnd;

                foreach (var chordPitch in note.ChordNotes)
                {
                    events.Add(new NoteOffEvent(
                        (SevenBitNumber)chordPitch.MidiNumber,
                        (SevenBitNumber)0)
                    {
                        Channel = (FourBitNumber)channel,
                        DeltaTime = 0
                    });
                }
            }

            absoluteTick += DomainToMidi(measure.TimeSignature.TicksPerMeasure);
        }

        return new TrackChunk(events);
    }

    private static long DomainToMidi(int domainTicks) =>
        (long)domainTicks * MidiPPQ / DomainPPQ;
}
