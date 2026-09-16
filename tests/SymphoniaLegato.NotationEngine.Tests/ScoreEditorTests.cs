// File: ScoreEditorTests.cs
// Description: Unit tests for ScoreEditor command execution and undo/redo.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-01
// Last edit date: 2026-09-15
// Version: 1.1.0

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.NotationEngine;
using Xunit;

namespace SymphoniaLegato.NotationEngine.Tests;

public sealed class ScoreEditorTests
{
    private static ScoreEditor CreateEditor()
    {
        var score = Score.CreatePianoScore("Test");
        var trebleStaff = score.Parts[0].Staves[0];
        trebleStaff.GetOrAddMeasure(1, TimeSignature.Common);
        return new ScoreEditor(score, NullLogger<ScoreEditor>.Instance);
    }

    [Fact]
    public void AddNote_AddsNoteToMeasure()
    {
        var editor = CreateEditor();
        var staff = editor.Score.Parts[0].Staves[0];
        var note = new Note { Duration = Duration.Quarter, Pitch = Pitch.MiddleC };

        editor.AddNote(staff.Id, 1, note);

        staff.GetMeasure(1)!.Notes.Should().ContainSingle(n => n.Id == note.Id);
    }

    [Fact]
    public void Undo_RemovesAddedNote()
    {
        var editor = CreateEditor();
        var staff = editor.Score.Parts[0].Staves[0];
        var note = new Note { Duration = Duration.Quarter, Pitch = Pitch.MiddleC };

        editor.AddNote(staff.Id, 1, note);
        editor.Undo();

        staff.GetMeasure(1)!.Notes.Should().BeEmpty();
    }

    [Fact]
    public void Redo_RestoresNote()
    {
        var editor = CreateEditor();
        var staff = editor.Score.Parts[0].Staves[0];
        var note = new Note { Duration = Duration.Quarter, Pitch = Pitch.MiddleC };

        editor.AddNote(staff.Id, 1, note);
        editor.Undo();
        editor.Redo();

        staff.GetMeasure(1)!.Notes.Should().ContainSingle(n => n.Id == note.Id);
    }

    [Fact]
    public void DeleteNote_RemovesNote()
    {
        var editor = CreateEditor();
        var staff = editor.Score.Parts[0].Staves[0];
        var note = new Note { Duration = Duration.Quarter, Pitch = Pitch.MiddleC };

        editor.AddNote(staff.Id, 1, note);
        editor.DeleteNote(staff.Id, 1, note.Id);

        staff.GetMeasure(1)!.Notes.Should().BeEmpty();
    }

    [Fact]
    public void IsDirty_SetAfterEdit_ClearedAfterSave()
    {
        var editor = CreateEditor();
        var staff = editor.Score.Parts[0].Staves[0];

        editor.IsDirty.Should().BeFalse();
        editor.AddNote(staff.Id, 1, new Note { Duration = Duration.Quarter });
        editor.IsDirty.Should().BeTrue();
        editor.MarkSaved();
        editor.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void AddMeasures_InsertsCorrectly()
    {
        var editor = CreateEditor();
        var staff = editor.Score.Parts[0].Staves[0];

        editor.AddMeasures(1, 3);

        staff.Measures.Should().HaveCount(4);
        staff.Measures.Select(m => m.Number).Should().BeEquivalentTo([1, 2, 3, 4]);
    }

    // ── Chord entry (AddChordPitchCommand) ──────────────────────────────

    [Fact]
    public void AddChordPitch_StacksPitchOntoExistingNote()
    {
        var editor = CreateEditor();
        var staff = editor.Score.Parts[0].Staves[0];
        var note = new Note { Duration = Duration.Quarter, Pitch = Pitch.MiddleC };
        editor.AddNote(staff.Id, 1, note);

        var third = new Pitch(NoteName.E, Accidental.Natural, 4);
        editor.AddChordPitch(staff.Id, 1, note.Id, third);

        note.ChordNotes.Should().ContainSingle().Which.Should().Be(third);
    }

    [Fact]
    public void AddChordPitch_Undo_RemovesOnlyThePitchItAdded()
    {
        var editor = CreateEditor();
        var staff = editor.Score.Parts[0].Staves[0];
        var note = new Note { Duration = Duration.Quarter, Pitch = Pitch.MiddleC };
        editor.AddNote(staff.Id, 1, note);
        editor.AddChordPitch(staff.Id, 1, note.Id, new Pitch(NoteName.E, Accidental.Natural, 4));

        editor.Undo();

        note.ChordNotes.Should().BeEmpty();
    }

    [Fact]
    public void AddChordPitch_OnRest_DoesNotAddAPitch()
    {
        var editor = CreateEditor();
        var staff = editor.Score.Parts[0].Staves[0];
        var rest = new Note { Duration = Duration.Quarter }; // Pitch left null = rest
        editor.AddNote(staff.Id, 1, rest);

        editor.AddChordPitch(staff.Id, 1, rest.Id, Pitch.MiddleC);

        rest.ChordNotes.Should().BeEmpty();
    }

    [Fact]
    public void AddChordPitch_DuplicateOfRootPitch_IsNotAddedTwice()
    {
        var editor = CreateEditor();
        var staff = editor.Score.Parts[0].Staves[0];
        var note = new Note { Duration = Duration.Quarter, Pitch = Pitch.MiddleC };
        editor.AddNote(staff.Id, 1, note);

        editor.AddChordPitch(staff.Id, 1, note.Id, Pitch.MiddleC);

        note.ChordNotes.Should().BeEmpty();
    }
}
