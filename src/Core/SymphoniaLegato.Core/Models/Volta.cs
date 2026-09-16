// File: Volta.cs
// Description: Volta bracket (first/second ending) model.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-01
// Last edit date: 2026-09-15
// Version: 1.0.0

namespace SymphoniaLegato.Core.Models;

/// <summary>A volta bracket (first ending, second ending, etc.).</summary>
public sealed class VoltaBracket
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int StartMeasure { get; set; }
    public int EndMeasure { get; set; }
    public int VoltaNumber { get; set; } = 1;
    public bool IsOpen { get; set; }  // open = no closing barline on right
}
