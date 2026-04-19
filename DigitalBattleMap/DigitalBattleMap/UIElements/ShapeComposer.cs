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
    /// Gets the composed geometry containing all fog shapes and background fill.
    /// </summary>
    public Geometry ComposedGeometry
    {
        get => Get<Geometry>();
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
    /// </summary>
    private void OnFogShapeCollectionChanged(object? sender, FogShapeCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case CollectionChangedAction.Add:
                var geometry = CreatePathGeometry(e.ChangedShape);
                var shapeData = new ShapeData(e.ChangedShape, geometry);
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
    /// Updates the composed geometry after shape changes.
    /// </summary>
    private void UpdateComposedGeometry()
    {
        if (_shapeCollection == null || _shapesData.Count == 0)
        {
            ComposedGeometry = null;
            ComposedGeometryOuterStrokeBrush = null;
            ComposedGeometryOuterStrokeThickness = 0;
            ComposedGeometryInnerStrokeBrush = null;
            ComposedGeometryInnerStrokeThickness = 0;
            return;
        }

        ComposedGeometry = RebuildComposedGeometry();
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
        ComposedGeometryOuterStrokeBrush = null;
        ComposedGeometryOuterStrokeThickness = 0;
        ComposedGeometryInnerStrokeBrush = null;
        ComposedGeometryInnerStrokeThickness = 0;
    }

    /// <summary>
    /// Combines all inner polygons into a single composed geometry.
    /// Only includes shapes where IsFogEnabled is true.
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
        Geometry result = enabledGeometries[0];
        for (int i = 1; i < enabledGeometries.Count; i++)
        {
            result = new CombinedGeometry(GeometryCombineMode.Union, result, enabledGeometries[i]);
        }

        return result;
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
