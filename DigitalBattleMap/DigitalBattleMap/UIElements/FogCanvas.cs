using DigitalBattleMap.FogShapes;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace DigitalBattleMap.UIElements;

/// <summary>
/// FogCanvas manages only the active (currently being drawn) fog shape.
/// Completed shapes are rendered via the ComposedGeometry Path element in MainWindow.
/// </summary>
public class FogCanvas : InkCanvas
{
    private PropertyChangedEventHandler? _activeShapePropertyChangedHandler;
    private NotifyCollectionChangedEventHandler? _activeShapePointsChangedHandler;

    public FogCanvas()
    {
        _activeShapePropertyChangedHandler = OnActiveShapePropertyChanged;
        _activeShapePointsChangedHandler = OnActiveShapePointsChanged;
        ClipToBounds = true;
    }

    public static readonly DependencyProperty ActiveShapeProperty = 
        DependencyProperty.Register(
            nameof(ActiveShape), 
            typeof(FogShape), 
            typeof(FogCanvas), 
            new PropertyMetadata(OnActiveShapeDependencyPropertyChanged));

    public FogShape ActiveShape
    {
        get => (FogShape)GetValue(ActiveShapeProperty);
        set => SetValue(ActiveShapeProperty, value);
    }

    private static void OnActiveShapeDependencyPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var canvas = (FogCanvas)dependencyObject;

        if (eventArgs.OldValue is FogShape oldShape)
        {
            oldShape.PropertyChanged -= canvas._activeShapePropertyChangedHandler;
            oldShape.OnPointsChanged -= canvas._activeShapePointsChangedHandler;
            canvas.ClearActiveShape();
        }

        if (eventArgs.NewValue is FogShape newShape)
        {
            newShape.PropertyChanged += canvas._activeShapePropertyChangedHandler;
            newShape.OnPointsChanged += canvas._activeShapePointsChangedHandler;
            canvas.DrawActiveShape(newShape);
        }
    }

    private void OnActiveShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not FogShape shape)
        {
            return;
        }

        // Redraw when IsFogEnabled changes
        if (e.PropertyName == nameof(FogShape.IsFogEnabled))
        {
            RefreshActiveShape(shape);
        }
    }

    private void OnActiveShapePointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not FogShape shape)
        {
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                UpdateActiveShapeStrokes(shape);
                break;
            case NotifyCollectionChangedAction.Remove:
                UpdateActiveShapeStrokes(shape);
                break;
            case NotifyCollectionChangedAction.Reset:
                ClearActiveShape();
                break;
        }
    }

    private void DrawActiveShape(FogShape shape)
    {
        if (shape.Points.Count > 0)
        {
            var strokeOuter = CreateStrokeOuter(shape);
            Strokes.Add(strokeOuter);

            var strokeInner = CreateStrokeInner(shape);
            Strokes.Add(strokeInner);
        }
    }

    private void UpdateActiveShapeStrokes(FogShape shape)
    {
        // If no strokes exist yet, draw them
        if (Strokes.Count < 2)
        {
            DrawActiveShape(shape);
            return;
        }

        var stylus = ConvertToStylusPointCollection(shape);
        Strokes[Strokes.Count - 2].StylusPoints = stylus;  // outer stroke
        Strokes[Strokes.Count - 1].StylusPoints = stylus;  // inner stroke
    }

    private void RefreshActiveShape(FogShape shape)
    {
        ClearActiveShape();
        DrawActiveShape(shape);
    }

    private void ClearActiveShape()
    {
        Strokes.Clear();
    }

    private static Stroke CreateStrokeOuter(FogShape shape)
    {
        var stroke = new Stroke(ConvertToStylusPointCollection(shape));
        stroke.DrawingAttributes.Width = shape.PenSizeCanvas;
        stroke.DrawingAttributes.Height = shape.PenSizeCanvas;
        stroke.DrawingAttributes.Color = shape.ColorOuter;
        stroke.DrawingAttributes.IgnorePressure = true;
        return stroke;
    }

    private static Stroke CreateStrokeInner(FogShape shape)
    {
        var stroke = new Stroke(ConvertToStylusPointCollection(shape));
        stroke.DrawingAttributes.Width = shape.PenSizeCanvas / 2;
        stroke.DrawingAttributes.Height = shape.PenSizeCanvas / 2;
        stroke.DrawingAttributes.Color = shape.ColorInner;
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
