using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DrawingCanvas;

public class DrawingShapeCollection : IEnumerable, INotifyCollectionChanged
{
    private List<DrawingShape> _drawingShapes = new();

    public event NotifyCollectionChangedEventHandler? OnDrawingShapePointsChanged;
    public event PropertyChangedEventHandler? OnDrawingShapePropertyChanged;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public void Add(DrawingShape drawingShape)
    {
        _drawingShapes.Add(drawingShape);
        drawingShape.PropertyChanged += DrawingShapePropertyChanged;
        drawingShape.OnPointsChanged += DrawingShapePointsChanged;

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
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool Contains(DrawingShape drawingShape)
    {
        return _drawingShapes.Contains(drawingShape);
    }

    public IEnumerable<DrawingShape> GetDrawingShapes()
    {
        return _drawingShapes;
    }

    public IEnumerator GetEnumerator()
    {
        return _drawingShapes.GetEnumerator();
    }

    private void DrawingShapePointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnDrawingShapePointsChanged?.Invoke(sender, e);
    }

    private void DrawingShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnDrawingShapePropertyChanged?.Invoke(sender, e);
    }
}
