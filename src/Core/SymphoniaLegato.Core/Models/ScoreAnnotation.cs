namespace SymphoniaLegato.Core.Models;

/// <summary>One continuous freehand stroke drawn on a page.</summary>
public sealed class AnnotationStroke
{
    public Guid   Id        { get; init; } = Guid.NewGuid();
    public string Color     { get; set; }  = "#FF4444";
    public double Thickness { get; set; }  = 2.0;

    /// <summary>Normalised coordinates [0,1] relative to the page bounding box.</summary>
    public List<(double X, double Y)> Points { get; init; } = [];
}

/// <summary>
/// All freehand annotations on one score page.
/// Stored inside <see cref="Score.Annotations"/> indexed by page number.
/// </summary>
public sealed class ScoreAnnotation
{
    public Guid      Id          { get; init; } = Guid.NewGuid();
    public int       PageNumber  { get; set; }  = 1;
    public DateTime  CreatedAt   { get; init; } = DateTime.UtcNow;
    public DateTime  ModifiedAt  { get; set; }  = DateTime.UtcNow;
    public List<AnnotationStroke> Strokes { get; init; } = [];

    public void ClearStrokes()
    {
        Strokes.Clear();
        ModifiedAt = DateTime.UtcNow;
    }

    public void AddStroke(AnnotationStroke stroke)
    {
        Strokes.Add(stroke);
        ModifiedAt = DateTime.UtcNow;
    }
}
