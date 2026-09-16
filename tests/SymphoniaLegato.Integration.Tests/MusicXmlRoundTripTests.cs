// File: MusicXmlRoundTripTests.cs
// Description: MusicXML export/import round-trip fidelity tests. Covers what actually survives today (single-staff notes, durations, chords, time/key signature) and explicitly characterizes two real gaps found while writing this file: a grand staff's second staff is silently dropped on export, and notation markup (slurs/dynamics/hairpins/lyrics/articulations/ties) isn't exported at all — see docs/BUGS.md and docs/TODO.md.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-09-15
// Last edit date: 2026-09-15
// Version: 1.0.0

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.ImportExport;
using Xunit;

namespace SymphoniaLegato.Integration.Tests;

public sealed class MusicXmlRoundTripTests
{
    private readonly MusicXmlExporter _exporter = new(NullLogger<MusicXmlExporter>.Instance);
    private readonly MusicXmlImporter _importer = new(NullLogger<MusicXmlImporter>.Instance);

    private async Task<Score> RoundTripAsync(Score score)
    {
        using var stream = new MemoryStream();
        await _exporter.ExportToStreamAsync(score, stream);
        stream.Position = 0;
        return await _importer.ImportFromStreamAsync(stream);
    }

    // ── What round-trips correctly today ────────────────────────────────

    [Fact]
    public async Task RoundTrip_PreservesTitleAndComposer()
    {
        var score = Score.CreatePianoScore("Für Elise");
        score.Composer = "Beethoven";

        var result = await RoundTripAsync(score);

        result.Title.Should().Be("Für Elise");
        result.Composer.Should().Be("Beethoven");
    }

    [Fact]
    public async Task RoundTrip_PreservesTimeAndKeySignature()
    {
        var score = Score.CreatePianoScore("Test");
        score.InitialTimeSignature = new TimeSignature(3, 4);
        score.InitialKeySignature  = new KeySignature(2, Mode.Major); // D major
        var staff = score.Parts[0].Staves[0];
        staff.GetOrAddMeasure(1, score.InitialTimeSignature);

        var result = await RoundTripAsync(score);
        var resultMeasure = result.Parts[0].Staves[0].Measures[0];

        resultMeasure.TimeSignature.Numerator.Should().Be(3);
        resultMeasure.TimeSignature.Denominator.Should().Be(4);
        resultMeasure.KeySignatureChange!.Value.Fifths.Should().Be(2);
    }

    [Fact]
    public async Task RoundTrip_PreservesNotePitchDurationAndDots()
    {
        var score = Score.CreatePianoScore("Test");
        var staff = score.Parts[0].Staves[0];
        var measure = staff.GetOrAddMeasure(1, TimeSignature.Common);
        measure.AddNote(new Note
        {
            Duration = new Duration(NoteValue.Eighth, Dots: 1),
            Pitch    = new Pitch(NoteName.F, Accidental.Sharp, 5)
        });

        var result = await RoundTripAsync(score);
        var note = result.Parts[0].Staves[0].Measures[0].Notes.Single();

        note.Pitch.Should().Be(new Pitch(NoteName.F, Accidental.Sharp, 5));
        note.Duration.Value.Should().Be(NoteValue.Eighth);
        note.Duration.Dots.Should().Be(1);
    }

    [Fact]
    public async Task RoundTrip_PreservesRests()
    {
        var score = Score.CreatePianoScore("Test");
        var staff = score.Parts[0].Staves[0];
        var measure = staff.GetOrAddMeasure(1, TimeSignature.Common);
        measure.AddNote(new Note { Duration = Duration.Quarter }); // Pitch null = rest

        var result = await RoundTripAsync(score);

        result.Parts[0].Staves[0].Measures[0].Notes.Single().IsRest.Should().BeTrue();
    }

    [Fact]
    public async Task RoundTrip_PreservesChordNotes()
    {
        var score = Score.CreatePianoScore("Test");
        var staff = score.Parts[0].Staves[0];
        var measure = staff.GetOrAddMeasure(1, TimeSignature.Common);
        var root = new Note { Duration = Duration.Quarter, Pitch = Pitch.MiddleC };
        root.ChordNotes.Add(new Pitch(NoteName.E, Accidental.Natural, 4));
        root.ChordNotes.Add(new Pitch(NoteName.G, Accidental.Natural, 4));
        measure.AddNote(root);

        var result = await RoundTripAsync(score);
        var note = result.Parts[0].Staves[0].Measures[0].Notes.Single();

        note.Pitch.Should().Be(Pitch.MiddleC);
        note.ChordNotes.Should().BeEquivalentTo(
        [
            new Pitch(NoteName.E, Accidental.Natural, 4),
            new Pitch(NoteName.G, Accidental.Natural, 4)
        ]);
    }

    // ── Known gaps (characterization tests — see docs/BUGS.md / docs/TODO.md) ──

    [Fact]
    public async Task KnownGap_ExportingAGrandStaff_SilentlyDropsTheSecondStaff()
    {
        // MusicXmlExporter.BuildDocument only ever reads part.Staves.FirstOrDefault().
        // A piano score's bass clef staff — the whole left hand — never reaches the
        // file. This test documents the current (broken) behavior on purpose: if it
        // ever starts failing because someone fixed multi-staff export, that's a sign
        // to delete this test and add real "second staff round-trips" coverage instead
        // of a "the bug is still there" assertion.
        var score = Score.CreatePianoScore("Test");
        var treble = score.Parts[0].Staves[0];
        var bass   = score.Parts[0].Staves[1];
        treble.GetOrAddMeasure(1, TimeSignature.Common).AddNote(new Note { Duration = Duration.Quarter, Pitch = Pitch.MiddleC });
        bass.GetOrAddMeasure(1, TimeSignature.Common).AddNote(new Note { Duration = Duration.Quarter, Pitch = new Pitch(NoteName.C, Accidental.Natural, 3) });

        var result = await RoundTripAsync(score);

        // Only one staff survives per part today — this is data loss, not a stylistic gap.
        result.Parts[0].Staves.Should().HaveCount(1,
            "known bug: MusicXmlExporter/Importer don't support more than one staff per part yet");
    }

    [Fact]
    public async Task KnownGap_SlursHairpinsDynamicsLyricsAndTies_AreNotExported()
    {
        var score = Score.CreatePianoScore("Test");
        var staff = score.Parts[0].Staves[0];
        var measure = staff.GetOrAddMeasure(1, TimeSignature.Common);
        var note = new Note
        {
            Duration     = Duration.Quarter,
            Pitch        = Pitch.MiddleC,
            Articulation = Articulation.Staccato,
            IsTiedTo     = true
        };
        note.Lyrics.Add(new Lyric { Text = "la", Syllable = LyricSyllable.Single });
        measure.AddNote(note);
        measure.Dynamics.Add(new Dynamic { Level = DynamicLevel.Ff, TickOffset = 0 });
        measure.Hairpins.Add(new Hairpin { Type = HairpinType.Crescendo, StartTick = 0, EndTick = 512 });
        measure.Slurs.Add(new Slur { StartNoteId = note.Id, EndNoteId = note.Id });

        var result = await RoundTripAsync(score);
        var resultMeasure = result.Parts[0].Staves[0].Measures[0];
        var resultNote = resultMeasure.Notes.Single();

        // None of this notation markup is written by MusicXmlExporter today — only
        // pitch/rest, duration, dots, and chord notes are. Documented here so a future
        // exporter change either fixes these together or this test is updated alongside it.
        resultMeasure.Dynamics.Should().BeEmpty("dynamics aren't exported yet");
        resultMeasure.Hairpins.Should().BeEmpty("hairpins aren't exported yet");
        resultMeasure.Slurs.Should().BeEmpty("slurs aren't exported yet");
        resultNote.Lyrics.Should().BeEmpty("lyrics aren't exported yet");
        resultNote.Articulation.Should().Be(Articulation.None, "articulations aren't exported yet");
        resultNote.IsTiedTo.Should().BeFalse("ties aren't exported yet");
    }
}
