using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.ImportExport;

/// <summary>Exports a <see cref="Score"/> to MusicXML 4.0 format.</summary>
public sealed class MusicXmlExporter : IScoreExporter
{
    private readonly ILogger<MusicXmlExporter> _logger;

    public ExportFormat Format => ExportFormat.MusicXml;
    public string DefaultFileExtension => ".musicxml";

    public MusicXmlExporter(ILogger<MusicXmlExporter> logger) => _logger = logger;

    public async Task ExportAsync(Score score, string filePath, ExportOptions? options = null, CancellationToken ct = default)
    {
        await using var stream = File.Create(filePath);
        await ExportToStreamAsync(score, stream, options, ct);
    }

    public async Task ExportToStreamAsync(Score score, Stream stream, ExportOptions? options = null, CancellationToken ct = default)
    {
        var doc = BuildDocument(score);
        var settings = new System.Xml.XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            Async = true
        };
        await using var writer = System.Xml.XmlWriter.Create(stream, settings);
        doc.WriteTo(writer);
        _logger.LogInformation("Exported MusicXML for '{Title}'", score.Title);
    }

    private XDocument BuildDocument(Score score)
    {
        const int Divisions = 256; // ticks per quarter in MusicXML output

        var root = new XElement("score-partwise",
            new XAttribute("version", "4.0"),
            new XElement("work", new XElement("work-title", score.Title)),
            new XElement("identification",
                new XElement("creator", new XAttribute("type", "composer"), score.Composer),
                new XElement("encoding",
                    new XElement("software", "Symphonia Legato 1.0"),
                    new XElement("encoding-date", DateTime.UtcNow.ToString("yyyy-MM-dd")))));

        // Part list
        var partList = new XElement("part-list");
        foreach (var (part, pi) in score.Parts.Select((p, i) => (p, i + 1)))
        {
            partList.Add(new XElement("score-part",
                new XAttribute("id", $"P{pi}"),
                new XElement("part-name", part.Name),
                new XElement("part-abbreviation", part.ShortName),
                new XElement("score-instrument",
                    new XAttribute("id", $"P{pi}-I1"),
                    new XElement("instrument-name", part.Staves.FirstOrDefault()?.Instrument.Name ?? "Piano")),
                new XElement("midi-instrument",
                    new XAttribute("id", $"P{pi}-I1"),
                    new XElement("midi-channel", pi),
                    new XElement("midi-program", (part.Staves.FirstOrDefault()?.Instrument.MidiProgram ?? 0) + 1))));
        }
        root.Add(partList);

        // Parts
        foreach (var (part, pi) in score.Parts.Select((p, i) => (p, i + 1)))
        {
            var partEl = new XElement("part", new XAttribute("id", $"P{pi}"));
            var staff = part.Staves.FirstOrDefault();
            if (staff is null) { root.Add(partEl); continue; }

            bool firstMeasure = true;
            foreach (var measure in staff.Measures.OrderBy(m => m.Number))
            {
                var measureEl = new XElement("measure", new XAttribute("number", measure.Number));

                if (firstMeasure || measure.ClefChange.HasValue || measure.KeySignatureChange.HasValue)
                {
                    var attrib = new XElement("attributes",
                        new XElement("divisions", Divisions));

                    if (firstMeasure || measure.KeySignatureChange.HasValue)
                    {
                        var ks = measure.KeySignatureChange ?? score.InitialKeySignature;
                        attrib.Add(new XElement("key",
                            new XElement("fifths", ks.Fifths),
                            new XElement("mode", ks.Mode.ToString().ToLower())));
                    }

                    if (firstMeasure)
                    {
                        attrib.Add(new XElement("time",
                            new XElement("beats", measure.TimeSignature.Numerator),
                            new XElement("beat-type", measure.TimeSignature.Denominator)));
                    }

                    var clef = measure.ClefChange ?? staff.DefaultClef;
                    attrib.Add(new XElement("clef",
                        new XElement("sign", clef.Type switch
                        {
                            ClefType.Treble => "G", ClefType.Bass => "F",
                            ClefType.Alto   => "C", ClefType.Tenor => "C",
                            _ => "G"
                        }),
                        new XElement("line", clef.StaffLine)));

                    measureEl.Add(attrib);
                    firstMeasure = false;
                }

                // Direction (tempo on first measure)
                if (measure.Number == 1 && score.InitialTempo > 0)
                {
                    measureEl.Add(new XElement("direction",
                        new XAttribute("placement", "above"),
                        new XElement("direction-type",
                            new XElement("metronome",
                                new XAttribute("parentheses", "no"),
                                new XElement("beat-unit", "quarter"),
                                new XElement("per-minute", score.InitialTempo)))));
                }

                // Notes
                foreach (var note in measure.Notes.OrderBy(n => n.TickOffset))
                {
                    measureEl.Add(BuildNoteElement(note, Divisions));
                    // Chord notes
                    foreach (var chordPitch in note.ChordNotes)
                    {
                        var chordNote = new Note { Pitch = chordPitch, Duration = note.Duration };
                        var el = BuildNoteElement(chordNote, Divisions);
                        el.AddFirst(new XElement("chord"));
                        measureEl.Add(el);
                    }
                }

                partEl.Add(measureEl);
            }

            root.Add(partEl);
        }

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XDocumentType("score-partwise",
                "-//Recordare//DTD MusicXML 4.0 Partwise//EN",
                "http://www.musicxml.org/dtds/partwise.dtd", null),
            root);
    }

    private static XElement BuildNoteElement(Note note, int divisions)
    {
        int durationTicks = note.Duration.Ticks * divisions / Duration.Quarter.Ticks;

        var noteEl = new XElement("note");
        if (note.IsRest)
        {
            noteEl.Add(new XElement("rest"));
        }
        else if (note.Pitch.HasValue)
        {
            var p = note.Pitch.Value;
            noteEl.Add(new XElement("pitch",
                new XElement("step", p.Name.ToString()),
                p.Accidental != Accidental.Natural
                    ? new XElement("alter", (int)p.Accidental)
                    : null!,
                new XElement("octave", p.Octave)));
        }

        noteEl.Add(
            new XElement("duration", durationTicks),
            new XElement("type", NoteValueToType(note.Duration.Value)));

        for (int d = 0; d < note.Duration.Dots; d++)
            noteEl.Add(new XElement("dot"));

        return noteEl;
    }

    private static string NoteValueToType(NoteValue v) => v switch
    {
        NoteValue.Whole         => "whole",
        NoteValue.Half          => "half",
        NoteValue.Quarter       => "quarter",
        NoteValue.Eighth        => "eighth",
        NoteValue.Sixteenth     => "16th",
        NoteValue.ThirtySecond  => "32nd",
        NoteValue.SixtyFourth   => "64th",
        NoteValue.Breve         => "breve",
        _ => "quarter"
    };
}
