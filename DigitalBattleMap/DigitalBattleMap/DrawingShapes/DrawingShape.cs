using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace DigitalBattleMap.DrawingShapes;

//public class DrawingShape : PropertyHandler, ILinkableObject, IDisposable
//{
//    private ITokenLink _tokenLink;
//    private ITokenLinker _tokenLinker;

//    public DrawingShape()
//    {
//        LinkToTokenButtonText = "Link to token";
//        LinkToTokenCommand = new RelayCommand(p => LinkToDifferentToken());
//    }

//    public DrawingShape(DrawingShape drawingShape)
//    {
//        DrawingShapeType = drawingShape.DrawingShapeType;
//        Size = new Point<int>(drawingShape.Size);
//        Stroke = drawingShape.Stroke.Clone();
//        DrawingButton = drawingShape.DrawingButton;
//        CanvasSize = drawingShape.CanvasSize;
//    }

//    public DrawingShapeType DrawingShapeType { get => Get<DrawingShapeType>(); set => Set(value); }
//    public Point<int> Size { get => Get<Point<int>>(); set => Set(value, UpdateSizeString); }
//    public string SizeString { get => Get<string>(); set => Set(value); }
//    public Stroke Stroke { get => Get<Stroke>(); set => Set(value, () => NotifyPropertyChange(nameof(Color))); }
//    public Brush Color { get => GetColor(); }
//    public DrawingButton DrawingButton { get; set; }
//    public ICanvasSize CanvasSize { get; set; }
//    public string LinkToTokenButtonText { get => Get<string>(); set => Set(value); }
//    public ICommand LinkToTokenCommand { get; set; }

//    public event EventHandler OnPositionChanged;

//    public void SetTokenLinker(ITokenLinker tokenLinker)
//    {
//        _tokenLinker = tokenLinker;
//    }

//    public void UpdatePosition(Point<int> offset)
//    {
//        var offsetDouble = Point<double>.Create(offset);
//        var matrix = new Matrix();
//        var distanceX = offsetDouble.X.Map(0, Constants.BitmapSize.Width, 0, CanvasSize.Width);
//        var distanceY = offsetDouble.Y.Map(0, Constants.BitmapSize.Height, 0, CanvasSize.Height);

//        matrix.Translate(distanceX, distanceY);

//        Application.Current.Dispatcher.Invoke(() => { Stroke.Transform(matrix, false); }, DispatcherPriority.Normal);
//        OnPositionChanged?.Invoke(this, new EventArgs());
//    }

//    public void Link(ITokenLink tokenLink)
//    {
//        _tokenLink?.Unlink(this);
//        _tokenLink = tokenLink;
//        RefershLinkToTokenButtonText();
//    }

//    public void Unlink()
//    {
//        _tokenLink?.Unlink(this);
//        _tokenLink = null;
//        RefershLinkToTokenButtonText();
//    }

//    public bool IsLinked()
//    {
//        return _tokenLink != null;
//    }

//    public TokenIndentifier GetLinkIdentifier()
//    {
//        return _tokenLink.GetTokenIndentifier();
//    }

//    public void DisposeLink()
//    {
//        _tokenLink = null;
//        RefershLinkToTokenButtonText();
//    }

//    public void Dispose()
//    {
//        Unlink();
//    }

//    private Brush GetColor()
//    {
//        var brush = System.Windows.Media.Brushes.Transparent;
//        if (Stroke != null)
//        {
//            brush = new SolidColorBrush(Stroke.DrawingAttributes.Color);
//        }

//        return brush;
//    }

//    private void RefershLinkToTokenButtonText()
//    {
//        if (IsLinked())
//        {
//            var linkIdentifier = GetLinkIdentifier();
//            LinkToTokenButtonText = $"Unlink from {linkIdentifier.Name} {linkIdentifier.Id}";
//        }
//        else
//        {
//            LinkToTokenButtonText = "Link to token";
//        }
//    }

//    private void LinkToDifferentToken()
//    {
//        if (!IsLinked())
//        {
//            _tokenLinker.LinkToToken(this);
//        }
//        else
//        {
//            Unlink();
//        }
//    }

//    private void UpdateSizeString()
//    {
//        if(DrawingShapeType == DrawingShapeType.Rectangle)
//        {
//            SizeString = $"{Size.X} x {Size.Y} ft";
//        }
//        else
//        {
//            SizeString = $"{Size.X} ft";
//        }
//    }
//}

//public class DrawingShapeSave
//{
//    public DrawingShapeSave()
//    {
//    }

//    public DrawingShapeSave(DrawingShape drawingShape, int strokeIndex)
//    {
//        DrawingShapeType = drawingShape.DrawingShapeType;
//        Size = drawingShape.Size;
//        DrawingButton = drawingShape.DrawingButton;
//        StrokeIndex = strokeIndex;
//    }

//    public DrawingShapeType DrawingShapeType { get; set; }
//    public Point<int> Size { get; set; }
//    public DrawingButton DrawingButton { get; set; }
//    public int StrokeIndex { get; set; }
//}

public abstract class DrawingShape : PropertyHandler
{
    private Action _applyShapeCallback;
    MouseButtonState _previousMouseButtonState;
    private DrawingShapeInfo _editInfo;
    protected ICanvasSize _canvasSize;
    protected int _gridSize;

    public DrawingShape(Action applyShapeCallback, ICanvasSize canvasSize, int gridSize)
    {
        _applyShapeCallback = applyShapeCallback;
        _canvasSize = canvasSize;
        _gridSize = gridSize;

        Color = Colors.Black;
        Size = 5;
        Points = new();
        Name = "DrawingShape";

        LeftButtonDownCommand = new RelayCommand(p => LeftButtonDown((MouseDataEventArgs)p));
        LeftButtonUpCommand = new RelayCommand(p => LeftButtonUp((MouseDataEventArgs)p));
        MouseMoveCommand = new RelayCommand(p => MouseMove((MouseDataEventArgs)p));

        RegisterPropertyChangedWatcher(nameof(Cursor), new List<string> { nameof(Color), nameof(Size) });
    }

    public event NotifyCollectionChangedEventHandler OnPointsChanged;

    public Color Color { get => Get<Color>(); set => Set(value, () => NotifyPropertyChange(nameof(ColorBrush))); }
    public Brush ColorBrush { get => new SolidColorBrush(Color); }
    public double Size { get => Get<double>(); set =>  Set(Math.Clamp(value, 1, 100)); }
    public bool IsEditing { get => Get<bool>(); set => Set(value); }
    public bool SnapToGrid { get => Get<bool>(); set => Set(value); }
    public string Name { get => Get<string>(); protected set => Set(value); }
    public virtual Cursor Cursor { get => CursorCreator.Create(new SolidColorBrush(Color), (int)Math.Max(8, Size)); }
    public virtual bool IsErasable => false;
    public ObservableCollection<Point<double>> Points
    {
        get => Get<ObservableCollection<Point<double>>>();
        set
        {
            var oldValue = Get<ObservableCollection<Point<double>>>();
            if (oldValue != null)
            {
                oldValue.CollectionChanged -= OnPointsCollectionChanged;
            }

            Set(value);

            if (value != null)
            {
                value.CollectionChanged += OnPointsCollectionChanged;
            }
        }
    }

    public ICommand LeftButtonDownCommand { get; set; }
    public ICommand LeftButtonUpCommand { get; set; }
    public ICommand MouseMoveCommand { get; set; }

    public void ApplyShape()
    {
        IsEditing = false;
        _applyShapeCallback();
    }

    public void EditShape()
    {
        IsEditing = true;
        _editInfo = new DrawingShapeInfo(this);
    }

    public void CancelEditShape()
    {
        IsEditing = false;
        Size = _editInfo.Size;
        Color = _editInfo.Color;
        Points = new ObservableCollection<Point<double>>(_editInfo.Points);
    }

    public void LeftButtonDown(MouseDataEventArgs mouseDataEventArgs)
    {
        if (_previousMouseButtonState == MouseButtonState.Released)
        {
            _previousMouseButtonState = MouseButtonState.Pressed;
            ButtonDown(mouseDataEventArgs.Position);
        }
    }

    public void LeftButtonUp(MouseDataEventArgs mouseDataEventArgs)
    {
        if (_previousMouseButtonState == MouseButtonState.Pressed)
        {
            _previousMouseButtonState = MouseButtonState.Released;
            ButtonUp(mouseDataEventArgs.Position);
        }
    }

    public void MouseMove(MouseDataEventArgs mouseDataEventArgs)
    {
        var directlyOver = mouseDataEventArgs.MouseEventArgs.MouseDevice.DirectlyOver;
        if (directlyOver is System.Windows.Controls.Canvas)
        {
            var mouseButtonState = mouseDataEventArgs.MouseEventArgs.LeftButton;
            if (_previousMouseButtonState != mouseButtonState)
            {
                _previousMouseButtonState = mouseButtonState;

                if (mouseButtonState == MouseButtonState.Pressed)
                {
                    ButtonDown(mouseDataEventArgs.Position);
                }
                else
                {
                    ButtonUp(mouseDataEventArgs.Position);
                }
            }
            else
            {
                MouseMove(mouseDataEventArgs.Position, mouseButtonState == MouseButtonState.Pressed);
            }
        }
    }

    public void UpdateGridSize(int gridSize)
    {
        _gridSize = gridSize;
    }

    protected abstract void ButtonDown(Point<double> position);
    protected abstract void ButtonUp(Point<double> position);
    protected abstract void MouseMove(Point<double> position, bool buttonDown);

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPointsChanged?.Invoke(this, e);
    }

    private class DrawingShapeInfo
    {
        public DrawingShapeInfo(DrawingShape drawingShape)
        {
            Color = drawingShape.Color;
            Size = drawingShape.Size;
            Points = drawingShape.Points.ToList();
        }

        public Color Color { get; set; }
        public double Size { get; set; }
        public List<Point<double>> Points { get; set; }
    }
}