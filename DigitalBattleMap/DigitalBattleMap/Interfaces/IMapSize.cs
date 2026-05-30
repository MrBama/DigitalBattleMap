using DigitalBattleMap.DataClasses;
using System;

namespace DigitalBattleMap.Interfaces;

public interface IMapSize
{
    event EventHandler OnGridSizeChanged;
    event EventHandler<CanvasSizeChangedEventArgs> OnCanvasSizeChanged;

    /// <summary>
    /// Fixed render width of the player viewport in pixels. Always 1920 (from Constants.MapSize).
    /// All map elements (fog, grid, tokens) are rendered at this resolution.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Fixed render height of the player viewport in pixels. Always 1080 (from Constants.MapSize).
    /// All map elements (fog, grid, tokens) are rendered at this resolution.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Current grid cell size in map pixels. Determines zoom level — a larger grid means
    /// zoomed in, smaller means zoomed out. Clamped between Constants.MinGridSize and Constants.MaxGridSize.
    /// </summary>
    public int GridSize { get; }

    /// <summary>
    /// Width of the DM canvas control in screen (WPF) pixels. Used to map between
    /// on-screen positions and map pixel positions via the Map() extension method.
    /// Changes when the window is resized.
    /// </summary>
    public double CanvasWidth { get; }

    /// <summary>
    /// Height of the DM canvas control in screen (WPF) pixels. Derived from CanvasWidth
    /// using a fixed 16:9 ratio — never set independently.
    /// </summary>
    public double CanvasHeight { get; }

    /// <summary>
    /// Size of a single grid cell in screen (WPF) pixels. Derived by mapping GridSize
    /// from map pixel space to canvas pixel space.
    /// </summary>
    public double CanvasGridSize { get; }

    /// <summary>
    /// Width of the full loaded background image in its native pixel dimensions.
    /// Null when no background is loaded.
    /// </summary>
    public int? BackgroundWidth { get; }

    /// <summary>
    /// Height of the full loaded background image in its native pixel dimensions.
    /// Null when no background is loaded.
    /// </summary>
    public int? BackgroundHeight { get; }

    /// <summary>
    /// Offset of the player viewport origin relative to the top-left of the full background image,
    /// in background image pixels. Matches the OffsetFromOrigin used by the background overview bitmap.
    /// Null when no background is loaded.
    /// </summary>
    public Point<int>? BackgroundOffset { get; }

    public Size<int> GetSize();
    public Size<double> GetCanvasSize();
}
