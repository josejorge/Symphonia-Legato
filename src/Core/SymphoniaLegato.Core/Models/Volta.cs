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
