// File: IScoreCommand.cs
// Description: Command-pattern interface for every score-mutating operation (Execute/Undo).
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-01
// Last edit date: 2026-09-15
// Version: 1.0.0

using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.NotationEngine;

/// <summary>Command pattern interface for all score-mutating operations.</summary>
public interface IScoreCommand
{
    string Description { get; }
    void Execute(Score score);
    void Undo(Score score);
}
