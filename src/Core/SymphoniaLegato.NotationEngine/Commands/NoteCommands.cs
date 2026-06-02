using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.NotationEngine;

internal sealed class AddNoteCommand(Guid staffId, int measureNumber, Note note) : IScoreCommand
{
    public string Description => $"Add {note} to measure {measureNumber}";

    public void Execute(Score score)
    {
        var measure = FindMeasure(score);
        measure?.AddNote(note);
    }

    public void Undo(Score score)
    {
        var measure = FindMeasure(score);
        measure?.Notes.RemoveAll(n => n.Id == note.Id);
    }

    private Measure? FindMeasure(Score score) =>
        score.Parts
             .SelectMany(p => p.Staves)
             .FirstOrDefault(s => s.Id == staffId)
             ?.GetMeasure(measureNumber);
}

internal sealed class DeleteNoteCommand(Guid staffId, int measureNumber, Guid noteId) : IScoreCommand
{
    private Note? _deletedNote;

    public string Description => $"Delete note {noteId} from measure {measureNumber}";

    public void Execute(Score score)
    {
        var measure = FindMeasure(score);
        if (measure is null) return;
        _deletedNote = measure.Notes.FirstOrDefault(n => n.Id == noteId);
        if (_deletedNote is not null)
            measure.Notes.Remove(_deletedNote);
    }

    public void Undo(Score score)
    {
        if (_deletedNote is null) return;
        var measure = FindMeasure(score);
        measure?.AddNote(_deletedNote);
    }

    private Measure? FindMeasure(Score score) =>
        score.Parts
             .SelectMany(p => p.Staves)
             .FirstOrDefault(s => s.Id == staffId)
             ?.GetMeasure(measureNumber);
}

internal sealed class ChangeTimeSignatureCommand(int measureNumber, TimeSignature newTimeSig) : IScoreCommand
{
    private readonly Dictionary<Guid, TimeSignature> _previous = new();

    public string Description => $"Change time signature to {newTimeSig} at measure {measureNumber}";

    public void Execute(Score score)
    {
        foreach (var staff in score.Parts.SelectMany(p => p.Staves))
        {
            var measure = staff.GetMeasure(measureNumber);
            if (measure is null) continue;
            _previous[staff.Id] = measure.TimeSignature;
            measure.TimeSignature = newTimeSig;
        }
    }

    public void Undo(Score score)
    {
        foreach (var staff in score.Parts.SelectMany(p => p.Staves))
        {
            if (!_previous.TryGetValue(staff.Id, out var prev)) continue;
            var measure = staff.GetMeasure(measureNumber);
            if (measure is not null) measure.TimeSignature = prev;
        }
    }
}

internal sealed class ChangeKeySignatureCommand(int measureNumber, KeySignature newKeySig) : IScoreCommand
{
    private KeySignature? _previous;

    public string Description => $"Change key signature to {newKeySig} at measure {measureNumber}";

    public void Execute(Score score)
    {
        foreach (var staff in score.Parts.SelectMany(p => p.Staves))
        {
            var measure = staff.GetMeasure(measureNumber);
            if (measure is null) continue;
            _previous ??= measure.KeySignatureChange ?? score.InitialKeySignature;
            measure.KeySignatureChange = newKeySig;
        }
    }

    public void Undo(Score score)
    {
        foreach (var staff in score.Parts.SelectMany(p => p.Staves))
        {
            var measure = staff.GetMeasure(measureNumber);
            if (measure is not null) measure.KeySignatureChange = _previous;
        }
    }
}

internal sealed class AddMeasuresCommand(int afterMeasure, int count) : IScoreCommand
{
    public string Description => $"Add {count} measure(s) after measure {afterMeasure}";

    public void Execute(Score score)
    {
        var timeSig = GetTimeSigAt(score, afterMeasure);
        foreach (var staff in score.Parts.SelectMany(p => p.Staves))
        {
            // Shift existing measures up
            foreach (var m in staff.Measures.Where(m => m.Number > afterMeasure))
                m.Number += count;

            for (int i = 1; i <= count; i++)
                staff.GetOrAddMeasure(afterMeasure + i, timeSig);
        }
    }

    public void Undo(Score score)
    {
        foreach (var staff in score.Parts.SelectMany(p => p.Staves))
        {
            staff.Measures.RemoveAll(m => m.Number > afterMeasure && m.Number <= afterMeasure + count);
            foreach (var m in staff.Measures.Where(m => m.Number > afterMeasure + count))
                m.Number -= count;
        }
    }

    private static TimeSignature GetTimeSigAt(Score score, int measureNumber)
    {
        var staff = score.Parts.SelectMany(p => p.Staves).FirstOrDefault();
        return staff?.GetMeasure(measureNumber)?.TimeSignature ?? score.InitialTimeSignature;
    }
}

internal sealed class DeleteMeasureCommand(int measureNumber) : IScoreCommand
{
    private readonly Dictionary<Guid, Measure> _deleted = new();

    public string Description => $"Delete measure {measureNumber}";

    public void Execute(Score score)
    {
        foreach (var staff in score.Parts.SelectMany(p => p.Staves))
        {
            var m = staff.GetMeasure(measureNumber);
            if (m is not null)
            {
                _deleted[staff.Id] = m;
                staff.Measures.Remove(m);
            }
            foreach (var rem in staff.Measures.Where(x => x.Number > measureNumber))
                rem.Number--;
        }
    }

    public void Undo(Score score)
    {
        foreach (var staff in score.Parts.SelectMany(p => p.Staves))
        {
            if (!_deleted.TryGetValue(staff.Id, out var m)) continue;
            foreach (var rem in staff.Measures.Where(x => x.Number >= measureNumber))
                rem.Number++;
            staff.Measures.Add(m);
            staff.Measures.Sort((a, b) => a.Number.CompareTo(b.Number));
        }
    }
}
