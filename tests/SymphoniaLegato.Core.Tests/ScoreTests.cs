using FluentAssertions;
using SymphoniaLegato.Core.Models;
using Xunit;

namespace SymphoniaLegato.Core.Tests;

public sealed class ScoreTests
{
    [Fact]
    public void CreatePianoScore_CreatesTwoLinkedStaves()
    {
        var score = Score.CreatePianoScore("Test");

        score.Parts.Should().HaveCount(1);
        var part = score.Parts[0];
        part.Staves.Should().HaveCount(2);

        var treble = part.Staves[0];
        var bass   = part.Staves[1];

        treble.DefaultClef.Type.Should().Be(ClefType.Treble);
        bass.DefaultClef.Type.Should().Be(ClefType.Bass);
        treble.IsGrandStaffTop.Should().BeTrue();
        treble.LinkedStaffId.Should().Be(bass.Id);
        bass.LinkedStaffId.Should().Be(treble.Id);
    }

    [Fact]
    public void Measure_AddNote_AccumulatesTicks()
    {
        var measure = new Measure { TimeSignature = TimeSignature.Common };
        measure.AddNote(new Note { Duration = Duration.Quarter });
        measure.AddNote(new Note { Duration = Duration.Quarter });

        measure.UsedTicks.Should().Be(2048);
        measure.RemainingTicks.Should().Be(2048);
        measure.IsFull.Should().BeFalse();
    }

    [Fact]
    public void Measure_IsFull_WhenFilledExactly()
    {
        var measure = new Measure { TimeSignature = TimeSignature.Common };
        for (int i = 0; i < 4; i++)
            measure.AddNote(new Note { Duration = Duration.Quarter });

        measure.IsFull.Should().BeTrue();
        measure.RemainingTicks.Should().Be(0);
    }

    [Fact]
    public void Staff_GetOrAddMeasure_ReturnsSameMeasure()
    {
        var staff = new Staff();
        var m1 = staff.GetOrAddMeasure(1, TimeSignature.Common);
        var m2 = staff.GetOrAddMeasure(1, TimeSignature.Common);
        m1.Should().BeSameAs(m2);
        staff.Measures.Should().HaveCount(1);
    }
}
