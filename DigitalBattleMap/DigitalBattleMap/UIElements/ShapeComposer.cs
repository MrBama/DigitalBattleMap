using DigitalBattleMap.DataClasses;
using DigitalBattleMap.FogShapes;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DigitalBattleMap.UIElements;

public class ShapeComposer : PropertyHandler, INotifyPropertyChanged
{
    private Dictionary<FogShape, ShapeData> _shapesData = new();
    private FogShapeCollection _shapeCollection;
    private Size<double> _canvasSize;

    public Geometry ComposedGeometry
    {
        get => Get<Geometry>();
        private set => Set(value);
    }

    public Geometry StrokesGeometry
    {
        get => Get<Geometry>();
        private set => Set(value);
    }

    public List<Geometry> StrokesGeometries
    {
        get => Get<List<Geometry>>();
        private set => Set(value);
    }

    public Brush ComposedGeometryOuterStrokeBrush
    {
        get => Get<Brush>();
        private set => Set(value);
    }

    public double ComposedGeometryOuterStrokeThickness
    {
        get => Get<double>();
        private set => Set(value);
    }

    public Brush ComposedGeometryInnerStrokeBrush
    {
        get => Get<Brush>();
        private set => Set(value);
    }

    public double ComposedGeometryInnerStrokeThickness
    {
        get => Get<double>();
        private set => Set(value);
    }

    public void SetShapeCollection(FogShapeCollection shapeCollection)
    {
        if (_shapeCollection != null)
        {
            _shapeCollection.OnFogShapeCollectionChanged -= OnFogShapeCollectionChanged;
            _shapeCollection.OnFogShapePropertyChanged -= OnFogShapePropertyChanged;
            _shapeCollection.PropertyChanged -= OnShapeCollectionPropertyChanged;
        }

        _shapeCollection = shapeCollection;

        if (_shapeCollection != null)
        {
            _shapeCollection.OnFogShapeCollectionChanged += OnFogShapeCollectionChanged;
            _shapeCollection.OnFogShapePropertyChanged += OnFogShapePropertyChanged;
            _shapeCollection.PropertyChanged += OnShapeCollectionPropertyChanged;

            InitializeWithExistingShapes();
        }
    }

    public void SetCanvasSize(Size<double> canvasSize)
    {
        _canvasSize = canvasSize;

        if (_shapeCollection?.IsFillFogEnabled == true)
        {
            UpdateComposedGeometry();
        }
    }

    private void InitializeWithExistingShapes()
    {
        foreach (var shape in _shapeCollection.GetFogShapes())
        {
            var geometry = CreatePathGeometry(shape);
            var shapeData = new ShapeData(shape, geometry);
            _shapesData[shape] = shapeData;
        }

        UpdateComposedGeometry();
    }

    private void OnFogShapeCollectionChanged(object? sender, FogShapeCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case CollectionChangedAction.Add:
                var geometry = CreatePathGeometry(e.ChangedShape);
                var shapeData = new ShapeData(e.ChangedShape, geometry);
                var newPoints = new List<Point<double>>(e.ChangedShape.Points);

                foreach (var existingShapeEntry in _shapesData)
                {
                    var existingShape = existingShapeEntry.Key;
                    var existingShapeData = existingShapeEntry.Value;

                    if (existingShape != e.ChangedShape)
                    {
                        if (PolygonUnionHelper.PolygonsOverlap(existingShapeData.Points, newPoints))
                        {
                            newPoints = PolygonUnionHelper.SubtractPolygon(newPoints, existingShapeData.Points);

                            if (newPoints == null || newPoints.Count < 3)
                            {
                                break;
                            }
                        }
                    }
                }

                if (newPoints == null || newPoints.Count < 3)
                {
                    _shapesData[e.ChangedShape] = shapeData;
                    _shapeCollection?.Remove(e.ChangedShape);
                    return;
                }

                shapeData.Points = newPoints;
                shapeData.Geometry = CreatePathGeometryFromPoints(newPoints);
                _shapesData[e.ChangedShape] = shapeData;

                UpdateComposedGeometry();
                break;

            case CollectionChangedAction.Remove:
                _shapesData.Remove(e.ChangedShape);
                UpdateComposedGeometry();
                break;

            case CollectionChangedAction.Clear:
                _shapesData.Clear();
                UpdateComposedGeometry();
                break;
        }
    }

    private void OnFogShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FogShape.Points) ||
            e.PropertyName == nameof(FogShape.IsFogEnabled) ||
            e.PropertyName == nameof(FogShape.PenSize))
        {
            if (sender is FogShape shape && _shapesData.ContainsKey(shape))
            {
                var shapeData = _shapesData[shape];

                if (e.PropertyName == nameof(FogShape.Points))
                {
                    var newFogPoints = new List<Point<double>>(shape.Points);
                    if (shapeData.Points.Count >= 3)
                    {
                        var matrix = ComputeTransformMatrix(shapeData.OriginalFogPoints, newFogPoints);
                        var pts = shapeData.Points
                            .Select(p => new System.Windows.Point(p.X, p.Y))
                            .ToArray();
                        matrix.Transform(pts);
                        shapeData.Points = pts.Select(p => new Point<double>(p.X, p.Y)).ToList();
                    }
                    shapeData.OriginalFogPoints = newFogPoints;
                    shapeData.Geometry = CreatePathGeometryFromPoints(shapeData.Points);
                }

                shapeData.UpdateFromFogShape();
                UpdateComposedGeometry();
            }
        }
    }

    private void OnShapeCollectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FogShapeCollection.IsFillFogEnabled))
        {
            UpdateComposedGeometry();
        }
    }

    private void UpdateComposedGeometry()
    {
        if (_shapeCollection == null)
        {
            ComposedGeometry = null;
            StrokesGeometry = null;
            StrokesGeometries = null;
            ComposedGeometryOuterStrokeBrush = null;
            ComposedGeometryOuterStrokeThickness = 0;
            ComposedGeometryInnerStrokeBrush = null;
            ComposedGeometryInnerStrokeThickness = 0;
            return;
        }

        ComposedGeometry = _shapeCollection.IsFillFogEnabled
            ? RebuildBackgroundFillGeometry()
            : ClipToCanvas(RebuildComposedGeometry());

        RebuildStrokesGeometry();
        UpdateStrokeProperties();
    }

    private void UpdateStrokeProperties()
    {
        if (_shapesData.Count == 0)
        {
            ComposedGeometryOuterStrokeBrush = null;
            ComposedGeometryOuterStrokeThickness = 0;
            ComposedGeometryInnerStrokeBrush = null;
            ComposedGeometryInnerStrokeThickness = 0;
            return;
        }

        var firstShapeData = _shapesData.Values.First();

        ComposedGeometryOuterStrokeBrush = new SolidColorBrush(Colors.Black);
        ComposedGeometryOuterStrokeThickness = firstShapeData.PenSizeCanvas;

        ComposedGeometryInnerStrokeBrush = new SolidColorBrush(Colors.White);
        ComposedGeometryInnerStrokeThickness = firstShapeData.PenSizeCanvas / 2;
    }

    public PathGeometry CreatePathGeometry(FogShape shape)
    {
        var geometry = new PathGeometry();

        if (shape.Points.Count == 0)
        {
            return geometry;
        }

        var figure = new PathFigure();
        figure.StartPoint = new System.Windows.Point(shape.Points[0].X, shape.Points[0].Y);
        figure.IsClosed = true;
        figure.IsFilled = true;

        if (shape.Points.Count > 1)
        {
            var line = new PolyLineSegment();

            for (int i = 1; i < shape.Points.Count; i++)
            {
                line.Points.Add(new System.Windows.Point(shape.Points[i].X, shape.Points[i].Y));
            }

            line.IsStroked = true;
            figure.Segments.Add(line);
        }

        geometry.Figures.Add(figure);
        return geometry;
    }

    public void Clear()
    {
        _shapesData.Clear();
        ComposedGeometry = null;
        StrokesGeometry = null;
        StrokesGeometries = null;
        ComposedGeometryOuterStrokeBrush = null;
        ComposedGeometryOuterStrokeThickness = 0;
        ComposedGeometryInnerStrokeBrush = null;
        ComposedGeometryInnerStrokeThickness = 0;
    }

    public IReadOnlyList<(List<Point<double>> Points, bool IsFogEnabled, double PenSizeCanvas)> GetTrimmedFogData()
    {
        return _shapesData.Values
            .Select(sd => (sd.Points, sd.IsFogEnabled, sd.PenSizeCanvas))
            .ToList();
    }

    private Geometry RebuildComposedGeometry()
    {
        var enabledGeometries = _shapesData
            .Where(sd => sd.Key.IsFogEnabled)
            .Select(sd => sd.Value.Geometry)
            .ToList();

        if (enabledGeometries.Count == 0)
        {
            return null;
        }

        if (enabledGeometries.Count == 1)
        {
            return enabledGeometries[0];
        }

        Geometry result = enabledGeometries[0];
        for (int i = 1; i < enabledGeometries.Count; i++)
        {
            result = new CombinedGeometry(GeometryCombineMode.Union, result, enabledGeometries[i]);
        }

        return result;
    }

    private Geometry RebuildBackgroundFillGeometry()
    {
        if (_canvasSize.Width <= 0 || _canvasSize.Height <= 0)
        {
            return null;
        }

        var canvasRect = new System.Windows.Rect(0, 0, _canvasSize.Width, _canvasSize.Height);

        var outerFigure = new PathFigure
        {
            StartPoint = canvasRect.TopLeft,
            IsClosed = true,
            IsFilled = true
        };
        outerFigure.Segments.Add(new PolyLineSegment(new[] { canvasRect.TopRight, canvasRect.BottomRight, canvasRect.BottomLeft }, true));

        var outer = new PathGeometry();
        outer.Figures.Add(outerFigure);

        Geometry result = outer;
        foreach (var entry in _shapesData)
        {
            if (!entry.Key.IsFogEnabled)
            {
                result = new CombinedGeometry(GeometryCombineMode.Exclude, result, entry.Value.Geometry);
            }
        }

        return result;
    }

    private void RebuildStrokesGeometry()
    {
        var relevantShapeData = _shapesData.Values.ToList();

        if (relevantShapeData.Count == 0)
        {
            StrokesGeometry = null;
            StrokesGeometries = null;
            return;
        }

        var strokeGeometries = relevantShapeData
            .Select(sd => ClipToCanvas((Geometry)CreatePathGeometryFromPoints(sd.Points)))
            .Where(g => g != null)
            .ToList();

        StrokesGeometries = strokeGeometries.Count > 0 ? strokeGeometries : null;

        if (strokeGeometries.Count == 0)
        {
            StrokesGeometry = null;
        }
        else if (strokeGeometries.Count == 1)
        {
            StrokesGeometry = strokeGeometries[0];
        }
        else
        {
            Geometry result = strokeGeometries[0];
            for (int i = 1; i < strokeGeometries.Count; i++)
            {
                result = new CombinedGeometry(GeometryCombineMode.Union, result, strokeGeometries[i]);
            }
            StrokesGeometry = result;
        }
    }

    private static Matrix ComputeTransformMatrix(List<Point<double>> oldPoints, List<Point<double>> newPoints)
    {
        if (oldPoints.Count == 0 || oldPoints.Count != newPoints.Count)
            return Matrix.Identity;

        // Try to find a second point distinct from the first to recover scale.
        for (int i = 1; i < oldPoints.Count; i++)
        {
            double dxOld = oldPoints[i].X - oldPoints[0].X;
            double dyOld = oldPoints[i].Y - oldPoints[0].Y;
            double oldLen = Math.Sqrt(dxOld * dxOld + dyOld * dyOld);
            if (oldLen > 0.001)
            {
                double dxNew = newPoints[i].X - newPoints[0].X;
                double dyNew = newPoints[i].Y - newPoints[0].Y;
                double newLen = Math.Sqrt(dxNew * dxNew + dyNew * dyNew);
                double scale = newLen / oldLen;
                double tx = newPoints[0].X - scale * oldPoints[0].X;
                double ty = newPoints[0].Y - scale * oldPoints[0].Y;
                return new Matrix(scale, 0, 0, scale, tx, ty);
            }
        }

        // All points coincide — pure translation.
        return new Matrix(1, 0, 0, 1,
            newPoints[0].X - oldPoints[0].X,
            newPoints[0].Y - oldPoints[0].Y);
    }

    private Geometry ClipToCanvas(Geometry geometry)
    {
        if (geometry == null || _canvasSize.Width <= 0 || _canvasSize.Height <= 0)
            return geometry;
        var clip = new RectangleGeometry(new System.Windows.Rect(0, 0, _canvasSize.Width, _canvasSize.Height));
        return new CombinedGeometry(GeometryCombineMode.Intersect, geometry, clip);
    }

    private PathGeometry CreatePathGeometryFromPoints(List<Point<double>> points)
    {
        var geometry = new PathGeometry();

        if (points.Count < 3)
        {
            return geometry;
        }

        var figure = new PathFigure();
        figure.StartPoint = new System.Windows.Point(points[0].X, points[0].Y);
        figure.IsClosed = true;
        figure.IsFilled = true;

        if (points.Count > 1)
        {
            var line = new PolyLineSegment();

            for (int i = 1; i < points.Count; i++)
            {
                line.Points.Add(new System.Windows.Point(points[i].X, points[i].Y));
            }

            line.IsStroked = true;
            figure.Segments.Add(line);
        }

        geometry.Figures.Add(figure);
        return geometry;
    }
}
