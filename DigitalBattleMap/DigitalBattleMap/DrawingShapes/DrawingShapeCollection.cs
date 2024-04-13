using DigitalBattleMap.DataClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;

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
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, drawingShape));
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
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, drawingShape));
        }
    }

    public void Remove(DrawingShape drawingShape)
    {
        var index = _drawingShapes.IndexOf(drawingShape);
        _drawingShapes.Remove(drawingShape);
        drawingShape.PropertyChanged -= DrawingShapePropertyChanged;
        drawingShape.OnPointsChanged -= DrawingShapePointsChanged;
        DrawingShapeCollectionChanged(new DrawingShapeCollectionChangedEventArgs { Action = CollectionChangedAction.Remove, ChangedShape = drawingShape });

        // Only show shapes that cannot be removed with the eraser
        if (!drawingShape.IsErasable)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, drawingShape, index));
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
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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
}
