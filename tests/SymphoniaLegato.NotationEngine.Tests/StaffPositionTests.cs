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

    [Theory]
    [InlineData(NoteName.E, 4, ClefType.Treble)]  // bottom line treble
    [InlineData(NoteName.G, 4, ClefType.Treble)]
    [InlineData(NoteName.B, 4, ClefType.Treble)]
    [InlineData(NoteName.C, 4, ClefType.Treble)]  // middle C, below the staff
    [InlineData(NoteName.A, 5, ClefType.Treble)]  // above the staff
    [InlineData(NoteName.G, 2, ClefType.Bass)]    // bottom line bass
    [InlineData(NoteName.D, 3, ClefType.Bass)]    // middle line bass
    public void FromStaffPosition_IsInverseOfCalculate(NoteName name, int octave, ClefType clefType)
    {
        var clef     = clefType == ClefType.Treble ? Clef.Treble : Clef.Bass;
        var original = new Pitch(name, Accidental.Natural, octave);

        int pos          = StaffPositionCalculator.Calculate(original, clef);
        var reconstructed = StaffPositionCalculator.FromStaffPosition(pos, clef, KeySignature.CMajor);

        reconstructed.Name.Should().Be(name);
        reconstructed.Octave.Should().Be(octave);
        reconstructed.MidiNumber.Should().Be(original.MidiNumber);
    }
}
