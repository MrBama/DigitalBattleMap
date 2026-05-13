using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.FogShapes;

public abstract class FogShape : PropertyHandler
{
    protected Action _applyShapeCallback;
    protected IMapSize _mapSize;

    public FogShape(Action applyShapeCallback, IMapSize mapSize)
    {
        _applyShapeCallback = applyShapeCallback;
        _mapSize = mapSize;
        IsDrawingFog = false;

        ColorOuter = Colors.Black;
        ColorInner = Colors.White;
        PenSize = 3;
        Points = new();
        Name = "Name";
        Size = "0";

        RegisterPropertyChangedWatcher(nameof(Cursor), new List<string> { nameof(Color), nameof(PenSize) });
    }

    public event NotifyCollectionChangedEventHandler OnPointsChanged;
    public event EventHandler<ControlInfoEventArgs> OnControlUpdated;
    public event EventHandler OnRenderChanged;

    public Color ColorOuter { get => Get<Color>(); set => Set(value); }
    public Color ColorInner { get => Get<Color>(); set => Set(value); }
    public double PenSize { get => Get<double>(); set => Set(Math.Clamp(value, 1, 100)); } // This is map size instead of canvas size because of UI reasons.
    public double PenSizeCanvas { get => PenSize.Map(0, _mapSize.Width, 0, _mapSize.CanvasWidth); }
    public string Size { get => Get<string>(); set => Set(value); }
    public bool IsDrawingFog { get => Get<bool>(); set => Set(value); }
    public bool SnapToGrid { get => Get<bool>(); set => Set(value); }
    public string Name { get => Get<string>(); set => Set(value); }
    public string ShapeType { get => Get<string>(); set => Set(value); }
    public virtual Cursor Cursor { get => CursorCreator.CreateFogCursor(32); }
    public bool IsFogEnabled { get => Get<bool>(); set => Set(value); }
    public bool IsDeleted { get => Get<bool>(); set => Set(value); }
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
    public BitmapSource DeleteIconBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.FogIcons.Delete.png")).ToBitmapImage(); }
    public virtual NType CurrentType { get => Get<NType>(); set => Set(value); }


    public void ApplyShape()
    {
        IsDrawingFog = false;
        _applyShapeCallback();
    }

    public void LeftButtonDown(MouseButtonDataEventArgs e)
    {
        ButtonDown(e.Position);
    }

    public void LeftButtonUp(MouseButtonDataEventArgs e)
    {
        ButtonUp(e.Position);
    }

    public void RightButtonUp(MouseButtonDataEventArgs e)
    {
        CancelButton();
    }

    public void MouseMove(MouseMoveDataEventArgs e)
    {
        MouseMove(e.Position, e.LeftButtonDown);
    }

    public void MouseWheel(MouseWheelDataEventArgs e)
    {
        MouseWheel(e.Position, e.Delta);
    }

    public void SetProperties(Action applyShapeCallback, IMapSize mapSize)
    {
        _applyShapeCallback = applyShapeCallback;
        _mapSize = mapSize;
    }

    public virtual void Transform(Matrix matrix)
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

    public abstract FogShape Clone();
    public abstract void UpdateControls();
    protected abstract void ButtonDown(Point<double> position);
    protected abstract void ButtonUp(Point<double> position);
    protected abstract void MouseMove(Point<double> position, bool buttonDown);
    protected virtual void CancelButton() { /* lets fog shapes choose to implement */ }
    protected virtual void MouseWheel(Point<double> position, int mouseDelta) { /* lets fog shapes choose to implement */ }

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

    public bool PositionInside(Point<double> position)
    {
        bool inside = false;
        for (int i = 0, j = Points.Count() - 1; i < Points.Count(); j = i++)
        {
            if (((Points[i].Y > position.Y) != (Points[j].Y > position.Y)) &&
                (position.X < (Points[j].X - Points[i].X) * (position.Y - Points[i].Y) / (Points[j].Y - Points[i].Y) + Points[i].X))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    public void NotifyControlUpdated(string controlName, List<InfoBlock> infoBlocks)
    {
        OnControlUpdated.Invoke(this, new ControlInfoEventArgs { controlName = controlName, infoBlocks = infoBlocks });
    }

    public void NotifyControlUpdated(object? sender, ControlInfoEventArgs e)
    {
        OnControlUpdated.Invoke(sender, e);
    }
}