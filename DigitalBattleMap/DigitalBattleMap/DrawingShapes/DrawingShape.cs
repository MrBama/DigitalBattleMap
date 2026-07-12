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
    private DrawingShapeInfo _editInfo = new();
    protected IMapSize _mapSize;
    private ITokenLinker _tokenLinker;
    private Point<double> _previousMovePosition;
    private Point<double> _centerOfRotation;
    private bool _isMoving;
    private DrawingShapeMode _lockedMode = DrawingShapeMode.Move;

    public DrawingShape(Action applyShapeCallback, ITokenLinker tokenLinker, IMapSize mapSize)
    {
        _applyShapeCallback = applyShapeCallback;
        _tokenLinker = tokenLinker;
        _mapSize = mapSize;

        Color = Colors.Black;
        PenSize = 5;
        Points = new();
        Name = "DrawingShape";
        SizeLabel = "";
        LinkableObject = new LinkableObject(UpdatePosition);

        LinkToTokenCommand = new RelayCommand(p => LinkToDifferentToken());
        ColorChangedCommand = new RelayCommand(p => ColorChanged((DrawingButton)p));
        PenSizeChangedCommand = new RelayCommand(p => PenSizeChanged(p));
        SnapToGridChangedCommand = new RelayCommand(p => SnapToGridChanged());

        RegisterPropertyChangedWatcher(nameof(Cursor), new List<string> { nameof(Color), nameof(PenSize) });
    }

    public event NotifyCollectionChangedEventHandler OnPointsChanged;

    // This event triggers a UI update for the players
    public event EventHandler<DrawingShapeEditedEventArgs> OnRenderChanged;

    public Color Color { get => Get<Color>(); set => Set(value, () => NotifyPropertyChange(nameof(ColorBrush))); }
    public Brush ColorBrush { get => new SolidColorBrush(Color); }
    public double PenSize { get => Get<double>(); set => Set(Math.Clamp(value, 1, 100), () => ContextMenuPenSize = PenSize); } // This is map size instead of canvas size because of UI reasons.
    public double ContextMenuPenSize { get => Get<double>(); set => Set(Math.Clamp(value, 1, 100)); }
    public double PenSizeCanvas { get => PenSize.Map(0, _mapSize.Width, 0, _mapSize.CanvasWidth); }
    public string SizeLabel { get => Get<string>(); set => Set(value); }
    public Thickness SizeLabelPosition { get => Get<Thickness>(); set => Set(value); }
    public bool SnapToGrid { get => Get<bool>(); set => Set(value); }
    public string Name { get => Get<string>(); protected set => Set(value); }
    public virtual Cursor Cursor { get => CursorCreator.Create(new SolidColorBrush(Color), (int)Math.Max(8, PenSize)); }
    public virtual bool ShowInShapesOverview => true;
    public virtual bool IsRotateShapeSupported => false;
    public bool IsErasable => !ShowInShapesOverview;
    public List<Point<double>> RotationMarkers { get; set; } = new();
    public List<Point<double>> CentersOfRotation { get; set; } = new();
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
    public DrawingShapeMode Mode { get; set; } = DrawingShapeMode.Move;
    [JsonIgnore]
    public LinkableObject LinkableObject { get; private set; }
    [JsonIgnore]
    public ICommand LinkToTokenCommand { get; set; }
    [JsonIgnore]
    public ICommand ColorChangedCommand { get; set; }
    [JsonIgnore]
    public ICommand SnapToGridChangedCommand { get; set; }
    [JsonIgnore]
    public ICommand PenSizeChangedCommand { get; set; }

    public void ApplyShape()
    {
        _applyShapeCallback();
    }

    public void LeftButtonDown(MouseButtonDataEventArgs e)
    {
        SizeLabelPosition = new Thickness(e.Position.X, e.Position.Y, 0, 0);
        ButtonDown(e.Position);
    }

    public void LeftButtonUp(MouseButtonDataEventArgs e)
    {
        ButtonUp(e.Position);
    }

    public void RightButtonDown(MouseButtonDataEventArgs e)
    {
        if(Mode == DrawingShapeMode.Rotate && !IsRotateShapeSupported)
        {
            return;
        }

        _lockedMode = Mode;
        _editInfo = new DrawingShapeInfo(this);

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

        _centerOfRotation = GetCenterOfRotation(e.Position);
    }

    public void RightButtonUp(MouseButtonDataEventArgs e)
    {
        if (_isMoving)
        {
            if (_lockedMode == DrawingShapeMode.Move)
            {
                MoveShape(e.Position);
            }
            RenderShape(_editInfo);
            _isMoving = false;
        }
    }

    public void MouseMove(MouseMoveDataEventArgs e)
    {
        if (!e.RightButtonDown)
        {
            MouseMove(e.Position, e.LeftButtonDown);
        }
        else if (e.RightButtonDown && _isMoving)
        {
            switch (_lockedMode)
            {
                case DrawingShapeMode.Move:
                    MoveShape(e.Position);
                    break;
                case DrawingShapeMode.Rotate:
                    RotateShape(e.Position);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public void SetProperties(Action applyShapeCallback, ITokenLinker tokenLinker, IMapSize mapSize)
    {
        _applyShapeCallback = applyShapeCallback;
        _tokenLinker = tokenLinker;
        _mapSize = mapSize;
    }

    public void Transform(Matrix matrix)
    {
        var points = ToWindowsPointArray(Points);
        matrix.Transform(points);
        Points = new(ToPointDoubleEnumerable(points));

        var rotationMarkers = ToWindowsPointArray(RotationMarkers);
        matrix.Transform(rotationMarkers);
        RotationMarkers = new(ToPointDoubleEnumerable(rotationMarkers));

        var centersOfRotation = ToWindowsPointArray(CentersOfRotation);
        matrix.Transform(centersOfRotation);
        CentersOfRotation = new(ToPointDoubleEnumerable(centersOfRotation));
    }

    public void UpdatePosition(Point<int> offset)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var oldInfo = new DrawingShapeInfo(this);
            var offsetDouble = Point<double>.Create(offset);
            var distanceX = offsetDouble.X.Map(0, _mapSize.Width, 0, _mapSize.CanvasWidth);
            var distanceY = offsetDouble.Y.Map(0, _mapSize.Height, 0, _mapSize.CanvasHeight);
            var matrix = new Matrix();
            matrix.Translate(distanceX, distanceY);

            Transform(matrix);
            RenderShape(oldInfo);
        });
    }

    protected abstract void ButtonDown(Point<double> position);
    protected abstract void ButtonUp(Point<double> position);
    protected abstract void MouseMove(Point<double> position, bool buttonDown);

    protected void RenderShape(DrawingShapeInfo oldInfo)
    {
        var eventArgs = new DrawingShapeEditedEventArgs
        {
            DrawingShape = this,
            OldInfo = oldInfo,
            NewInfo = new DrawingShapeInfo(this)
        };
        OnRenderChanged?.Invoke(this, eventArgs);
    }

    protected virtual Point<double> GetCenterOfRotation(Point<double> position)
    {
        if (CentersOfRotation.Count > 0)
        {
            return CentersOfRotation.First();
        }
        else
        {
            return new Point<double>();
        }
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

    private void ColorChanged(DrawingButton newDrawingButton)
    {
        var oldInfo = new DrawingShapeInfo(this);
        Color = newDrawingButton.ToColor();
        RenderShape(oldInfo);
    }

    private void PenSizeChanged(object penSize)
    {
        var oldInfo = new DrawingShapeInfo(this);
        PenSize = ContextMenuPenSize;
        RenderShape(oldInfo);
    }

    private void SnapToGridChanged()
    {
        SnapToGrid = !SnapToGrid;
    }

    private void MoveShape(Point<double> position)
    {
        var snappedPosition = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2) : position;
        if (snappedPosition != _previousMovePosition)
        {
            var distanceX = snappedPosition.X - _previousMovePosition.X;
            var distanceY = snappedPosition.Y - _previousMovePosition.Y;

            var matrix = new Matrix();
            matrix.Translate(distanceX, distanceY);
            Transform(matrix);

            _previousMovePosition = snappedPosition;
        }
    }

    private void RotateShape(Point<double> position)
    {
        var snappedPosition = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2) : position;
        var newAngle = CalculateAngle(_centerOfRotation, snappedPosition);
        var oldAngle = CalculateAngle(_centerOfRotation, _previousMovePosition);
        var angle = newAngle - oldAngle;

        var points = Points.ToList();
        Points.Clear();

        // Rotate shape
        foreach (var point in points)
        {
            Points.Add(point.Rotate(_centerOfRotation, angle));
        }

        // Rotate rotation markers
        var rotationMarkers = new List<Point<double>>();
        foreach (var rotationMarker in RotationMarkers)
        {
            rotationMarkers.Add(rotationMarker.Rotate(_centerOfRotation, angle));
        }
        RotationMarkers = new(rotationMarkers);

        // Rotate centers of rotation
        var centersOfRotation = new List<Point<double>>();
        foreach (var centerOfRotation in CentersOfRotation)
        {
            centersOfRotation.Add(centerOfRotation.Rotate(_centerOfRotation, angle));
        }
        CentersOfRotation = new(centersOfRotation);

        _previousMovePosition = snappedPosition;
    }

    private double CalculateAngle(Point<double> startPosition, Point<double> endPosition)
    {
        var distanceX = endPosition.X - startPosition.X;
        var distanceY = endPosition.Y - startPosition.Y;

        var angle = Math.Atan2(distanceY, distanceX);
        var angleInDegrees = angle / Math.PI * 180;

        return angleInDegrees;
    }
}