using FluentAssertions;
using SymphoniaLegato.Core.Models;
using Xunit;

namespace SymphoniaLegato.Core.Tests;

public sealed class PitchTests
{
    [Fact]
    public void MiddleC_HasMidiNumber60()
    {
        Pitch.MiddleC.MidiNumber.Should().Be(60);
    }

    [Theory]
    [InlineData(NoteName.C, Accidental.Natural,  4, 60)]
    [InlineData(NoteName.A, Accidental.Natural,  4, 69)]
    [InlineData(NoteName.C, Accidental.Sharp,    4, 61)]
    [InlineData(NoteName.B, Accidental.Natural,  3, 59)]
    [InlineData(NoteName.C, Accidental.Natural,  5, 72)]
    [InlineData(NoteName.C, Accidental.Natural,  0, 12)]
    public void MidiNumber_ComputesCorrectly(NoteName name, Accidental acc, int octave, int expected)
    {
        var pitch = new Pitch(name, acc, octave);
        pitch.MidiNumber.Should().Be(expected);
    }

    [Theory]
    [InlineData(60, NoteName.C, Accidental.Natural,  4)]
    [InlineData(69, NoteName.A, Accidental.Natural,  4)]
    [InlineData(61, NoteName.C, Accidental.Sharp,    4)]
    [InlineData(59, NoteName.B, Accidental.Natural,  3)]
    public void FromMidi_RoundTrips(int midi, NoteName expectedName, Accidental expectedAcc, int expectedOctave)
    {
        var pitch = Pitch.FromMidi(midi);
        pitch.Name.Should().Be(expectedName);
        pitch.Accidental.Should().Be(expectedAcc);
        pitch.Octave.Should().Be(expectedOctave);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        new Pitch(NoteName.F, Accidental.Sharp, 4).ToString().Should().Be("F#4");
        new Pitch(NoteName.B, Accidental.Flat,  3).ToString().Should().Be("Bb3");
        Pitch.MiddleC.ToString().Should().Be("C4");
    }
}
