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

namespace DigitalBattleMap.FogShapes;

public abstract class FogShape : PropertyHandler, ILinkableObject, IShape
{
    private Action _applyShapeCallback;
    private FogShapeInfo _editInfo;
    protected IMapSize _mapSize;
    private Point<double> _previousMovePosition;
    private bool _isMoving;

    public FogShape(Action applyShapeCallback, IMapSize mapSize)
    {
        _applyShapeCallback = applyShapeCallback;
        _mapSize = mapSize;

        Color = Colors.Black;
        PenSize = 5;
        Points = new();
        Name = "DrawingShape";
        Size = "0";
        LinkableObject = new LinkableObject(UpdatePosition);

        RegisterPropertyChangedWatcher(nameof(Cursor), new List<string> { nameof(Color), nameof(PenSize) });
    }

    public event NotifyCollectionChangedEventHandler OnPointsChanged;
    public event EventHandler OnRenderChanged;

    public Color Color { get => Get<Color>(); set => Set(value, () => NotifyPropertyChange(nameof(ColorBrush))); }
    public Brush ColorBrush { get => new SolidColorBrush(Color); }
    public double PenSize { get => Get<double>(); set => Set(Math.Clamp(value, 1, 100)); } // This is map size instead of canvas size because of UI reasons.
    public double PenSizeCanvas { get => PenSize.Map(0, _mapSize.Width, 0, _mapSize.CanvasWidth); }
    public string Size { get => Get<string>(); set => Set(value); }
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
    public LinkableObject LinkableObject { get; private set; } // todo is this needed for fog?

    public void ApplyShape()
    {
        IsEditing = false;
        _applyShapeCallback();
    }

    public void EditShape()
    {
        IsEditing = true;
        _editInfo = new FogShapeInfo(this);
    }

    public void CancelEditShape()
    {
        IsEditing = false;
        PenSize = _editInfo.Size;
        Color = _editInfo.Color;
        Points = new ObservableCollection<Point<double>>(_editInfo.Points);
    }

    public void LeftButtonDown(MouseButtonDataEventArgs e)
    {
        ButtonDown(e.Position);
    }

    public void LeftButtonUp(MouseButtonDataEventArgs e)
    {
        ButtonUp(e.Position);
    }

    public void RightButtonDown(MouseButtonDataEventArgs e)
    {
        if (IsEditing)
        {
            var position = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(e.Position, _mapSize, _mapSize.CanvasGridSize / 2) : e.Position;

            var margin = 0.001; // This is required for floating point comparison
            var minX = Points.Min(p => p.X) - margin;
            var minY = Points.Min(p => p.Y) - margin;
            var maxX = Points.Max(p => p.X) + margin;
            var maxY = Points.Max(p => p.Y) + margin;

            if (position.X >= minX && position.X <= maxX && position.Y >= minY && position.Y <= maxY)
            {
                _previousMovePosition = position;
                _isMoving = true;
            }
        }
    }

    public void RightButtonUp(MouseButtonDataEventArgs e)
    {
        if (IsEditing && _isMoving)
        {
            var position = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(e.Position, _mapSize, _mapSize.CanvasGridSize / 2) : e.Position;
            if (position != _previousMovePosition)
            {
                var distanceX = position.X - _previousMovePosition.X;
                var distanceY = position.Y - _previousMovePosition.Y;

                var matrix = new Matrix();
                matrix.Translate(distanceX, distanceY);
                Transform(matrix);
                RenderShape();
            }

            _isMoving = false;
        }
    }

    public void MouseMove(MouseMoveDataEventArgs e)
    {
        MouseMove(e.Position, e.LeftButtonDown);

        if (e.RightButtonDown && IsEditing && _isMoving)
        {
            var position = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(e.Position, _mapSize, _mapSize.CanvasGridSize / 2) : e.Position;
            if (position != _previousMovePosition)
            {
                var distanceX = position.X - _previousMovePosition.X;
                var distanceY = position.Y - _previousMovePosition.Y;

                var matrix = new Matrix();
                matrix.Translate(distanceX, distanceY);
                Transform(matrix);

                _previousMovePosition = position;
            }
        }
    }

    public void SetProperties(Action applyShapeCallback, IMapSize mapSize)
    {
        _applyShapeCallback = applyShapeCallback;
        _mapSize = mapSize;
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
            var distanceX = offsetDouble.X.Map(0, _mapSize.Width, 0, _mapSize.CanvasWidth);
            var distanceY = offsetDouble.Y.Map(0, _mapSize.Height, 0, _mapSize.CanvasHeight);
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

    private Point[] ToWindowsPointArray(IEnumerable<Point<double>> points)
    {
        return points.Select(p => new Point(p.X, p.Y)).ToArray();
    }

    private IEnumerable<Point<double>> ToPointDoubleEnumerable(Point[] points)
    {
        return points.Select(p => new Point<double>(p.X, p.Y));
    }

    private class FogShapeInfo
    {
        public FogShapeInfo(FogShape fogShape)
        {
            Color = fogShape.Color;
            Size = fogShape.PenSize;
            Points = fogShape.Points.ToList();
        }

        public Color Color { get; set; }
        public double Size { get; set; }
        public List<Point<double>> Points { get; set; }
    }
}