// File: Lyric.cs
// Description: Lyric syllable attached to a note.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-01
// Last edit date: 2026-09-15
// Version: 1.0.0

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
