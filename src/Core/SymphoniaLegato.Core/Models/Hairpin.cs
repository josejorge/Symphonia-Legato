// File: Hairpin.cs
// Description: Crescendo/decrescendo hairpin spanning a tick range.
// Author: Jose-Jorge HERNANDEZ
// Company: N/A (personal open-source project, MIT licensed)
// Date: 2026-06-01
// Last edit date: 2026-09-15
// Version: 1.0.0

namespace SymphoniaLegato.Core.Models;

public enum HairpinType { Crescendo, Decrescendo }

/// <summary>A crescendo or decrescendo hairpin spanning a range of ticks.</summary>
public sealed class Hairpin
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public HairpinType Type { get; set; } = HairpinType.Crescendo;
    public int StartTick { get; set; }
    public int EndTick { get; set; }
    public DynamicLevel? StartLevel { get; set; }
    public DynamicLevel? EndLevel { get; set; }
}
