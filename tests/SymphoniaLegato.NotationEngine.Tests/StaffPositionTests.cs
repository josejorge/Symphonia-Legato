using FluentAssertions;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.NotationEngine;
using Xunit;

namespace SymphoniaLegato.NotationEngine.Tests;

public sealed class StaffPositionTests
{
    [Theory]
    [InlineData(NoteName.E, Accidental.Natural, 4, ClefType.Treble, 1)]  // E4 = bottom line treble
    [InlineData(NoteName.G, Accidental.Natural, 4, ClefType.Treble, 3)]  // G4 = 2nd line treble
    [InlineData(NoteName.B, Accidental.Natural, 4, ClefType.Treble, 5)]  // B4 = 3rd line treble
    [InlineData(NoteName.G, Accidental.Natural, 2, ClefType.Bass,   1)]  // G2 = bottom line bass
    public void Calculate_ReturnsCorrectPosition(
        NoteName name, Accidental acc, int octave, ClefType clefType, int expectedPos)
    {
        var pitch = new Pitch(name, acc, octave);
        var clef  = clefType == ClefType.Treble ? Clef.Treble : Clef.Bass;
        StaffPositionCalculator.Calculate(pitch, clef).Should().Be(expectedPos);
    }

    [Fact]
    public void LedgerLines_MiddleC_BelowTreble()
    {
        var pitch = Pitch.MiddleC;
        int pos = StaffPositionCalculator.Calculate(pitch, Clef.Treble);
        pos.Should().BeLessThan(1); // below bottom line
        var (count, above) = StaffPositionCalculator.GetLedgerLines(pos);
        count.Should().BeGreaterThan(0);
        above.Should().BeFalse();
    }
}
