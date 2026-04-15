using DigitalBattleMap.DataClasses;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace DigitalBattleMap.FogShapes.Region;

/// <summary>
/// Represents a resolved fog region after geometry operations.
/// This is the output of FogShapeResolver and serves as the bridge between
/// shape resolution logic and UI rendering (FogCanvas).
/// Contains all properties from the original FogShape needed for rendering.
/// </summary>
public sealed class ResolvedFogRegion
{
    public ResolvedRegion Region { get; }

    // FogShape rendering properties
    public ObservableCollection<Point<double>> Points { get; }
    public Color ColorOuter { get; }
    public Color ColorInner { get; }
    public double PenSize { get; }
    public double PenSizeCanvas { get; }
    public bool IsFogEnabled { get; }

    public ResolvedFogRegion(
        ResolvedRegion region,
        ObservableCollection<Point<double>> points,
        Color colorOuter,
        Color colorInner,
        double penSize,
        double penSizeCanvas,
        bool isFogEnabled)
    {
        Region = region ?? throw new ArgumentNullException(nameof(region));
        Points = points ?? throw new ArgumentNullException(nameof(points));
        ColorOuter = colorOuter;
        ColorInner = colorInner;
        PenSize = penSize;
        PenSizeCanvas = penSizeCanvas;
        IsFogEnabled = isFogEnabled;
    }
}
