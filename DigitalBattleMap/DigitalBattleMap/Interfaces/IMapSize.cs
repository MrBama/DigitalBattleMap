using DigitalBattleMap.DataClasses;
using System;

namespace DigitalBattleMap.Interfaces;

public interface IMapSize
{
    event EventHandler OnGridSizeChanged;
    event EventHandler<CanvasSizeChangedEventArgs> OnCanvasSizeChanged;

    public int Width { get; }
    public int Height { get; }
    public int GridSize { get; }
    public double CanvasWidth { get; }
    public double CanvasHeight { get; }
    public double CanvasGridSize { get; }

    public Size<int> GetSize();
    public Size<double> GetCanvasSize();
}
