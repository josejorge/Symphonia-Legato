using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.NotationEngine;

/// <summary>Command pattern interface for all score-mutating operations.</summary>
public interface IScoreCommand
{
    string Description { get; }
    void Execute(Score score);
    void Undo(Score score);
}
