using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Extensions.Logging;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;
using MidiTimeSignature = Melanchall.DryWetMidi.Interaction.TimeSignature;
using DomainTimeSignature = SymphoniaLegato.Core.Models.TimeSignature;
using MidiNote = Melanchall.DryWetMidi.Interaction.Note;
using DomainNote = SymphoniaLegato.Core.Models.Note;

namespace SymphoniaLegato.ImportExport;

/// <summary>
/// Imports a Standard MIDI File (.mid) into a <see cref="Score"/>.
/// Maps each MIDI track to a <see cref="Part"/> with one <see cref="Staff"/>.
/// </summary>
public sealed class MidiImporter : IScoreImporter
{
    private readonly ILogger<MidiImporter> _logger;

    public ImportFormat Format => ImportFormat.Midi;
    public IReadOnlyList<string> FileExtensions => [".mid", ".midi"];

    public MidiImporter(ILogger<MidiImporter> logger) => _logger = logger;

    public async Task<Score> ImportAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        return await ImportFromStreamAsync(stream, ct);
    }

    public Task<Score> ImportFromStreamAsync(Stream stream, CancellationToken ct = default)
    {
        var midiFile = MidiFile.Read(stream);
        var score = ConvertToScore(midiFile);
        _logger.LogInformation("Imported MIDI: {Parts} tracks", score.Parts.Count);
        return Task.FromResult(score);
    }

    private static Score ConvertToScore(MidiFile midiFile)
    {
        var score = new Score { Title = "Imported from MIDI" };

        double ppq = midiFile.TimeDivision is TicksPerQuarterNoteTimeDivision tpq
            ? tpq.TicksPerQuarterNote
            : 480.0;

        var tempoMap = midiFile.GetTempoMap();
        int trackIndex = 0;

        foreach (var track in midiFile.GetTrackChunks())
        {
            var notes = track.GetNotes().OrderBy(n => n.Time).ToList();
            if (notes.Count == 0) { trackIndex++; continue; }

            var part = new Part { Name = $"Track {trackIndex + 1}" };
            var staff = new Staff
            {
                Name = part.Name,
                Instrument = Instrument.GrandPiano,
                DefaultClef = DetectClef(notes, ppq)
            };

            ConvertNotes(notes, staff, ppq, score.InitialTimeSignature);
            part.Staves.Add(staff);
            score.Parts.Add(part);
            trackIndex++;
        }

        return score;
    }

    private static Clef DetectClef(List<MidiNote> notes, double ppq)
    {
        double avgMidi = notes.Average(n => (double)n.NoteNumber);
        return avgMidi >= 60 ? Clef.Treble : Clef.Bass;
    }

    private static void ConvertNotes(
        List<MidiNote> midiNotes,
        Staff staff, double ppq, DomainTimeSignature ts)
    {
        int ticksPerMeasure = ts.TicksPerMeasure;
        int domainPerMidi   = (int)(Duration.Quarter.Ticks / ppq);

        // Group into measures
        var byMeasure = new SortedDictionary<int, List<(int startTick, int endTick, int midi, int velocity)>>();
        foreach (var mn in midiNotes)
        {
            int startDomain = (int)(mn.Time * domainPerMidi);
            int endDomain   = (int)((mn.Time + mn.Length) * domainPerMidi);
            int measureNum  = startDomain / ticksPerMeasure + 1;
            int offsetTick  = startDomain % ticksPerMeasure;

            if (!byMeasure.TryGetValue(measureNum, out var list))
            {
                list = [];
                byMeasure[measureNum] = list;
            }
            list.Add((offsetTick, endDomain - startDomain, mn.NoteNumber, mn.Velocity));
        }

        foreach (var (mNum, noteData) in byMeasure)
        {
            var measure = staff.GetOrAddMeasure(mNum, ts);
            foreach (var (offset, dur, midi, vel) in noteData)
            {
                var pitch = Pitch.FromMidi(midi);
                var duration = QuantizeDuration(dur);
                var note = new DomainNote
                {
                    Pitch = pitch,
                    Duration = duration,
                    TickOffset = offset,
                    Velocity = vel
                };
                measure.Notes.Add(note);
            }
            measure.Notes.Sort((a, b) => a.TickOffset.CompareTo(b.TickOffset));
        }
    }

    private static Duration QuantizeDuration(int ticks)
    {
        // Find closest standard duration
        var candidates = new[]
        {
            Duration.Whole, Duration.DottedHalf, Duration.Half,
            Duration.DottedQuarter, Duration.Quarter,
            Duration.DottedEighth, Duration.Eighth,
            Duration.Sixteenth, Duration.ThirtySecond
        };
        return candidates.MinBy(d => Math.Abs(d.Ticks - ticks));
    }
}
