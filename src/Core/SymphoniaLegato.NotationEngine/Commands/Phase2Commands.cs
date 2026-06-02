using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.NotationEngine;

// ── Dynamic ───────────────────────────────────────────────────────────

internal sealed class AddDynamicCommand(Guid staffId, int measureNumber, Dynamic dynamic) : IScoreCommand
{
    public string Description => $"Add dynamic {dynamic.Symbol} in measure {measureNumber}";
    public void Execute(Score score) => FindMeasure(score)?.Dynamics.Add(dynamic);
    public void Undo(Score score)   => FindMeasure(score)?.Dynamics.RemoveAll(d => d.Id == dynamic.Id);
    private Measure? FindMeasure(Score score) =>
        score.Parts.SelectMany(p => p.Staves)
             .FirstOrDefault(s => s.Id == staffId)?.GetMeasure(measureNumber);
}

internal sealed class AddHairpinCommand(Guid staffId, int measureNumber, Hairpin hairpin) : IScoreCommand
{
    public string Description => $"Add {hairpin.Type} hairpin in measure {measureNumber}";
    public void Execute(Score score) => FindMeasure(score)?.Hairpins.Add(hairpin);
    public void Undo(Score score)   => FindMeasure(score)?.Hairpins.RemoveAll(h => h.Id == hairpin.Id);
    private Measure? FindMeasure(Score score) =>
        score.Parts.SelectMany(p => p.Staves)
             .FirstOrDefault(s => s.Id == staffId)?.GetMeasure(measureNumber);
}

// ── Slur ─────────────────────────────────────────────────────────────

internal sealed class AddSlurCommand(Guid staffId, int measureNumber, Slur slur) : IScoreCommand
{
    public string Description => $"Add slur in measure {measureNumber}";
    public void Execute(Score score) => FindMeasure(score)?.Slurs.Add(slur);
    public void Undo(Score score)   => FindMeasure(score)?.Slurs.RemoveAll(s => s.Id == slur.Id);
    private Measure? FindMeasure(Score score) =>
        score.Parts.SelectMany(p => p.Staves)
             .FirstOrDefault(s => s.Id == staffId)?.GetMeasure(measureNumber);
}

// ── Tempo ─────────────────────────────────────────────────────────────

internal sealed class AddTempoMarkingCommand(int measureNumber, TempoMarking tempo) : IScoreCommand
{
    public string Description => $"Add tempo \"{tempo.Text}\" at measure {measureNumber}";
    public void Execute(Score score)
    {
        foreach (var staff in score.Parts.SelectMany(p => p.Staves))
            staff.GetMeasure(measureNumber)?.TempoMarkings.Add(tempo);
    }
    public void Undo(Score score)
    {
        foreach (var staff in score.Parts.SelectMany(p => p.Staves))
            staff.GetMeasure(measureNumber)?.TempoMarkings.RemoveAll(t => t.Id == tempo.Id);
    }
}

// ── Lyric ─────────────────────────────────────────────────────────────

internal sealed class AddLyricCommand(Guid staffId, int measureNumber, Guid noteId, Lyric lyric) : IScoreCommand
{
    public string Description => $"Add lyric \"{lyric.Text}\"";
    public void Execute(Score score)
    {
        var note = FindNote(score);
        note?.Lyrics.Add(lyric);
    }
    public void Undo(Score score)
    {
        var note = FindNote(score);
        note?.Lyrics.RemoveAll(l => l.Id == lyric.Id);
    }
    private Note? FindNote(Score score) =>
        score.Parts.SelectMany(p => p.Staves)
             .FirstOrDefault(s => s.Id == staffId)
             ?.GetMeasure(measureNumber)
             ?.Notes.FirstOrDefault(n => n.Id == noteId);
}

// ── Articulation ──────────────────────────────────────────────────────

internal sealed class SetArticulationCommand(Guid staffId, int measureNumber, Guid noteId, Articulation articulation) : IScoreCommand
{
    private Articulation _previous;
    public string Description => $"Set articulation {articulation}";
    public void Execute(Score score)
    {
        var note = FindNote(score);
        if (note is null) return;
        _previous = note.Articulation;
        note.Articulation = articulation;
    }
    public void Undo(Score score)
    {
        var note = FindNote(score);
        if (note is not null) note.Articulation = _previous;
    }
    private Note? FindNote(Score score) =>
        score.Parts.SelectMany(p => p.Staves)
             .FirstOrDefault(s => s.Id == staffId)
             ?.GetMeasure(measureNumber)
             ?.Notes.FirstOrDefault(n => n.Id == noteId);
}

// ── Hand ─────────────────────────────────────────────────────────────

internal sealed class SetHandCommand(Guid staffId, int measureNumber, Guid noteId, Hand hand) : IScoreCommand
{
    private Hand _previous;
    public string Description => $"Set hand {hand}";
    public void Execute(Score score)
    {
        var note = FindNote(score);
        if (note is null) return;
        _previous = note.Hand;
        note.Hand = hand;
    }
    public void Undo(Score score)
    {
        var note = FindNote(score);
        if (note is not null) note.Hand = _previous;
    }
    private Note? FindNote(Score score) =>
        score.Parts.SelectMany(p => p.Staves)
             .FirstOrDefault(s => s.Id == staffId)
             ?.GetMeasure(measureNumber)
             ?.Notes.FirstOrDefault(n => n.Id == noteId);
}
