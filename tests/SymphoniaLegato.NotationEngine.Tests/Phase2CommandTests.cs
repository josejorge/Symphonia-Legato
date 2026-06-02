using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.NotationEngine;
using Xunit;

namespace SymphoniaLegato.NotationEngine.Tests;

public sealed class Phase2CommandTests
{
    private static ScoreEditor CreateEditorWithNote(out Guid staffId, out Guid noteId, out int measureNumber)
    {
        var score = Score.CreatePianoScore("Test");
        var staff = score.Parts[0].Staves[0];
        staffId = staff.Id;
        measureNumber = 1;
        staff.GetOrAddMeasure(1, TimeSignature.Common);

        var editor = new ScoreEditor(score, NullLogger<ScoreEditor>.Instance);
        var note = new Note { Duration = Duration.Quarter, Pitch = Pitch.MiddleC, StaffPosition = 1 };
        editor.AddNote(staff.Id, 1, note);
        noteId = note.Id;
        return editor;
    }

    [Fact]
    public void SetArticulation_ChangesNote()
    {
        var editor = CreateEditorWithNote(out var staffId, out var noteId, out var mNum);
        editor.SetArticulation(staffId, mNum, noteId, Articulation.Staccato);

        var note = editor.Score.Parts[0].Staves[0].GetMeasure(mNum)!
                         .Notes.First(n => n.Id == noteId);
        note.Articulation.Should().Be(Articulation.Staccato);
    }

    [Fact]
    public void SetArticulation_CanUndo()
    {
        var editor = CreateEditorWithNote(out var staffId, out var noteId, out var mNum);
        editor.SetArticulation(staffId, mNum, noteId, Articulation.Accent);
        editor.Undo();

        var note = editor.Score.Parts[0].Staves[0].GetMeasure(mNum)!
                         .Notes.First(n => n.Id == noteId);
        note.Articulation.Should().Be(Articulation.None);
    }

    [Fact]
    public void AddDynamic_AppearInMeasure()
    {
        var editor = CreateEditorWithNote(out var staffId, out _, out var mNum);
        var dyn = new Dynamic { Level = DynamicLevel.Ff };
        editor.AddDynamic(staffId, mNum, dyn);

        var measure = editor.Score.Parts[0].Staves[0].GetMeasure(mNum)!;
        measure.Dynamics.Should().ContainSingle(d => d.Id == dyn.Id);
    }

    [Fact]
    public void AddDynamic_CanUndo()
    {
        var editor = CreateEditorWithNote(out var staffId, out _, out var mNum);
        var dyn = new Dynamic { Level = DynamicLevel.P };
        editor.AddDynamic(staffId, mNum, dyn);
        editor.Undo();

        var measure = editor.Score.Parts[0].Staves[0].GetMeasure(mNum)!;
        measure.Dynamics.Should().BeEmpty();
    }

    [Fact]
    public void SetHand_AssignsHand()
    {
        var editor = CreateEditorWithNote(out var staffId, out var noteId, out var mNum);
        editor.SetHand(staffId, mNum, noteId, Hand.Right);

        var note = editor.Score.Parts[0].Staves[0].GetMeasure(mNum)!
                         .Notes.First(n => n.Id == noteId);
        note.Hand.Should().Be(Hand.Right);
    }

    [Fact]
    public void AddLyric_AttachesToNote()
    {
        var editor = CreateEditorWithNote(out var staffId, out var noteId, out var mNum);
        var lyric = new Lyric { NoteId = noteId, Text = "Hel", Syllable = LyricSyllable.Begin };
        editor.AddLyric(staffId, mNum, noteId, lyric);

        var note = editor.Score.Parts[0].Staves[0].GetMeasure(mNum)!
                         .Notes.First(n => n.Id == noteId);
        note.Lyrics.Should().ContainSingle(l => l.Text == "Hel");
    }

    [Fact]
    public void PostProcess_AssignsBeamGroupsAfterNoteAdd()
    {
        var score = Score.CreatePianoScore("Test");
        var staff = score.Parts[0].Staves[0];
        staff.GetOrAddMeasure(1, TimeSignature.Common);

        var editor = new ScoreEditor(score, NullLogger<ScoreEditor>.Instance);

        for (int i = 0; i < 4; i++)
            editor.AddNote(staff.Id, 1,
                new Note { Duration = Duration.Eighth, Pitch = Pitch.MiddleC, StaffPosition = 5 });

        var measure = staff.GetMeasure(1)!;
        // With 4 eighths in 4/4, expect 2 beam groups of 2
        var beamedNotes = measure.Notes.Where(n => n.BeamGroup > 0).ToList();
        beamedNotes.Should().HaveCount(4);
    }
}
