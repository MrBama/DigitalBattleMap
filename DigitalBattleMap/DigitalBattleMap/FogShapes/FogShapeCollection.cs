using DigitalBattleMap.DataClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace DigitalBattleMap.FogShapes;

public class FogShapeCollection : IEnumerable, INotifyCollectionChanged
{
    private List<FogShape> _fogShapes = new();

    public event NotifyCollectionChangedEventHandler? OnFogShapePointsChanged;
    public event PropertyChangedEventHandler? OnFogShapePropertyChanged;
    public event EventHandler<FogShapeCollectionChangedEventArgs> OnFogShapeCollectionChanged;
    public event EventHandler OnRenderShapes; // todo change?
    public event NotifyCollectionChangedEventHandler? CollectionChanged; // This is only used for UI

    public void Add(FogShape fogShape)
    {
        _fogShapes.Add(fogShape);
        fogShape.PropertyChanged += FogShapePropertyChanged;
        fogShape.OnPointsChanged += FogShapePointsChanged;
        fogShape.OnRenderChanged += RenderShapes; // todo change?
        FogShapeCollectionChanged(new FogShapeCollectionChangedEventArgs { Action = CollectionChangedAction.Add, ChangedShape = fogShape });
        FogShapeUICollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, fogShape));
    }

    public void Insert(int index, FogShape fogShape)
    {
        _fogShapes.Insert(index, fogShape);
        fogShape.PropertyChanged += FogShapePropertyChanged;
        fogShape.OnPointsChanged += FogShapePointsChanged;
        fogShape.OnRenderChanged += RenderShapes;
        FogShapeCollectionChanged(new FogShapeCollectionChangedEventArgs { Action = CollectionChangedAction.Insert, ChangedShape = fogShape, Index = index });
        FogShapeUICollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, fogShape));
    }

    public void Remove(FogShape fogShape)
    {
        var index = _fogShapes.ToList().IndexOf(fogShape);
        fogShape.PropertyChanged -= FogShapePropertyChanged;
        fogShape.OnPointsChanged -= FogShapePointsChanged;
        fogShape.OnRenderChanged -= RenderShapes;
        _fogShapes.Remove(fogShape);

        FogShapeCollectionChanged(new FogShapeCollectionChangedEventArgs { Action = CollectionChangedAction.Remove, ChangedShape = fogShape });
        FogShapeUICollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, fogShape, index));
    }

    public void Clear()
    {
        foreach (var drawingShape in _fogShapes)
        {
            drawingShape.PropertyChanged -= FogShapePropertyChanged;
            drawingShape.OnPointsChanged -= FogShapePointsChanged;
            drawingShape.OnRenderChanged -= RenderShapes;
        }
        _fogShapes.Clear();
        FogShapeCollectionChanged(new FogShapeCollectionChangedEventArgs { Action = CollectionChangedAction.Clear });
        FogShapeUICollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool Contains(FogShape fogShape)
    {
        return _fogShapes.Contains(fogShape);
    }

    public IEnumerable<FogShape> GetFogShapes()
    {
        return _fogShapes;
    }

    public IEnumerator GetEnumerator()
    {
        return _fogShapes.GetEnumerator();
    }

    public int IndexOf(FogShape fogShape)
    {
        return _fogShapes.IndexOf(fogShape);
    }

    public FogShape ElementAt(int index)
    {
        return _fogShapes.ElementAt(index);
    }

    public void Transform(Matrix matrix)
    {
        foreach (var shape in _fogShapes)
        {
            shape.Transform(matrix);
        }
    }

    private void FogShapePointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnFogShapePointsChanged?.Invoke(sender, e);
    }

    private void FogShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnFogShapePropertyChanged?.Invoke(sender, e);
    }

    private void FogShapeCollectionChanged(FogShapeCollectionChangedEventArgs eventArgs)
    {
        OnFogShapeCollectionChanged?.Invoke(this, eventArgs);
    }

    private void FogShapeUICollectionChanged(NotifyCollectionChangedEventArgs eventArgs)
    {
        CollectionChanged?.Invoke(this, eventArgs);
    }

    private void RenderShapes(object? sender, EventArgs e)
    {
        OnRenderShapes?.Invoke(this, new EventArgs());
    }
}
