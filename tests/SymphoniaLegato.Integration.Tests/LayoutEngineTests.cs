// File: LayoutEngineTests.cs
// Description: Regression tests for LayoutEngine's proportional measure-width computation across a grand staff (see docs/BUGS.md — measure width used to be driven only by the first staff).
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-09-15
// Last edit date: 2026-09-15
// Version: 1.0.0

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SymphoniaLegato.Core.Interfaces;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.NotationEngine;
using Xunit;
using LayoutEngineImpl = SymphoniaLegato.LayoutEngine.LayoutEngine;

namespace SymphoniaLegato.Integration.Tests;

public sealed class LayoutEngineTests
{
    private readonly ILayoutEngine _layout = new LayoutEngineImpl(NullLogger<LayoutEngineImpl>.Instance);

    [Fact]
    public void ComputeLayout_MeasureWidth_DrivenByBusiestStaff_NotJustTheFirst()
    {
        // A grand staff: both staves simple in measure 1 (a "balanced" control measure),
        // but in measure 2 the treble (the first staff) stays sparse while the bass gets
        // busy — 16 sixteenth notes filling the bar, versus the treble's single half
        // note. Before the fix, EstimateMeasureWidth read only the first staff (treble),
        // so measure 2 — which actually needs far more room for the bass — was sized as
        // if it were just as simple as measure 1, and ended up *narrower* than measure 1
        // despite needing to fit 8x as many notes on one of its staves. That's the exact
        // "everything crammed together" symptom reported against the app.
        var score = Score.CreatePianoScore("Test");
        var treble = score.Parts[0].Staves[0];
        var bass   = score.Parts[0].Staves[1];
        var editor = new ScoreEditor(score, NullLogger<ScoreEditor>.Instance);
        editor.AddMeasures(0, 2);

        // Measure 1: balanced — one simple note per staff.
        editor.AddNote(treble.Id, 1, new Note { Duration = Duration.Half, Pitch = Pitch.MiddleC });
        editor.AddNote(bass.Id,   1, new Note { Duration = Duration.Half, Pitch = new Pitch(NoteName.C, Accidental.Natural, 3) });

        // Measure 2: treble stays just as sparse as measure 1's...
        editor.AddNote(treble.Id, 2, new Note { Duration = Duration.Half, Pitch = Pitch.MiddleC });
        // ...but the bass is packed with 16 sixteenth notes (a full 4/4 bar).
        for (int i = 0; i < 16; i++)
        {
            editor.AddNote(bass.Id, 2, new Note
            {
                Duration = new Duration(NoteValue.Sixteenth),
                Pitch    = new Pitch(NoteName.C, Accidental.Natural, 3)
            });
        }

        var layoutResult = _layout.ComputeLayout(score, new LayoutOptions());
        var bassStaff = layoutResult.Pages[0].Systems[0].Staves.Single(s => s.StaffId == bass.Id);
        double measure1Width = bassStaff.Measures[0].Width;
        double measure2Width = bassStaff.Measures[1].Width;

        // Measure 2 has 8x the note content of measure 1 on its busiest staff — it must
        // end up wider, not narrower, regardless of the treble staying sparse.
        measure2Width.Should().BeGreaterThan(measure1Width);
    }

    [Fact]
    public void ComputeLayout_BusyStaffNotes_GetAtLeastMinimumSpacing()
    {
        // Same scenario as above, but asserts the concrete, user-visible consequence:
        // the 16 busy bass notes in measure 2 must not be squeezed closer together
        // than a sane minimum on-screen spacing.
        var score = Score.CreatePianoScore("Test");
        var treble = score.Parts[0].Staves[0];
        var bass   = score.Parts[0].Staves[1];
        var editor = new ScoreEditor(score, NullLogger<ScoreEditor>.Instance);
        editor.AddMeasures(0, 2);

        editor.AddNote(treble.Id, 1, new Note { Duration = Duration.Half, Pitch = Pitch.MiddleC });
        editor.AddNote(bass.Id,   1, new Note { Duration = Duration.Half, Pitch = new Pitch(NoteName.C, Accidental.Natural, 3) });
        editor.AddNote(treble.Id, 2, new Note { Duration = Duration.Half, Pitch = Pitch.MiddleC });
        for (int i = 0; i < 16; i++)
        {
            editor.AddNote(bass.Id, 2, new Note
            {
                Duration = new Duration(NoteValue.Sixteenth),
                Pitch    = new Pitch(NoteName.C, Accidental.Natural, 3)
            });
        }

        var layoutResult = _layout.ComputeLayout(score, new LayoutOptions());
        var bassMeasure2 = layoutResult.Pages[0].Systems[0].Staves
            .Single(s => s.StaffId == bass.Id).Measures[1];

        var xs = bassMeasure2.Elements.OrderBy(e => e.X).Select(e => e.X).ToList();
        var gaps = xs.Zip(xs.Skip(1), (a, b) => b - a);

        gaps.Should().OnlyContain(g => g > 4.0, "no two consecutive notes should be squeezed closer than a few pixels apart");
    }
}
