using DigitalBattleMap.DataClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace DigitalBattleMap.DrawingShapes;

public class DrawingShapeCollection : IEnumerable, INotifyCollectionChanged
{
    private List<DrawingShape> _drawingShapes = new();

    public event NotifyCollectionChangedEventHandler? OnDrawingShapePointsChanged;
    public event PropertyChangedEventHandler? OnDrawingShapePropertyChanged;
    public event EventHandler<DrawingShapeCollectionChangedEventArgs> OnDrawingShapeCollectionChanged;
    public event EventHandler OnRenderShapes;
    public event NotifyCollectionChangedEventHandler? CollectionChanged; // This is only used for UI

    public void Add(DrawingShape drawingShape)
    {
        _drawingShapes.Add(drawingShape);
        drawingShape.PropertyChanged += DrawingShapePropertyChanged;
        drawingShape.OnPointsChanged += DrawingShapePointsChanged;
        drawingShape.OnRenderChanged += RenderShapes;
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
        drawingShape.OnRenderChanged += RenderShapes;
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
        drawingShape.OnRenderChanged -= RenderShapes;
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
            drawingShape.OnRenderChanged -= RenderShapes;
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

    public DrawingShape ElementAt(int index)
    {
        return _drawingShapes.ElementAt(index);
    }

    public void Transform(Matrix matrix)
    {
        foreach (var shape in _drawingShapes)
        {
            shape.Transform(matrix);
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

    private void RenderShapes(object? sender, EventArgs e)
    {
        OnRenderShapes?.Invoke(this, new EventArgs());
    }
}
