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
        _logger.LogDebug("Executed: {Command}", command.Description);
        ScoreChanged?.Invoke(this, EventArgs.Empty);
        CommandExecuted?.Invoke(this, command.Description);
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
}
