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

/// <summary>
/// Manages the composition and rendering of completed fog shapes.
/// Listens to FogShapeCollection events and provides ComposedGeometry for rendering.
/// </summary>
public class ShapeComposer : PropertyHandler, INotifyPropertyChanged
{
    private Dictionary<FogShape, ShapeData> _shapesData = new();
    private PathGeometry _outerGeometry;
    private FogShapeCollection _shapeCollection;

    /// <summary>
    /// Gets the composed geometry containing all fog shapes.
    /// </summary>
    public Geometry ComposedGeometry
    {
        get => Get<Geometry>();
        private set => Set(value);
    }

    /// <summary>
    /// Gets the strokes geometry containing outlines of all enabled fog shapes.
    /// Built from individual shape data points to show all shape boundaries.
    /// </summary>
    public Geometry StrokesGeometry
    {
        get => Get<Geometry>();
        private set => Set(value);
    }

    /// <summary>
    /// Gets individual stroke geometries for each enabled fog shape.
    /// Each geometry is rendered separately to preserve internal boundaries.
    /// </summary>
    public List<Geometry> StrokesGeometries
    {
        get => Get<List<Geometry>>();
        private set => Set(value);
    }

    /// <summary>
    /// Gets the outer stroke brush (from fog shapes' ColorOuter).
    /// </summary>
    public Brush ComposedGeometryOuterStrokeBrush
    {
        get => Get<Brush>();
        private set => Set(value);
    }

    /// <summary>
    /// Gets the outer stroke thickness (from fog shapes' PenSize).
    /// </summary>
    public double ComposedGeometryOuterStrokeThickness
    {
        get => Get<double>();
        private set => Set(value);
    }

    /// <summary>
    /// Gets the inner stroke brush (from fog shapes' ColorInner).
    /// </summary>
    public Brush ComposedGeometryInnerStrokeBrush
    {
        get => Get<Brush>();
        private set => Set(value);
    }

    /// <summary>
    /// Gets the inner stroke thickness (half of fog shapes' PenSize).
    /// </summary>
    public double ComposedGeometryInnerStrokeThickness
    {
        get => Get<double>();
        private set => Set(value);
    }

    /// <summary>
    /// Sets the shape collection and subscribes to its events.
    /// </summary>
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

            // Initialize with existing shapes
            InitializeWithExistingShapes();
        }
    }

    /// <summary>
    /// Initializes the composer with existing shapes in the collection.
    /// </summary>
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

    /// <summary>
    /// Handles when shapes are added/removed/cleared from the collection.
    /// When adding: Only modifies the new shape's points to avoid overlapping existing shapes.
    /// Existing shapes are never modified - they retain their original points.
    /// </summary>
    private void OnFogShapeCollectionChanged(object? sender, FogShapeCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case CollectionChangedAction.Add:
                var geometry = CreatePathGeometry(e.ChangedShape);
                var shapeData = new ShapeData(e.ChangedShape, geometry);
                var newPoints = new List<Point<double>>(e.ChangedShape.Points);

                // Subtract all existing enabled shapes from the new shape's points
                foreach (var existingShapeEntry in _shapesData)
                {
                    var existingShape = existingShapeEntry.Key;
                    var existingShapeData = existingShapeEntry.Value;

                    // Only process enabled shapes that aren't the new one
                    if (existingShape.IsFogEnabled && existingShape != e.ChangedShape)
                    {
                        if (PolygonUnionHelper.PolygonsOverlap(existingShapeData.Points, newPoints))
                        {
                            // Subtract existing shape from new shape's points
                            newPoints = PolygonUnionHelper.SubtractPolygon(newPoints, existingShapeData.Points);
                            
                            // If new shape is completely covered, stop processing
                            if (newPoints == null || newPoints.Count < 3)
                            {
                                break;
                            }
                        }
                    }
                }

                // If new shape is completely covered by existing shapes, remove it
                if (newPoints == null || newPoints.Count < 3)
                {
                    _shapesData[e.ChangedShape] = shapeData;
                    _shapeCollection?.Remove(e.ChangedShape);
                    return;
                }

                // Update the new shape's data with the modified points
                shapeData.Points = newPoints;
                shapeData.Geometry = CreatePathGeometryFromPoints(newPoints);

                // Add the new shape
                _shapesData[e.ChangedShape] = shapeData;

                UpdateComposedGeometry();
                break;

            case CollectionChangedAction.Remove:
                _shapesData.Remove(e.ChangedShape);
                UpdateComposedGeometry();
                break;

            case CollectionChangedAction.Clear:
                _shapesData.Clear();
                _outerGeometry = null;
                ComposedGeometry = null;
                break;
        }
    }

    /// <summary>
    /// Handles when properties of shapes change (e.g., IsFogEnabled, Points, ColorOuter, ColorInner, PenSize).
    /// </summary>
    private void OnFogShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FogShape.Points) || 
            e.PropertyName == nameof(FogShape.IsFogEnabled) ||
            e.PropertyName == nameof(FogShape.ColorOuter) ||
            e.PropertyName == nameof(FogShape.ColorInner) ||
            e.PropertyName == nameof(FogShape.PenSize))
        {
            if (sender is FogShape shape && _shapesData.ContainsKey(shape))
            {
                var shapeData = _shapesData[shape];
                
                if (e.PropertyName == nameof(FogShape.Points))
                {
                    var updatedGeometry = CreatePathGeometry(shape);
                    shapeData.Geometry = updatedGeometry;
                    shapeData.Points = new List<Point<double>>(shape.Points);
                }
                
                shapeData.UpdateFromFogShape();
                UpdateComposedGeometry();
            }
        }
    }

    /// <summary>
    /// Handles when the fill fog setting changes.
    /// </summary>
    private void OnShapeCollectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FogShapeCollection.IsFillFogEnabled))
        {
            UpdateComposedGeometry();
        }
    }

    /// <summary>
    /// Updates the composed geometry and strokes geometry after shape changes.
    /// </summary>
    private void UpdateComposedGeometry()
    {
        if (_shapeCollection == null || _shapesData.Count == 0)
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

        ComposedGeometry = RebuildComposedGeometry();
        RebuildStrokesGeometry();
        UpdateStrokeProperties();
    }

    /// <summary>
    /// Updates stroke brush and thickness from the first shape's properties.
    /// For multiple shapes, uses the first shape's style.
    /// </summary>
    private void UpdateStrokeProperties()
    {
        if (_shapesData.Count == 0)
        {
            return;
        }

        var firstShapeData = _shapesData.Values.First();
        
        ComposedGeometryOuterStrokeBrush = new SolidColorBrush(firstShapeData.ColorOuter);
        ComposedGeometryOuterStrokeThickness = firstShapeData.PenSizeCanvas;
        
        ComposedGeometryInnerStrokeBrush = new SolidColorBrush(firstShapeData.ColorInner);
        ComposedGeometryInnerStrokeThickness = firstShapeData.PenSizeCanvas / 2;
    }

    /// <summary>
    /// Creates a PathGeometry representation of the fog shape.
    /// Fixes the bug where a line was drawn to the origin by properly setting StartPoint.
    /// </summary>
    public PathGeometry CreatePathGeometry(FogShape shape)
    {
        var geometry = new PathGeometry();
        
        // Handle empty shapes
        if (shape.Points.Count == 0)
        {
            return geometry;
        }

        var figure = new PathFigure();
        
        // Set the start point to the first point to avoid lines to origin
        figure.StartPoint = new System.Windows.Point(shape.Points[0].X, shape.Points[0].Y);
        figure.IsClosed = true;
        figure.IsFilled = true;

        // If there's more than one point, add them as a segment
        if (shape.Points.Count > 1)
        {
            var line = new PolyLineSegment();
            
            // Add all points except the first one (already set as StartPoint)
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

    /// <summary>
    /// Clears all registered shapes.
    /// </summary>
    public void Clear()
    {
        _shapesData.Clear();
        _outerGeometry = null;
        ComposedGeometry = null;
        StrokesGeometry = null;
        StrokesGeometries = null;
        ComposedGeometryOuterStrokeBrush = null;
        ComposedGeometryOuterStrokeThickness = 0;
        ComposedGeometryInnerStrokeBrush = null;
        ComposedGeometryInnerStrokeThickness = 0;
    }

    /// <summary>
    /// Combines all inner polygons into a single composed geometry.
    /// Only includes shapes where IsFogEnabled is true.
    /// Overlaps are already resolved at shape addition time, so this just combines the non-overlapping shapes.
    /// </summary>
    private Geometry RebuildComposedGeometry()
    {
        if (_shapesData.Count == 0)
        {
            return null;
        }

        // Filter only enabled fog shapes
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

        // Combine multiple geometries using Union
        // (These should already be non-overlapping due to subtraction at add time)
        Geometry result = enabledGeometries[0];
        for (int i = 1; i < enabledGeometries.Count; i++)
        {
            result = new CombinedGeometry(GeometryCombineMode.Union, result, enabledGeometries[i]);
        }

        return result;
    }

    /// <summary>
    /// Builds individual stroke geometries from all enabled shapes' points.
    /// Each geometry is stored separately to preserve internal boundaries.
    /// </summary>
    private void RebuildStrokesGeometry()
    {
        var enabledShapeData = _shapesData
            .Where(sd => sd.Key.IsFogEnabled)
            .Select(sd => sd.Value)
            .ToList();

        if (enabledShapeData.Count == 0)
        {
            StrokesGeometry = null;
            StrokesGeometries = null;
            return;
        }

        // Build individual geometries from each shape's points
        var strokeGeometries = enabledShapeData
            .Select(sd => (Geometry)CreatePathGeometryFromPoints(sd.Points))
            .Where(g => g != null)
            .ToList();

        // Store the list of individual geometries for XAML rendering
        StrokesGeometries = strokeGeometries.Count > 0 ? strokeGeometries : null;

        // Also keep a combined version for backward compatibility if needed
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
            // Combine all for the single geometry property
            Geometry result = strokeGeometries[0];
            for (int i = 1; i < strokeGeometries.Count; i++)
            {
                result = new CombinedGeometry(GeometryCombineMode.Union, result, strokeGeometries[i]);
            }
            StrokesGeometry = result;
        }
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

    /// <summary>
    /// Creates the background fill PathGeometry for fog.
    /// Starts with outer geometry and excludes all inner polygons.
    /// </summary>
    private Geometry CreateBackgroundFillGeometry(System.Windows.Rect canvasRect)
    {
        PathGeometry outer = new PathGeometry();
        PathFigure outerFigure = new PathFigure { StartPoint = canvasRect.TopLeft, IsClosed = true, IsFilled = true };
        outerFigure.Segments.Add(new PolyLineSegment(new[] { canvasRect.TopRight, canvasRect.BottomRight, canvasRect.BottomLeft }, true));
        outer.Figures.Add(outerFigure);
        _outerGeometry = outer;

        Geometry result = (Geometry)_outerGeometry.CloneCurrentValue();
        foreach (var shapeData in _shapesData.Values)
        {
            result = new CombinedGeometry(GeometryCombineMode.Exclude, result, shapeData.Geometry);
        }

        return result;
    }

    /// <summary>
    /// Rebuilds the background fill geometry from scratch excluding all inner polygons.
    /// </summary>
    private Geometry RebuildBackgroundFillGeometry()
    {
        if (!(_shapeCollection?.IsFillFogEnabled ?? false))
        {
            return null;
        }

        if (_shapesData.Count == 0)
        {
            return null;
        }

        // Create a dummy rectangle to get the outer geometry
        // In practice, this should be the canvas bounds
        var dummyRect = new System.Windows.Rect(0, 0, 1000, 1000);
        return CreateBackgroundFillGeometry(dummyRect);
    }
}
