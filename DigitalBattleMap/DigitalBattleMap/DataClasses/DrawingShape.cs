using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DigitalBattleMap.DataClasses;

public class DrawingShape : PropertyHandler, ILinkableObject, IDisposable
{
    private ITokenLink _tokenLink;
    private ITokenLinker _tokenLinker;

    public DrawingShape()
    {
        LinkToTokenButtonText = "Link to token";
        LinkToTokenCommand = new RelayCommand(p => LinkToDifferentToken());
    }

    public DrawingShape(DrawingShape drawingShape)
    {
        DrawingShapeType = drawingShape.DrawingShapeType;
        Size = new Point<int>(drawingShape.Size);
        Stroke = drawingShape.Stroke.Clone();
        DrawingButton = drawingShape.DrawingButton;
        CanvasSize = drawingShape.CanvasSize;
    }

    public DrawingShapeType DrawingShapeType { get => Get<DrawingShapeType>(); set => Set(value); }
    public Point<int> Size { get => Get<Point<int>>(); set => Set(value, UpdateSizeString); }
    public string SizeString { get => Get<string>(); set => Set(value); }
    public Stroke Stroke { get => Get<Stroke>(); set => Set(value, () => NotifyPropertyChange(nameof(Color))); }
    public Brush Color { get => GetColor(); }
    public DrawingButton DrawingButton { get; set; }
    public Size<double> CanvasSize { get; set; } = new();
    public string LinkToTokenButtonText { get => Get<string>(); set => Set(value); }
    public ICommand LinkToTokenCommand { get; set; }

    public event EventHandler OnPositionChanged;

    public void SetTokenLinker(ITokenLinker tokenLinker)
    {
        _tokenLinker = tokenLinker;
    }

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
        RefershLinkToTokenButtonText();
    }

    public void Unlink()
    {
        _tokenLink?.Unlink(this);
        _tokenLink = null;
        RefershLinkToTokenButtonText();
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
        RefershLinkToTokenButtonText();
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

    private void RefershLinkToTokenButtonText()
    {
        if (IsLinked())
        {
            var linkIdentifier = GetLinkIdentifier();
            LinkToTokenButtonText = $"Unlink from {linkIdentifier.Name} {linkIdentifier.Id}";
        }
        else
        {
            LinkToTokenButtonText = "Link to token";
        }
    }

    private void LinkToDifferentToken()
    {
        if (!IsLinked())
        {
            _tokenLinker.LinkToToken(this);
        }
        else
        {
            Unlink();
        }
    }

    private void UpdateSizeString()
    {
        if(DrawingShapeType == DrawingShapeType.Rectangle)
        {
            SizeString = $"{Size.X} x {Size.Y} ft";
        }
        else
        {
            SizeString = $"{Size.X} ft";
        }
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
    public Point<int> Size { get; set; }
    public DrawingButton DrawingButton { get; set; }
    public int StrokeIndex { get; set; }
}
