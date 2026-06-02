namespace SymphoniaLegato.Core.Models;

/// <summary>Syllable type within a word.</summary>
public enum LyricSyllable { Single, Begin, Middle, End }

/// <summary>A lyric syllable attached to a note.</summary>
public sealed class Lyric
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid NoteId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Verse { get; set; } = 1;
    public LyricSyllable Syllable { get; set; } = LyricSyllable.Single;
}
