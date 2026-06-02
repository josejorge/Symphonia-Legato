using FluentAssertions;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.NotationEngine;
using Xunit;

namespace SymphoniaLegato.NotationEngine.Tests;

public sealed class BeamCalculatorTests
{
    [Fact]
    public void TwoEighths_InSameBeat_GetSameBeamGroup()
    {
        var measure = new Measure { TimeSignature = TimeSignature.Common };
        var n1 = new Note { Duration = Duration.Eighth, Pitch = Pitch.MiddleC, StaffPosition = 1 };
        var n2 = new Note { Duration = Duration.Eighth, Pitch = Pitch.MiddleC, StaffPosition = 1 };
        measure.AddNote(n1);
        measure.AddNote(n2);

        BeamCalculator.AssignBeams(measure);

        n1.BeamGroup.Should().Be(n2.BeamGroup).And.BePositive();
        n1.IsBeamStart.Should().BeTrue();
        n2.IsBeamEnd.Should().BeTrue();
    }

    [Fact]
    public void QuarterNotes_NeverBeamed()
    {
        var measure = new Measure { TimeSignature = TimeSignature.Common };
        for (int i = 0; i < 4; i++)
            measure.AddNote(new Note { Duration = Duration.Quarter, Pitch = Pitch.MiddleC });

        BeamCalculator.AssignBeams(measure);

        measure.Notes.Should().AllSatisfy(n => n.BeamGroup.Should().Be(0));
    }

    [Fact]
    public void FourEighths_BeatBoundary_TwoGroups()
    {
        // 4/4 beat groups = quarter note = 1024 ticks
        // Eighths: 512 ticks each
        // Notes 0-512 in beat 1, notes 512-1024 in beat 2, etc.
        // Four eighths span beats 1-2: two groups of 2
        var measure = new Measure { TimeSignature = TimeSignature.Common };
        for (int i = 0; i < 4; i++)
            measure.AddNote(new Note { Duration = Duration.Eighth, Pitch = Pitch.MiddleC, StaffPosition = 1 });

        BeamCalculator.AssignBeams(measure);

        var groups = measure.Notes.Where(n => n.BeamGroup > 0)
                                  .GroupBy(n => n.BeamGroup).ToList();
        groups.Should().HaveCount(2);
        groups.All(g => g.Count() == 2).Should().BeTrue();
    }

    [Fact]
    public void StemDirection_CalcForEighthAboveMiddle()
    {
        // Position 6 is above the middle line (5), so stem should go down
        var note = new Note { Duration = Duration.Eighth, StaffPosition = 6,
            Pitch = new Pitch(NoteName.D, Accidental.Natural, 5) };
        note.Stem = StemDirectionCalculator.Calculate(note);
        note.Stem.Should().Be(StemDirection.Down);
    }

    [Fact]
    public void StemDirection_BelowMiddle_Up()
    {
        var note = new Note { Duration = Duration.Eighth, StaffPosition = 3, Pitch = Pitch.MiddleC };
        note.Stem = StemDirectionCalculator.Calculate(note);
        note.Stem.Should().Be(StemDirection.Up);
    }

    [Fact]
    public void WholeNote_NoStem()
    {
        var note = new Note { Duration = Duration.Whole, StaffPosition = 5 };
        note.Stem = StemDirectionCalculator.Calculate(note);
        note.Stem.Should().Be(StemDirection.None);
    }
}
