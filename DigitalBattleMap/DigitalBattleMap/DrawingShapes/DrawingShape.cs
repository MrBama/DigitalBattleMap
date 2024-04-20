using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DigitalBattleMap.DrawingShapes;

public abstract class DrawingShape : PropertyHandler, ILinkableObject
{
    private Action _applyShapeCallback;
    MouseButtonState _previousMouseButtonState;
    private DrawingShapeInfo _editInfo;
    protected ICanvasSize _canvasSize;
    private ITokenLinker _tokenLinker;
    protected int _gridSize;

    public DrawingShape(Action applyShapeCallback, ITokenLinker tokenLinker, ICanvasSize canvasSize, int gridSize)
    {
        _applyShapeCallback = applyShapeCallback;
        _tokenLinker = tokenLinker;
        _canvasSize = canvasSize;
        _gridSize = gridSize;

        Color = Colors.Black;
        PenSize = 5;
        Points = new();
        Name = "DrawingShape";
        LinkableObject = new LinkableObject(UpdatePosition);

        LeftButtonDownCommand = new RelayCommand(p => LeftButtonDown((MouseDataEventArgs)p));
        LeftButtonUpCommand = new RelayCommand(p => LeftButtonUp((MouseDataEventArgs)p));
        MouseMoveCommand = new RelayCommand(p => MouseMove((MouseDataEventArgs)p));
        LinkToTokenCommand = new RelayCommand(p => LinkToDifferentToken());

        RegisterPropertyChangedWatcher(nameof(Cursor), new List<string> { nameof(Color), nameof(PenSize) });
    }

    public event NotifyCollectionChangedEventHandler OnPointsChanged;
    public event EventHandler OnRenderChanged;

    public Color Color { get => Get<Color>(); set => Set(value, () => NotifyPropertyChange(nameof(ColorBrush))); }
    public Brush ColorBrush { get => new SolidColorBrush(Color); }
    public double PenSize { get => Get<double>(); set => Set(Math.Clamp(value, 1, 100)); } // This is map size instead of canvas size because of UI reasons.
    public double PenSizeCanvas { get => PenSize.Map(0, Constants.BitmapSize.Width, 0, _canvasSize.Width); }
    public bool IsEditing { get => Get<bool>(); set => Set(value); }
    public bool SnapToGrid { get => Get<bool>(); set => Set(value); }
    public string Name { get => Get<string>(); protected set => Set(value); }
    public virtual Cursor Cursor { get => CursorCreator.Create(new SolidColorBrush(Color), (int)Math.Max(8, PenSize)); }
    public virtual bool IsErasable => false;
    public Type Type { get => GetType(); }
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

    [JsonIgnore]
    public LinkableObject LinkableObject { get; private set; }
    [JsonIgnore]
    public ICommand LeftButtonDownCommand { get; set; }
    [JsonIgnore]
    public ICommand LeftButtonUpCommand { get; set; }
    [JsonIgnore]
    public ICommand MouseMoveCommand { get; set; }
    [JsonIgnore]
    public ICommand LinkToTokenCommand { get; set; }

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
        PenSize = _editInfo.Size;
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

    public void UpdateGridSize(int gridSize)
    {
        _gridSize = gridSize;
    }

    public void SetProperties(Action applyShapeCallback, ITokenLinker tokenLinker, ICanvasSize canvasSize, int gridSize)
    {
        _applyShapeCallback = applyShapeCallback;
        _tokenLinker = tokenLinker;
        _canvasSize = canvasSize;
        _gridSize = gridSize;
    }

    public void Transform(Matrix matrix)
    {
        var points = ToWindowsPointArray(Points);
        matrix.Transform(points);
        Points = new ObservableCollection<Point<double>>(ToPointDoubleEnumerable(points));
    }

    public void UpdatePosition(Point<int> offset)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var offsetDouble = Point<double>.Create(offset);
            var distanceX = offsetDouble.X.Map(0, Constants.BitmapSize.Width, 0, _canvasSize.Width);
            var distanceY = offsetDouble.Y.Map(0, Constants.BitmapSize.Height, 0, _canvasSize.Height);
            var matrix = new Matrix();
            matrix.Translate(distanceX, distanceY);

            Transform(matrix);
            RenderShape();
        });
    }

    protected abstract void ButtonDown(Point<double> position);
    protected abstract void ButtonUp(Point<double> position);
    protected abstract void MouseMove(Point<double> position, bool buttonDown);

    protected void RenderShape()
    {
        OnRenderChanged?.Invoke(this, new EventArgs());
    }

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPointsChanged?.Invoke(this, e);
    }

    private void LinkToDifferentToken()
    {
        if (!LinkableObject.IsLinked())
        {
            _tokenLinker.LinkToToken(this);
        }
        else
        {
            LinkableObject.Unlink();
        }
    }

    private Point[] ToWindowsPointArray(IEnumerable<Point<double>> points)
    {
        return points.Select(p => new Point(p.X, p.Y)).ToArray();
    }

    private IEnumerable<Point<double>> ToPointDoubleEnumerable(Point[] points)
    {
        return points.Select(p => new Point<double>(p.X, p.Y));
    }

    private class DrawingShapeInfo
    {
        public DrawingShapeInfo(DrawingShape drawingShape)
        {
            Color = drawingShape.Color;
            Size = drawingShape.PenSize;
            Points = drawingShape.Points.ToList();
        }

        public Color Color { get; set; }
        public double Size { get; set; }
        public List<Point<double>> Points { get; set; }
    }
}