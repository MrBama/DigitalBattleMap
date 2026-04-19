using DigitalBattleMap.FogShapes;
using System.Windows.Media;

namespace DigitalBattleMap.UIElements;

/// <summary>
/// Represents tracked data for a fog shape needed for visualization.
/// Decouples the visualization layer from the FogShape class.
/// </summary>
public class ShapeData
{
    /// <summary>
    /// Reference to the original FogShape.
    /// </summary>
    public FogShape FogShape { get; set; }

    /// <summary>
    /// The path geometry representation of the shape.
    /// </summary>
    public PathGeometry Geometry { get; set; }

    /// <summary>
    /// The outer stroke color (from FogShape.ColorOuter).
    /// </summary>
    public Color ColorOuter { get; set; }

    /// <summary>
    /// The inner stroke color (from FogShape.ColorInner).
    /// </summary>
    public Color ColorInner { get; set; }

    /// <summary>
    /// The pen size in canvas coordinates (from FogShape.PenSizeCanvas).
    /// </summary>
    public double PenSizeCanvas { get; set; }

    /// <summary>
    /// Whether the shape is enabled as fog.
    /// </summary>
    public bool IsFogEnabled { get; set; }

    public ShapeData(FogShape fogShape, PathGeometry geometry)
    {
        FogShape = fogShape;
        Geometry = geometry;
        ColorOuter = fogShape.ColorOuter;
        ColorInner = fogShape.ColorInner;
        PenSizeCanvas = fogShape.PenSizeCanvas;
        IsFogEnabled = fogShape.IsFogEnabled;
    }

    /// <summary>
    /// Updates the tracked data from the FogShape.
    /// </summary>
    public void UpdateFromFogShape()
    {
        ColorOuter = FogShape.ColorOuter;
        ColorInner = FogShape.ColorInner;
        PenSizeCanvas = FogShape.PenSizeCanvas;
        IsFogEnabled = FogShape.IsFogEnabled;
    }
}
