using Microsoft.Extensions.Logging;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.NotationEngine;

/// <summary>
/// Stateful score editor that wraps a <see cref="Score"/> and exposes
/// command-based mutation with full undo/redo support.
/// </summary>
public sealed class ScoreEditor
{
    private readonly ILogger<ScoreEditor> _logger;
    private readonly Stack<IScoreCommand> _undoStack = new();
    private readonly Stack<IScoreCommand> _redoStack = new();

    public Score Score { get; private set; }
    public bool IsDirty { get; private set; }
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event EventHandler? ScoreChanged;
    public event EventHandler<string>? CommandExecuted;

    public ScoreEditor(Score score, ILogger<ScoreEditor> logger)
    {
        Score = score;
        _logger = logger;
    }

    public void Execute(IScoreCommand command)
    {
        command.Execute(Score);
        _undoStack.Push(command);
        _redoStack.Clear();
        IsDirty = true;
        PostProcess();
        _logger.LogDebug("Executed: {Command}", command.Description);
        ScoreChanged?.Invoke(this, EventArgs.Empty);
        CommandExecuted?.Invoke(this, command.Description);
    }

    /// <summary>Runs beam and accidental processing after any mutation.</summary>
    private void PostProcess()
    {
        var keySig = Score.InitialKeySignature;
        foreach (var part in Score.Parts)
        foreach (var staff in part.Staves)
        foreach (var measure in staff.Measures)
        {
            var effectiveKey = measure.KeySignatureChange ?? keySig;
            BeamCalculator.AssignBeams(measure);
            AccidentalProcessor.Process(measure, effectiveKey);
            if (measure.KeySignatureChange.HasValue)
                keySig = measure.KeySignatureChange.Value;

            // Set stem direction for non-beamed notes
            foreach (var note in measure.Notes.Where(n => n.BeamGroup == 0))
                note.Stem = StemDirectionCalculator.Calculate(note);
        }
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var command = _undoStack.Pop();
        command.Undo(Score);
        _redoStack.Push(command);
        IsDirty = true;
        _logger.LogDebug("Undone: {Command}", command.Description);
        ScoreChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var command = _redoStack.Pop();
        command.Execute(Score);
        _undoStack.Push(command);
        IsDirty = true;
        _logger.LogDebug("Redone: {Command}", command.Description);
        ScoreChanged?.Invoke(this, EventArgs.Empty);
    }

    public void MarkSaved() => IsDirty = false;

    // ── Convenience factory methods ────────────────────────────────────

    public void AddNote(Guid staffId, int measureNumber, Note note) =>
        Execute(new AddNoteCommand(staffId, measureNumber, note));

    public void DeleteNote(Guid staffId, int measureNumber, Guid noteId) =>
        Execute(new DeleteNoteCommand(staffId, measureNumber, noteId));

    public void ChangeTimeSignature(int measureNumber, TimeSignature timeSig) =>
        Execute(new ChangeTimeSignatureCommand(measureNumber, timeSig));

    public void ChangeKeySignature(int measureNumber, KeySignature keySig) =>
        Execute(new ChangeKeySignatureCommand(measureNumber, keySig));

    public void AddMeasures(int afterMeasure, int count) =>
        Execute(new AddMeasuresCommand(afterMeasure, count));

    public void DeleteMeasure(int measureNumber) =>
        Execute(new DeleteMeasureCommand(measureNumber));

    // ── Notation helpers ──────────────────────────────────────────────

    public void AddDynamic(Guid staffId, int measureNumber, Dynamic dynamic) =>
        Execute(new AddDynamicCommand(staffId, measureNumber, dynamic));

    public void AddHairpin(Guid staffId, int measureNumber, Hairpin hairpin) =>
        Execute(new AddHairpinCommand(staffId, measureNumber, hairpin));

    public void AddSlur(Guid staffId, int measureNumber, Slur slur) =>
        Execute(new AddSlurCommand(staffId, measureNumber, slur));

    public void AddTempoMarking(int measureNumber, TempoMarking tempo) =>
        Execute(new AddTempoMarkingCommand(measureNumber, tempo));

    public void AddLyric(Guid staffId, int measureNumber, Guid noteId, Lyric lyric) =>
        Execute(new AddLyricCommand(staffId, measureNumber, noteId, lyric));

    public void SetArticulation(Guid staffId, int measureNumber, Guid noteId, Articulation articulation) =>
        Execute(new SetArticulationCommand(staffId, measureNumber, noteId, articulation));

    public void SetHand(Guid staffId, int measureNumber, Guid noteId, Hand hand) =>
        Execute(new SetHandCommand(staffId, measureNumber, noteId, hand));
}
