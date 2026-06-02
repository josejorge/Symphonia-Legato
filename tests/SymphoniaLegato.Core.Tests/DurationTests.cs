using FluentAssertions;
using SymphoniaLegato.Core.Models;
using Xunit;

namespace SymphoniaLegato.Core.Tests;

public sealed class DurationTests
{
    [Theory]
    [InlineData(NoteValue.Whole,    0, 4096)]
    [InlineData(NoteValue.Half,     0, 2048)]
    [InlineData(NoteValue.Quarter,  0, 1024)]
    [InlineData(NoteValue.Eighth,   0, 512)]
    [InlineData(NoteValue.Sixteenth,0, 256)]
    [InlineData(NoteValue.Half,     1, 3072)] // dotted half
    [InlineData(NoteValue.Quarter,  1, 1536)] // dotted quarter
    [InlineData(NoteValue.Eighth,   1, 768)]  // dotted eighth
    [InlineData(NoteValue.Quarter,  2, 1792)] // double-dotted quarter
    public void Ticks_ComputeCorrectly(NoteValue value, int dots, int expectedTicks)
    {
        new Duration(value, dots).Ticks.Should().Be(expectedTicks);
    }

    [Fact]
    public void TimeSignature_TicksPerMeasure_ForCommonTime()
    {
        TimeSignature.Common.TicksPerMeasure.Should().Be(4096); // 4 quarters
    }

    [Fact]
    public void TimeSignature_TicksPerMeasure_ForSixEight()
    {
        TimeSignature.SixEight.TicksPerMeasure.Should().Be(3072); // 6 eighths = 3 quarters
    }
}
