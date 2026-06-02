using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.ImportExport;

/// <summary>Imports MusicXML 4.0 files into the Symphonia Legato domain model.</summary>
public sealed class MusicXmlImporter : IScoreImporter
{
    private readonly ILogger<MusicXmlImporter> _logger;

    public ImportFormat Format => ImportFormat.MusicXml;
    public IReadOnlyList<string> FileExtensions => [".xml", ".musicxml", ".mxl"];

    public MusicXmlImporter(ILogger<MusicXmlImporter> logger) => _logger = logger;

    public async Task<Score> ImportAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        return await ImportFromStreamAsync(stream, ct);
    }

    public async Task<Score> ImportFromStreamAsync(Stream stream, CancellationToken ct = default)
    {
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, ct);
        return Parse(doc);
    }

    private Score Parse(XDocument doc)
    {
        var root = doc.Root ?? throw new InvalidDataException("Empty MusicXML document");

        var score = new Score();

        // Score metadata
        var work = root.Element("work");
        score.Title    = work?.Element("work-title")?.Value    ?? root.Element("movement-title")?.Value ?? "Untitled";
        var credit = root.Elements("credit").FirstOrDefault();
        score.Composer = root.Elements("identification")
                            .Elements("creator")
                            .FirstOrDefault(e => e.Attribute("type")?.Value == "composer")?.Value
                         ?? string.Empty;

        // Part list
        var partListEl = root.Element("part-list");
        var partMap = new Dictionary<string, Part>();
        if (partListEl is not null)
        {
            foreach (var scorePart in partListEl.Elements("score-part"))
            {
                string id = scorePart.Attribute("id")?.Value ?? Guid.NewGuid().ToString();
                string name = scorePart.Element("part-name")?.Value ?? "Part";
                string shortName = scorePart.Element("part-abbreviation")?.Value ?? name[..Math.Min(4, name.Length)];
                var part = new Part { Name = name, ShortName = shortName };
                var staff = new Staff { Name = name };
                part.Staves.Add(staff);
                partMap[id] = part;
                score.Parts.Add(part);
            }
        }

        // Parse measures per part
        foreach (var partEl in root.Elements("part"))
        {
            string partId = partEl.Attribute("id")?.Value ?? "";
            if (!partMap.TryGetValue(partId, out var part)) continue;
            var staff = part.Staves[0];

            int measureNumber = 0;
            int divisions = 1;
            TimeSignature timeSig = TimeSignature.Common;
            KeySignature keySig = KeySignature.CMajor;
            Clef clef = Clef.Treble;

            foreach (var measureEl in partEl.Elements("measure"))
            {
                measureNumber++;
                var measure = new Measure { Number = measureNumber, TimeSignature = timeSig };

                // Attributes
                var attrib = measureEl.Element("attributes");
                if (attrib is not null)
                {
                    if (int.TryParse(attrib.Element("divisions")?.Value, out int div))
                        divisions = div;

                    var timeEl = attrib.Element("time");
                    if (timeEl is not null)
                    {
                        int num = int.Parse(timeEl.Element("beats")?.Value ?? "4");
                        int den = int.Parse(timeEl.Element("beat-type")?.Value ?? "4");
                        timeSig = new TimeSignature(num, den);
                        measure.TimeSignature = timeSig;
                    }

                    var fifthsEl = attrib.Element("key")?.Element("fifths");
                    if (fifthsEl is not null)
                    {
                        int fifths = int.Parse(fifthsEl.Value);
                        string mode = attrib.Element("key")?.Element("mode")?.Value ?? "major";
                        keySig = new KeySignature(fifths, mode == "minor" ? Mode.Minor : Mode.Major);
                        measure.KeySignatureChange = keySig;
                    }

                    var clefEl = attrib.Element("clef");
                    if (clefEl is not null)
                    {
                        string sign = clefEl.Element("sign")?.Value ?? "G";
                        clef = sign switch
                        {
                            "G" => Clef.Treble,
                            "F" => Clef.Bass,
                            "C" => Clef.Alto,
                            _   => Clef.Treble
                        };
                        measure.ClefChange = clef;
                    }
                }

                // Notes
                foreach (var noteEl in measureEl.Elements("note"))
                {
                    bool isRest = noteEl.Element("rest") is not null;
                    bool isChord = noteEl.Element("chord") is not null;

                    int durationTicks = 0;
                    if (int.TryParse(noteEl.Element("duration")?.Value, out int midiDur))
                        durationTicks = DurationToTicks(midiDur, divisions);

                    var note = new Note
                    {
                        Duration = TicksToDuration(durationTicks),
                        Pitch = isRest ? null : ParsePitch(noteEl)
                    };

                    if (isChord && measure.Notes.Count > 0)
                    {
                        var lastNote = measure.Notes[^1];
                        if (note.Pitch.HasValue)
                            lastNote.ChordNotes.Add(note.Pitch.Value);
                    }
                    else
                    {
                        measure.AddNote(note);
                    }
                }

                staff.Measures.Add(measure);
            }
        }

        _logger.LogInformation("Imported MusicXML: '{Title}' ({Parts} parts)", score.Title, score.Parts.Count);
        return score;
    }

    private static Pitch? ParsePitch(XElement noteEl)
    {
        var pitchEl = noteEl.Element("pitch");
        if (pitchEl is null) return null;

        string stepStr = pitchEl.Element("step")?.Value ?? "C";
        int octave = int.Parse(pitchEl.Element("octave")?.Value ?? "4");
        int alter = (int)(double.TryParse(pitchEl.Element("alter")?.Value, out double a) ? a : 0);

        NoteName name = stepStr switch
        {
            "C" => NoteName.C, "D" => NoteName.D, "E" => NoteName.E,
            "F" => NoteName.F, "G" => NoteName.G, "A" => NoteName.A,
            "B" => NoteName.B, _ => NoteName.C
        };
        Accidental acc = alter switch { -2 => Accidental.DoubleFlat, -1 => Accidental.Flat, 1 => Accidental.Sharp, 2 => Accidental.DoubleSharp, _ => Accidental.Natural };
        return new Pitch(name, acc, octave);
    }

    // Convert MusicXML divisions-based duration to domain ticks (1 quarter = 1024)
    private static int DurationToTicks(int midiDur, int divisions) =>
        (int)((long)midiDur * Duration.Quarter.Ticks / divisions);

    private static Duration TicksToDuration(int ticks)
    {
        // Find closest standard duration
        var candidates = Enum.GetValues<NoteValue>()
            .Select(v => new Duration(v))
            .OrderBy(d => Math.Abs(d.Ticks - ticks));
        return candidates.First();
    }
}
