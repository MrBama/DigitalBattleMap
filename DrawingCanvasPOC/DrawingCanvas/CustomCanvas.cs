using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DrawingCanvas;

public class CustomCanvas : Canvas
{
    private static PropertyChangedEventHandler? _shapePropertyChangedHandler;
    private static NotifyCollectionChangedEventHandler? _shapePointsChangedHandler;

    public CustomCanvas()
    {
        _shapePropertyChangedHandler = OnShapePropertyChanged;
        _shapePointsChangedHandler = OnShapePointsChanged;
        ClipToBounds = true;
    }

    public static readonly DependencyProperty ActiveShapeProperty = DependencyProperty.Register(nameof(ActiveShape), typeof(DrawingShape), typeof(CustomCanvas), new PropertyMetadata(OnActiveShapeDependencyPropertyChanged));
    public static readonly DependencyProperty ShapeCollectionProperty = DependencyProperty.Register(nameof(ShapeCollection), typeof(DrawingShapeCollection), typeof(CustomCanvas), new PropertyMetadata(OnShapesDependencyPropertyChanged));

    public DrawingShape ActiveShape
    {
        get => (DrawingShape)GetValue(ActiveShapeProperty);
        set => SetValue(ActiveShapeProperty, value);
    }

    public DrawingShapeCollection ShapeCollection
    {
        get => (DrawingShapeCollection)GetValue(ShapeCollectionProperty);
        set => SetValue(ShapeCollectionProperty, value);
    }

    private static void OnActiveShapeDependencyPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var canvas = (CustomCanvas)dependencyObject;

        if (eventArgs.OldValue is DrawingShape oldDrawingShape)
        {
            oldDrawingShape.PropertyChanged -= _shapePropertyChangedHandler;
            oldDrawingShape.OnPointsChanged -= _shapePointsChangedHandler;
            canvas.EraseActiveShape(oldDrawingShape);
        }

        if (eventArgs.NewValue is DrawingShape newDrawingShape)
        {
            newDrawingShape.PropertyChanged += _shapePropertyChangedHandler;
            newDrawingShape.OnPointsChanged += _shapePointsChangedHandler;
        }
    }

    private static void OnShapesDependencyPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var canvas = (CustomCanvas)dependencyObject;

        if (eventArgs.OldValue is DrawingShapeCollection oldShapeCollection)
        {
            oldShapeCollection.OnDrawingShapePropertyChanged -= _shapePropertyChangedHandler;
            oldShapeCollection.OnDrawingShapePointsChanged -= _shapePointsChangedHandler;
            canvas.EraseAll();
        }

        if (eventArgs.NewValue is DrawingShapeCollection newShapeCollection)
        {
            newShapeCollection.OnDrawingShapePropertyChanged += _shapePropertyChangedHandler;
            newShapeCollection.OnDrawingShapePointsChanged += _shapePointsChangedHandler;
            canvas.DrawShapes(newShapeCollection.GetDrawingShapes());
        }
    }

    private void OnShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is DrawingShape drawingShape)
        {
            EraseShape(drawingShape);
            DrawShape(drawingShape);
        }
    }

    private void OnShapePointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is DrawingShape shape)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    DrawPoints(e.NewItems!.OfType<Point>(), shape);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    ErasePoints(e.OldItems!.OfType<Point>(), shape);
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

    private void EraseAll()
    {
        Children.Clear();
    }

    private void EraseShape(DrawingShape shape)
    {
        var shapePoints = Children.OfType<PointEllipse>().Where(p => p.IsPartOfShape(shape)).ToList();

        foreach (var shapePoint in shapePoints)
        {
            Children.Remove(shapePoint);
        }
    }

    private void EraseActiveShape(DrawingShape shape)
    {
        if (!ShapeCollection.GetDrawingShapes().Contains(shape))
        {
            EraseShape(shape);
        }
    }

    private void ErasePoints(IEnumerable<Point> points, DrawingShape shape)
    {
        var shapePoints = Children.OfType<PointEllipse>().Where(p => p.IsPartOfShape(shape));

        foreach (var point in points)
        {
            var pointEllipses = shapePoints.Where(s => s.Point == point).ToList();
            foreach (var pointEllipse in pointEllipses)
            {
                Children.Remove(pointEllipse);
            }
        }
    }

    private void DrawShape(DrawingShape shape)
    {
        foreach (var point in shape.Points)
        {
            Children.Add(new PointEllipse(shape, point));
        }
    }

    private void DrawShapes(IEnumerable<DrawingShape> shapes)
    {
        foreach (var shape in shapes)
        {
            DrawShape(shape);
        }
    }

    private void DrawPoints(IEnumerable<Point> points, DrawingShape shape)
    {
        foreach (var point in points)
        {
            Children.Add(new PointEllipse(shape, point));
        }
    }
}
