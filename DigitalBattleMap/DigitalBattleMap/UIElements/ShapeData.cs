using DigitalBattleMap.DataClasses;
using DigitalBattleMap.FogShapes;
using System.Collections.Generic;
using System.Windows.Media;

namespace DigitalBattleMap.UIElements;

public class ShapeData
{
    /// <summary>
    /// Reference to the original FogShape.
    /// </summary>
    public FogShape FogShape { get; set; }

    /// <summary>
    /// The geometry representation of the shape (may include holes via CombinedGeometry).
    /// </summary>
    public Geometry Geometry { get; set; }

    /// <summary>
    /// The pen size in canvas coordinates (from FogShape.PenSizeCanvas).
    /// </summary>
    public double PenSizeCanvas { get; set; }

    /// <summary>
    /// Whether the shape is enabled as fog.
    /// </summary>
    public bool IsFogEnabled { get; set; }

    /// <summary>
    /// The stored (clipped) boundary points used for rendering.
    /// </summary>
    public List<Point<double>> Points { get; set; }

    /// <summary>
    /// Snapshot of FogShape.Points at the last known state, used to compute
    /// transform deltas so the clipped Points stay in sync with pan/zoom.
    /// </summary>
    public List<Point<double>> OriginalFogPoints { get; set; }

    /// <summary>
    /// Shapes that are fully contained within this shape and rendered as holes.
    /// </summary>
    public List<FogShape> HoleShapes { get; set; } = new();

    public ShapeData(FogShape fogShape, PathGeometry geometry)
    {
        FogShape = fogShape;
        Geometry = geometry;
        PenSizeCanvas = fogShape.PenSizeCanvas;
        IsFogEnabled = fogShape.IsFogEnabled;
        Points = new List<Point<double>>(fogShape.Points);
        OriginalFogPoints = new List<Point<double>>(fogShape.Points);
    }

    /// <summary>
    /// Updates the tracked data from the FogShape.
    /// </summary>
    public void UpdateFromFogShape()
    {
        PenSizeCanvas = FogShape.PenSizeCanvas;
        IsFogEnabled = FogShape.IsFogEnabled;
    }
}
