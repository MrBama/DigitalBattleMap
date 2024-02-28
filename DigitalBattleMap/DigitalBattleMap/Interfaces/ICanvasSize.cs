using DigitalBattleMap.DataClasses;
using System;

namespace DigitalBattleMap.Interfaces;

public interface ICanvasSize
{
    event EventHandler<CanvasSizeChangedEventArgs> OnCanvasSizeChanged;

    public double Width { get; }
    public double Height { get; }

    public Size<double> GetSize();
}
