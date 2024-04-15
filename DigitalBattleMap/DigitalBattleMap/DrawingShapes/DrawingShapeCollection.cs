using DigitalBattleMap.DataClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DigitalBattleMap.DrawingShapes;

public class DrawingShapeCollection : IEnumerable, INotifyCollectionChanged
{
    private List<DrawingShape> _drawingShapes = new();

    public event NotifyCollectionChangedEventHandler? OnDrawingShapePointsChanged;
    public event PropertyChangedEventHandler? OnDrawingShapePropertyChanged;
    public event EventHandler<DrawingShapeCollectionChangedEventArgs> OnDrawingShapeCollectionChanged;
    public event NotifyCollectionChangedEventHandler? CollectionChanged; // This is only used for UI

    public void Add(DrawingShape drawingShape)
    {
        _drawingShapes.Add(drawingShape);
        drawingShape.PropertyChanged += DrawingShapePropertyChanged;
        drawingShape.OnPointsChanged += DrawingShapePointsChanged;
        DrawingShapeCollectionChanged(new DrawingShapeCollectionChangedEventArgs { Action = CollectionChangedAction.Add, ChangedShape = drawingShape });

        // Only show shapes that cannot be removed with the eraser
        if (!drawingShape.IsErasable)
        {
            DrawingShapeUICollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, drawingShape));
        }
    }

    public void Insert(int index, DrawingShape drawingShape)
    {
        _drawingShapes.Insert(index, drawingShape);
        drawingShape.PropertyChanged += DrawingShapePropertyChanged;
        drawingShape.OnPointsChanged += DrawingShapePointsChanged;
        DrawingShapeCollectionChanged(new DrawingShapeCollectionChangedEventArgs { Action = CollectionChangedAction.Insert, ChangedShape = drawingShape, Index = index });

        // Only show shapes that cannot be removed with the eraser
        if (!drawingShape.IsErasable)
        {
            DrawingShapeUICollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, drawingShape));
        }
    }

    public void Remove(DrawingShape drawingShape)
    {
        var index = _drawingShapes.Where(s => !s.IsErasable).ToList().IndexOf(drawingShape);
        drawingShape.PropertyChanged -= DrawingShapePropertyChanged;
        drawingShape.OnPointsChanged -= DrawingShapePointsChanged;
        _drawingShapes.Remove(drawingShape);

        DrawingShapeCollectionChanged(new DrawingShapeCollectionChangedEventArgs { Action = CollectionChangedAction.Remove, ChangedShape = drawingShape });

        // Only show shapes that cannot be removed with the eraser
        if (!drawingShape.IsErasable)
        {
            DrawingShapeUICollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, drawingShape, index));
        }
    }

    public void Clear()
    {
        foreach (var drawingShape in _drawingShapes)
        {
            drawingShape.PropertyChanged -= DrawingShapePropertyChanged;
            drawingShape.OnPointsChanged -= DrawingShapePointsChanged;
        }
        _drawingShapes.Clear();
        DrawingShapeCollectionChanged(new DrawingShapeCollectionChangedEventArgs { Action = CollectionChangedAction.Clear });
        DrawingShapeUICollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool Contains(DrawingShape drawingShape)
    {
        return _drawingShapes.Contains(drawingShape);
    }

    public IEnumerable<DrawingShape> GetShapes()
    {
        return _drawingShapes;
    }

    public IEnumerator GetEnumerator()
    {
        return _drawingShapes.GetEnumerator();
    }

    public int IndexOf(DrawingShape drawingShape)
    {
        return _drawingShapes.IndexOf(drawingShape);
    }

    public void Transform(Matrix matrix)
    {
        foreach (var shape in _drawingShapes)
        {
            var points = ToWindowsPointArray(shape.Points);
            matrix.Transform(points);
            shape.Points = new ObservableCollection<Point<double>>(ToPointDoubleEnumerable(points));

            DrawingShapeCollectionChanged(new DrawingShapeCollectionChangedEventArgs { Action = CollectionChangedAction.Update, ChangedShape = shape });
        }
    }

    private void DrawingShapePointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnDrawingShapePointsChanged?.Invoke(sender, e);
    }

    private void DrawingShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnDrawingShapePropertyChanged?.Invoke(sender, e);
    }

    private void DrawingShapeCollectionChanged(DrawingShapeCollectionChangedEventArgs eventArgs)
    {
        OnDrawingShapeCollectionChanged?.Invoke(this, eventArgs);
    }

    private void DrawingShapeUICollectionChanged(NotifyCollectionChangedEventArgs eventArgs)
    {
        CollectionChanged?.Invoke(this, eventArgs);
    }

    private Point[] ToWindowsPointArray(IEnumerable<Point<double>> points)
    {
        return points.Select(p => new Point(p.X, p.Y)).ToArray();
    }

    private IEnumerable<Point<double>> ToPointDoubleEnumerable(Point[] points)
    {
        return points.Select(p => new Point<double>(p.X, p.Y));
    }
}
