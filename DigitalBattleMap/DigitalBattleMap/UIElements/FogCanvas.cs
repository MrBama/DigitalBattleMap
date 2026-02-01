using DigitalBattleMap.DataClasses;
using DigitalBattleMap.FogShapes;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Polygon = System.Windows.Shapes.Polygon;

namespace DigitalBattleMap.UIElements;

public class FogCanvas : InkCanvas
{
    private static EventHandler<FogShapeCollectionChangedEventArgs> _shapeCollectionChangedHandler;
    private static PropertyChangedEventHandler? _shapePropertyChangedHandler;
    private static PropertyChangedEventHandler? _shapeCollectionPropertyChangedHandler;
    private static NotifyCollectionChangedEventHandler? _shapePointsChangedHandler;
    private Dictionary<FogShape, Stroke> _strokes = new();
    private Dictionary<FogShape, Polygon> _polygons = new();

    private Dictionary<FogShape, PathGeometry> _innerPolygons = new(); // only used when background fill is enabled
    private PathGeometry _outerGeometry;
    private Path _backgroundFillPath;

    public FogCanvas()
    {
        _shapeCollectionChangedHandler = OnFogShapeCollectionChanged;
        _shapePropertyChangedHandler = OnShapePropertyChanged;
        _shapePointsChangedHandler = OnShapePointsChanged;
        _shapeCollectionPropertyChangedHandler = OnShapeCollectionPropertyChanged;
        ClipToBounds = true;
    }

    public static readonly DependencyProperty ActiveShapeProperty = DependencyProperty.Register(nameof(ActiveShape), typeof(FogShape), typeof(FogCanvas), new PropertyMetadata(OnActiveShapeDependencyPropertyChanged));
    public static readonly DependencyProperty ShapeCollectionProperty = DependencyProperty.Register(nameof(ShapeCollection), typeof(FogShapeCollection), typeof(FogCanvas), new PropertyMetadata(OnShapesDependencyPropertyChanged));

    public FogShape ActiveShape
    {
        get => (FogShape)GetValue(ActiveShapeProperty);
        set => SetValue(ActiveShapeProperty, value);
    }

    public FogShapeCollection ShapeCollection
    {
        get => (FogShapeCollection)GetValue(ShapeCollectionProperty);
        set => SetValue(ShapeCollectionProperty, value);
    }

    private static void OnActiveShapeDependencyPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var canvas = (FogCanvas)dependencyObject;

        if (eventArgs.OldValue is FogShape oldShape)
        {
            oldShape.PropertyChanged -= _shapePropertyChangedHandler;
            oldShape.OnPointsChanged -= _shapePointsChangedHandler;
            canvas.EraseActiveShape(oldShape);
        }

        if (eventArgs.NewValue is FogShape newShape)
        {
            newShape.PropertyChanged += _shapePropertyChangedHandler;
            newShape.OnPointsChanged += _shapePointsChangedHandler;
        }
    }

    private static void OnShapesDependencyPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var canvas = (FogCanvas)dependencyObject;

        if (eventArgs.OldValue is FogShapeCollection oldShapeCollection)
        {
            oldShapeCollection.OnFogShapeCollectionChanged -= _shapeCollectionChangedHandler;
            oldShapeCollection.OnFogShapePropertyChanged -= _shapePropertyChangedHandler;
            oldShapeCollection.OnFogShapePointsChanged -= _shapePointsChangedHandler;
            oldShapeCollection.PropertyChanged -= _shapeCollectionPropertyChangedHandler;
            canvas.EraseAll();
        }

        if (eventArgs.NewValue is FogShapeCollection newShapeCollection)
        {
            newShapeCollection.OnFogShapeCollectionChanged += _shapeCollectionChangedHandler;
            newShapeCollection.OnFogShapePropertyChanged += _shapePropertyChangedHandler;
            newShapeCollection.OnFogShapePointsChanged += _shapePointsChangedHandler;
            newShapeCollection.PropertyChanged += _shapeCollectionPropertyChangedHandler;
            canvas.DrawShapes(newShapeCollection.GetFogShapes());
        }
    }

    private void OnFogShapeCollectionChanged(object? sender, FogShapeCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case CollectionChangedAction.Add:
                DrawFinishedShape(e.ChangedShape);
                break;
            case CollectionChangedAction.Insert:
                InsertShape(e.Index, e.ChangedShape);
                break;
            case CollectionChangedAction.Remove:
                EraseShape(e.ChangedShape);
                break;
            case CollectionChangedAction.Clear:
                EraseAll();
                break;
            default:
                break;
        }
    }

    private void OnShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var watchedProperties = new List<string> { nameof(FogShape.Points), nameof(FogShape.IsFogEnabled) };
        if (sender is not FogShape shape || !watchedProperties.Contains(e.PropertyName))
        {
            return;
        }

        if (_strokes.ContainsKey(shape))
        {
            var index = Strokes.IndexOf(_strokes[shape]);
            EraseShape(shape);
            InsertShape(index, shape);
        }
        else
        {
            DrawFinishedShape(shape);
        }
        shape.ApplyShape();
    }

    private void OnShapePointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is FogShape shape)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    DrawPoints(shape);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    ErasePoints(shape);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();
                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
                case NotifyCollectionChangedAction.Reset:
                    EraseShape(shape);
                    break;
                default:
                    break;
            }
        }
    }

    private void OnShapeCollectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (ShapeCollection.HasBackgroundShape)
        {
            FillBackground();
        }
        else
        {
            Children.Remove(_backgroundFillPath);
            _backgroundFillPath = null;
        }
    }

    private void FillBackground()
    {
        var rect = new System.Windows.Rect(RenderSize);

        PathGeometry outer = new PathGeometry();
        PathFigure outerFigure = new PathFigure { StartPoint = rect.TopLeft, IsClosed = true, IsFilled = true };
        outerFigure.Segments.Add(new PolyLineSegment(new[] { rect.TopRight, rect.BottomRight, rect.BottomLeft }, true));
        outer.Figures.Add(outerFigure);
        _outerGeometry = outer;

        Path myPath = new Path { Fill = Brushes.Black, Opacity=0.25, Data = outer };
        Children.Add(myPath);
        _backgroundFillPath = myPath;

        UpdateInnerPolygonsFill();
    }

    private void UpdateInnerPolygonFill(PathGeometry newInner)
    {
        if (_backgroundFillPath == null)
        {
            return;
        }

        _backgroundFillPath.Data = new CombinedGeometry(GeometryCombineMode.Exclude,
            _backgroundFillPath.Data,
            newInner);
    }

    private void UpdateInnerPolygonsFill()
    {
        if (_backgroundFillPath == null || !_innerPolygons.Any())
        {
            return;
        }

        _backgroundFillPath.Data = _outerGeometry; // start with fog filled canvas, then exclude the inner polygons
        foreach (var innerPolygon in _innerPolygons.Values) {
            _backgroundFillPath.Data = new CombinedGeometry(GeometryCombineMode.Exclude,
                _backgroundFillPath.Data,
                innerPolygon);
        }
    }

    private void EraseAll()
    {
        Strokes.Clear();
        _strokes.Clear();
        Children.Clear();
        _polygons.Clear();
    }

    private void EraseShape(FogShape shape)
    {
        if (_polygons.ContainsKey(shape))
        {
            Children.Remove(_polygons[shape]);
            _polygons.Remove(shape);

            Strokes.Remove(_strokes[shape]);
            _strokes.Remove(shape);

            _innerPolygons.Remove(shape);
            // Shapes are excluded from background fog by remove a shape
            // we do not know what to add back so we need to recreate it.
            UpdateInnerPolygonsFill();
        }
    }

    private void EraseActiveShape(FogShape shape)
    {
        if (!ShapeCollection.GetFogShapes().Contains(shape))
        {
            EraseShape(shape);
        }
    }

    private void ErasePoints(FogShape shape)
    {
        if (shape.Points.Count > 0)
        {
            if (_strokes.ContainsKey(shape))
            {
                _strokes[shape].StylusPoints = ConvertToStylusPointCollection(shape);
            }
        }
        else
        {
            EraseShape(shape);
        }
    }

    private void DrawShape(FogShape shape)
    {
        if (!_strokes.ContainsKey(shape) && shape.Points.Count > 0)
        {
            var stroke = CreateStroke(shape);
            Strokes.Add(stroke);
            _strokes[shape] = stroke;
        }
    }

    private void DrawFinishedShape(FogShape shape)
    {
        if (!_polygons.ContainsKey(shape) && shape.Points.Count > 0)
        {
            var polygon = CreatePolygon(shape);
            Children.Add(polygon);
            _polygons[shape] = polygon;

            DrawShape(shape);

            var inner = AddToInnerPolygons(shape);
            UpdateInnerPolygonFill(inner);
        }
    }

    private PathGeometry AddToInnerPolygons(FogShape shape)
    {
        var inner = CreatePathGeometry(shape);
        _innerPolygons.Add(shape, inner);
        return inner;
    }

    private PathGeometry CreatePathGeometry(FogShape shape)
    {
        var geometry = new PathGeometry();
        var figure = new PathFigure();
        var line = new PolyLineSegment();

        figure.IsClosed = true;
        figure.IsFilled = true;


        foreach(var point in shape.Points)
        {
            line.Points.Add(new System.Windows.Point(point.X, point.Y));
        }
        line.IsStroked = true;
        figure.Segments.Add(line);
        geometry.Figures.Add(figure);
        return geometry;
    }

    private void InsertShape(int index, FogShape shape)
    {
        if (!_polygons.ContainsKey(shape) && shape.Points.Count > 0)
        {
            var polygon = CreatePolygon(shape);
            Children.Insert(index, polygon);
            _polygons[shape] = polygon;

            var stroke = CreateStroke(shape);
            Strokes.Insert(index, stroke);
            _strokes[shape] = stroke;

            var inner = AddToInnerPolygons(shape);
            UpdateInnerPolygonFill(inner);
        }
    }

    private void DrawShapes(IEnumerable<FogShape> shapes)
    {
        foreach (var shape in shapes)
        {
            DrawShape(shape);
        }
    }

    private static Polygon CreatePolygon(FogShape shape)
    {
        var polygon = new Polygon();
        var pointCollection = new PointCollection();
        foreach (var point in shape.Points)
        {
            pointCollection.Add(new System.Windows.Point(point.X, point.Y));
        }

        polygon.Points = pointCollection;
        polygon.Fill = shape.IsFogEnabled ? Brushes.Black : Brushes.Transparent;
        polygon.Opacity = 0.5;
        return polygon;
    }

    private void DrawPoints(FogShape shape)
    {
        if (shape.Points.Count > 0)
        {
            if (_strokes.ContainsKey(shape))
            {
                _strokes[shape].StylusPoints = ConvertToStylusPointCollection(shape);
            }
            else
            {
                DrawShape(shape);
            }
        }
    }

    private Stroke CreateStroke(FogShape shape)
    {
        var stroke = new Stroke(ConvertToStylusPointCollection(shape));
        stroke.DrawingAttributes.Width = shape.PenSizeCanvas;
        stroke.DrawingAttributes.Height = shape.PenSizeCanvas;
        stroke.DrawingAttributes.Color = shape.Color;
        stroke.DrawingAttributes.IgnorePressure = true;

        return stroke;
    }

    private static StylusPointCollection ConvertToStylusPointCollection(FogShape shape)
    {
        var stylusPoints = new StylusPointCollection();
        foreach (var point in shape.Points)
        {
            stylusPoints.Add(new StylusPoint(point.X, point.Y));
        }

        return stylusPoints;
    }
}
