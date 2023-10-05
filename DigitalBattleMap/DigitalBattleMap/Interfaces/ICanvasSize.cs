using DigitalBattleMap.DataClasses;

namespace DigitalBattleMap.Interfaces;

public delegate void CanvasSizeChangedEventHandler(object sender, CanvasSizeChangedEventArgs e);

public interface ICanvasSize
{
    event CanvasSizeChangedEventHandler OnCanvasSizeChanged;

    public double Width { get; }
    public double Height { get; }

    public Size<double> GetSize();
}
