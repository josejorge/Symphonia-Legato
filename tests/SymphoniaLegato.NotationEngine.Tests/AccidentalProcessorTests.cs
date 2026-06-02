using FluentAssertions;
using SymphoniaLegato.Core.Models;
using SymphoniaLegato.NotationEngine;
using Xunit;

namespace SymphoniaLegato.NotationEngine.Tests;

public sealed class AccidentalProcessorTests
{
    [Fact]
    public void NaturalNote_InCMajor_NoAccidental()
    {
        var measure = new Measure { TimeSignature = TimeSignature.Common };
        var note = new Note { Duration = Duration.Quarter,
            Pitch = new Pitch(NoteName.G, Accidental.Natural, 4) };
        measure.AddNote(note);

        AccidentalProcessor.Process(measure, KeySignature.CMajor);

        note.ShowAccidental.Should().BeFalse();
    }

    [Fact]
    public void SharpNote_InCMajor_ShowsAccidental()
    {
        var measure = new Measure { TimeSignature = TimeSignature.Common };
        var note = new Note { Duration = Duration.Quarter,
            Pitch = new Pitch(NoteName.F, Accidental.Sharp, 4) };
        measure.AddNote(note);

        AccidentalProcessor.Process(measure, KeySignature.CMajor);

        note.ShowAccidental.Should().BeTrue();
    }

    [Fact]
    public void SharpNote_AlreadyInKey_NoAccidental()
    {
        // G major has F# in the key signature
        var measure = new Measure { TimeSignature = TimeSignature.Common };
        var note = new Note { Duration = Duration.Quarter,
            Pitch = new Pitch(NoteName.F, Accidental.Sharp, 4) };
        measure.AddNote(note);

        AccidentalProcessor.Process(measure, KeySignature.GMajor);

        note.ShowAccidental.Should().BeFalse();
    }

    [Fact]
    public void SameNoteAppearingTwice_SecondHasNoAccidental()
    {
        var measure = new Measure { TimeSignature = TimeSignature.Common };
        var n1 = new Note { Duration = Duration.Quarter,
            Pitch = new Pitch(NoteName.F, Accidental.Sharp, 4) };
        var n2 = new Note { Duration = Duration.Quarter,
            Pitch = new Pitch(NoteName.F, Accidental.Sharp, 4) };
        measure.AddNote(n1);
        measure.AddNote(n2);

        AccidentalProcessor.Process(measure, KeySignature.CMajor);

        n1.ShowAccidental.Should().BeTrue();
        n2.ShowAccidental.Should().BeFalse();
    }
}
