using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Threading;

namespace DigitalBattleMap.DataClasses;

public class DrawingShape : ILinkableObject, IDisposable
{
    private ITokenLink _tokenLink;

    public DrawingShapeType DrawingShapeType { get; set; }
    public int Size { get; set; }
    public Stroke Stroke { get; set; }
    public Brush Color { get => GetColor(); }
    public DrawingButton DrawingButton { get; set; }
    public Size<double> CanvasSize { get; set; } = new();

    public event EventHandler OnUnlink;
    public event EventHandler OnPositionChanged;

    public void UpdatePosition(Point<int> offset)
    {
        var offsetDouble = Point<double>.Create(offset);
        var matrix = new Matrix();
        var distanceX = offsetDouble.X.Map(0, Constants.BitmapSize.Width, 0, CanvasSize.Width);
        var distanceY = offsetDouble.Y.Map(0, Constants.BitmapSize.Height, 0, CanvasSize.Height);

        matrix.Translate(distanceX, distanceY);
        
        Application.Current.Dispatcher.Invoke(() => { Stroke.Transform(matrix, false); }, DispatcherPriority.Normal);
        OnPositionChanged?.Invoke(this, new EventArgs());
    }

    public void Link(ITokenLink tokenLink)
    {
        _tokenLink?.Unlink(this);
        _tokenLink = tokenLink;
    }

    public void Unlink()
    {
        _tokenLink?.Unlink(this);
        _tokenLink = null;
        OnUnlink?.Invoke(this, new EventArgs());
    }

    public bool IsLinked()
    {
        return _tokenLink != null;
    }

    public TokenIndentifier GetLinkIdentifier()
    {
        return _tokenLink.GetTokenIndentifier();
    }

    public void DisposeLink()
    {
        _tokenLink = null;
        OnUnlink?.Invoke(this, new EventArgs());
    }

    public void Dispose()
    {
        Unlink();
    }

    private Brush GetColor()
    {
        var brush = System.Windows.Media.Brushes.Transparent;
        if (Stroke != null)
        {
            brush = new SolidColorBrush(Stroke.DrawingAttributes.Color);
        }

        return brush;
    }
}

public class DrawingShapeSave
{
    public DrawingShapeSave()
    {
    }

    public DrawingShapeSave(DrawingShape drawingShape, int strokeIndex)
    {
        DrawingShapeType = drawingShape.DrawingShapeType;
        Size = drawingShape.Size;
        DrawingButton = drawingShape.DrawingButton;
        StrokeIndex = strokeIndex;
    }

    public DrawingShapeType DrawingShapeType { get; set; }
    public int Size { get; set; }
    public DrawingButton DrawingButton { get; set; }
    public int StrokeIndex { get; set; }
}
