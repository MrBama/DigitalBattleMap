using DigitalBattleMap.DataClasses;
using DigitalBattleMap.FogShapes;
using DigitalBattleMap.FogShapes.Region;
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
    private static EventHandler<FogShapeCollectionChangedEventArgs> _fogShapeCollectionChangedHandler;
    private static PropertyChangedEventHandler? _fogShapePropertyChangedHandler;
    private static PropertyChangedEventHandler? _fogShapeCollectionPropertyChangedHandler;
    private static NotifyCollectionChangedEventHandler? _fogShapePointsChangedHandler;
    private static EventHandler<FogShapeResolvedEventArgs>? _fogShapeResolvedHandler;

    // Changed from FogShape keys to ResolvedFogRegion keys
    private Dictionary<ResolvedFogRegion, Stroke> _strokesOuter = new();
    private Dictionary<ResolvedFogRegion, Stroke> _strokesInner = new();
    private Dictionary<ResolvedFogRegion, Polygon> _polygons = new();
    private Dictionary<ResolvedFogRegion, PathGeometry> _innerPolygons = new();

    // Map original FogShape to ResolvedFogRegion for tracking
    private Dictionary<FogShape, ResolvedFogRegion> _fogShapeToResolvedRegion = new();

    private PathGeometry _outerGeometry;
    private Path _backgroundFillPath;
    
    private FogShapeResolver _fogShapeResolver;

    public FogCanvas()
    {
        _fogShapeCollectionChangedHandler = OnFogShapeCollectionChanged;
        _fogShapePropertyChangedHandler = OnShapePropertyChanged;
        _fogShapePointsChangedHandler = OnShapePointsChanged;
        _fogShapeCollectionPropertyChangedHandler = OnShapeCollectionPropertyChanged;
        _fogShapeResolvedHandler = OnFogShapeResolved;
        _fogShapeResolver = new FogShapeResolver();
        _fogShapeResolver.OnFogShapeResolved += _fogShapeResolvedHandler;
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
            oldShape.PropertyChanged -= _fogShapePropertyChangedHandler;
            oldShape.OnPointsChanged -= _fogShapePointsChangedHandler;
            canvas.EraseActiveShape(oldShape);
        }

        if (eventArgs.NewValue is FogShape newShape)
        {
            newShape.PropertyChanged += _fogShapePropertyChangedHandler;
            newShape.OnPointsChanged += _fogShapePointsChangedHandler;
        }
    }

    private static void OnShapesDependencyPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var canvas = (FogCanvas)dependencyObject;

        if (eventArgs.OldValue is FogShapeCollection oldShapeCollection)
        {
            oldShapeCollection.OnFogShapeCollectionChanged -= _fogShapeCollectionChangedHandler;
            oldShapeCollection.OnFogShapePropertyChanged -= _fogShapePropertyChangedHandler;
            oldShapeCollection.OnFogShapePointsChanged -= _fogShapePointsChangedHandler;
            oldShapeCollection.PropertyChanged -= _fogShapeCollectionPropertyChangedHandler;
            canvas.EraseAll();
        }

        if (eventArgs.NewValue is FogShapeCollection newShapeCollection)
        {
            newShapeCollection.OnFogShapeCollectionChanged += _fogShapeCollectionChangedHandler;
            newShapeCollection.OnFogShapePropertyChanged += _fogShapePropertyChangedHandler;
            newShapeCollection.OnFogShapePointsChanged += _fogShapePointsChangedHandler;
            newShapeCollection.PropertyChanged += _fogShapeCollectionPropertyChangedHandler;
            canvas.DrawShapes(newShapeCollection.GetFogShapes());
        }
    }

    private void OnFogShapeCollectionChanged(object? sender, FogShapeCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case CollectionChangedAction.Add:
                // Route through resolver to create ResolvedFogRegion
                _fogShapeResolver.ResolveShape(e.ChangedShape, ((FogShapeCollection)sender).GetFogShapes().ToList());
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

    /// <summary>
    /// Handles the resolved fog shape from FogShapeResolver.
    /// This is called after the shape has been resolved and is ready for rendering.
    /// </summary>
    private void OnFogShapeResolved(object? sender, FogShapeResolvedEventArgs e)
    {
        _fogShapeToResolvedRegion[e.OriginalShape] = e.ResolvedRegion;
        DrawFinishedShape(e.ResolvedRegion);
    }

    private void OnShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var watchedProperties = new List<string> { nameof(FogShape.Points), nameof(FogShape.IsFogEnabled) };
        if (sender is not FogShape shape || !watchedProperties.Contains(e.PropertyName))
        {
            return;
        }

        // Find the corresponding resolved region
        if (_fogShapeToResolvedRegion.TryGetValue(shape, out var resolvedRegion))
        {
            if (_strokesOuter.ContainsKey(resolvedRegion))
            {
                var indexPolygon = Children.IndexOf(_polygons[resolvedRegion]);
                var indexOuter = Strokes.IndexOf(_strokesOuter[resolvedRegion]);
                var indexInner = Strokes.IndexOf(_strokesInner[resolvedRegion]);
                EraseShape(shape);
                // Re-resolve after property change
                _fogShapeResolver.ResolveShape(shape, ShapeCollection.GetFogShapes().ToList());
            }
            else
            {
                _fogShapeResolver.ResolveShape(shape, ShapeCollection.GetFogShapes().ToList());
            }
        }
        shape.ApplyShape();
    }

    private void OnShapePointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is FogShape shape && _fogShapeToResolvedRegion.TryGetValue(shape, out var resolvedRegion))
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    DrawPoints(resolvedRegion);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    ErasePoints(resolvedRegion);
                    break;
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
        if (ShapeCollection.IsFillFogEnabled)
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
        if (_backgroundFillPath == null)
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
        _strokesOuter.Clear();
        _strokesInner.Clear();
        Children.Clear();
        _polygons.Clear();
        _innerPolygons.Clear();
        _fogShapeToResolvedRegion.Clear();
        _backgroundFillPath = null;
    }

    private void EraseShape(FogShape fogShape)
    {
        if (_fogShapeToResolvedRegion.TryGetValue(fogShape, out var resolvedRegion))
        {
            EraseShape(resolvedRegion);
            _fogShapeToResolvedRegion.Remove(fogShape);
        }
    }

    private void EraseShape(ResolvedFogRegion resolvedRegion)
    {
        if (_polygons.ContainsKey(resolvedRegion))
        {
            Children.Remove(_polygons[resolvedRegion]);
            _polygons.Remove(resolvedRegion);

            Strokes.Remove(_strokesOuter[resolvedRegion]);
            _strokesOuter.Remove(resolvedRegion);
            Strokes.Remove(_strokesInner[resolvedRegion]);
            _strokesInner.Remove(resolvedRegion);

            _innerPolygons.Remove(resolvedRegion);
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

    private void ErasePoints(ResolvedFogRegion resolvedRegion)
    {
        if (resolvedRegion.Points.Count > 0)
        {
            if (_strokesOuter.ContainsKey(resolvedRegion))
            {
                var stylus = ConvertToStylusPointCollection(resolvedRegion);
                _strokesOuter[resolvedRegion].StylusPoints = stylus;
                _strokesInner[resolvedRegion].StylusPoints = stylus;
            }
        }
        else
        {
            EraseShape(resolvedRegion);
        }
    }

    private void DrawShape(ResolvedFogRegion resolvedRegion)
    {
        if (!_strokesOuter.ContainsKey(resolvedRegion) && resolvedRegion.Points.Count > 0)
        {
            var strokeOuter = CreateStrokeOuter(resolvedRegion);
            Strokes.Add(strokeOuter);
            _strokesOuter[resolvedRegion] = strokeOuter;

            var strokeInner = CreateStrokeInner(resolvedRegion);
            Strokes.Add(strokeInner);
            _strokesInner[resolvedRegion] = strokeInner;
        }
    }

    private void DrawFinishedShape(ResolvedFogRegion resolvedRegion)
    {
        if (!_polygons.ContainsKey(resolvedRegion) && resolvedRegion.Points.Count > 0)
        {
            var polygon = CreatePolygon(resolvedRegion);
            Children.Add(polygon);
            _polygons[resolvedRegion] = polygon;

            DrawShape(resolvedRegion);

            var inner = AddToInnerPolygons(resolvedRegion);
            UpdateInnerPolygonFill(inner);
        }
    }

    private PathGeometry AddToInnerPolygons(ResolvedFogRegion resolvedRegion)
    {
        var inner = CreatePathGeometry(resolvedRegion);
        _innerPolygons.Add(resolvedRegion, inner);
        return inner;
    }

    private PathGeometry CreatePathGeometry(ResolvedFogRegion resolvedRegion)
    {
        var geometry = new PathGeometry();
        var figure = new PathFigure();
        var line = new PolyLineSegment();

        figure.IsClosed = true;
        figure.IsFilled = true;

        foreach(var point in resolvedRegion.Points)
        {
            line.Points.Add(new System.Windows.Point(point.X, point.Y));
        }
        line.IsStroked = true;
        figure.Segments.Add(line);
        geometry.Figures.Add(figure);
        return geometry;
    }

    private void DrawShapes(IEnumerable<FogShape> shapes)
    {
        foreach (var shape in shapes)
        {
            // Route through resolver to create ResolvedFogRegion
            _fogShapeResolver.ResolveShape(shape, ShapeCollection.GetFogShapes().ToList());
        }
    }

    private static Polygon CreatePolygon(ResolvedFogRegion resolvedRegion)
    {
        var polygon = new Polygon();
        var pointCollection = new PointCollection();
        foreach (var point in resolvedRegion.Points)
        {
            pointCollection.Add(new System.Windows.Point(point.X, point.Y));
        }

        polygon.Points = pointCollection;
        polygon.Fill = resolvedRegion.IsFogEnabled ? Brushes.Black : Brushes.Transparent;
        polygon.Opacity = 0.5;
        return polygon;
    }

    private void DrawPoints(ResolvedFogRegion resolvedRegion)
    {
        if (resolvedRegion.Points.Count > 0)
        {
            if (_strokesOuter.ContainsKey(resolvedRegion))
            {
                var stylus = ConvertToStylusPointCollection(resolvedRegion);
                _strokesOuter[resolvedRegion].StylusPoints = stylus;
                _strokesInner[resolvedRegion].StylusPoints = stylus;
            }
            else
            {
                DrawShape(resolvedRegion);
            }
        }
    }

    private Stroke CreateStrokeOuter(ResolvedFogRegion resolvedRegion)
    {
        var stroke = new Stroke(ConvertToStylusPointCollection(resolvedRegion));
        stroke.DrawingAttributes.Width = resolvedRegion.PenSizeCanvas;
        stroke.DrawingAttributes.Height = resolvedRegion.PenSizeCanvas;
        stroke.DrawingAttributes.Color = resolvedRegion.ColorOuter;
        stroke.DrawingAttributes.IgnorePressure = true;

        return stroke;
    }

    private Stroke CreateStrokeInner(ResolvedFogRegion resolvedRegion)
    {
        var stroke = new Stroke(ConvertToStylusPointCollection(resolvedRegion));
        stroke.DrawingAttributes.Width = resolvedRegion.PenSizeCanvas / 2;
        stroke.DrawingAttributes.Height = resolvedRegion.PenSizeCanvas / 2;
        stroke.DrawingAttributes.Color = resolvedRegion.ColorInner;
        stroke.DrawingAttributes.IgnorePressure = true;

        return stroke;
    }

    private static StylusPointCollection ConvertToStylusPointCollection(ResolvedFogRegion resolvedRegion)
    {
        var stylusPoints = new StylusPointCollection();
        foreach (var point in resolvedRegion.Points)
        {
            stylusPoints.Add(new StylusPoint(point.X, point.Y));
        }

        return stylusPoints;
    }
}
