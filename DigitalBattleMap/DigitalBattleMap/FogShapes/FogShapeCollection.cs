using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace DigitalBattleMap.FogShapes;

public class FogShapeCollection : PropertyHandler, IEnumerable, INotifyCollectionChanged
{
    private List<FogShape> _fogShapes = new();

    public event NotifyCollectionChangedEventHandler? OnFogShapePointsChanged;
    public event PropertyChangedEventHandler? OnFogShapePropertyChanged;
    public event EventHandler<FogShapeCollectionChangedEventArgs> OnFogShapeCollectionChanged;
    public event EventHandler OnRenderShapes;
    public event NotifyCollectionChangedEventHandler? CollectionChanged; // This is only used for UI

    public bool IsFillFogEnabled { get => Get<bool>(); set => Set(value); }

    public void Add(FogShape fogShape)
    {
        if (!fogShape.Points.Any())
        {
            return;
        }
        _fogShapes.Add(fogShape);
        fogShape.PropertyChanged += FogShapePropertyChanged;
        fogShape.OnPointsChanged += FogShapePointsChanged;
        fogShape.OnRenderChanged += RenderShapes;

        FogShapeCollectionChanged(new FogShapeCollectionChangedEventArgs { Action = CollectionChangedAction.Add, ChangedShape = fogShape });
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
        fogShape.ApplyShape(); // remove from map
    }

    public void RemoveByUI(FogShape fogShape)
    {
        var index = _fogShapes.ToList().IndexOf(fogShape);
        fogShape.PropertyChanged -= FogShapePropertyChanged;
        fogShape.OnPointsChanged -= FogShapePointsChanged;
        fogShape.OnRenderChanged -= RenderShapes;
        _fogShapes.Remove(fogShape);

        FogShapeCollectionChanged(new FogShapeCollectionChangedEventArgs { Action = CollectionChangedAction.Remove, ChangedShape = fogShape });

        fogShape.ApplyShape(); // remove from map
    }

    public void Clear()
    {
        foreach (var fogShape in _fogShapes)
        {
            fogShape.PropertyChanged -= FogShapePropertyChanged;
            fogShape.OnPointsChanged -= FogShapePointsChanged;
            fogShape.OnRenderChanged -= RenderShapes;
        }
        _fogShapes.Clear();
        IsFillFogEnabled = false;
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
        if(e.PropertyName == nameof(FogShape.IsDeleted))
        {
            Remove(sender as FogShape);
        }
        else
        {
            OnFogShapePropertyChanged?.Invoke(sender, e);

            if (e.PropertyName == nameof(FogShape.IsFogEnabled))
            {
                OnRenderShapes?.Invoke(this, new EventArgs());
            }
        }
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

    internal void ToggleFog(Point<double> position)
    {
        var containingShapes = _fogShapes.Where(fog => fog.PositionInside(position)).ToList();
        if (!containingShapes.Any())
        {
            return;
        }

        var smallest = containingShapes.OrderBy(fog => PolygonArea(fog.Points.ToList())).First();
        smallest.IsFogEnabled = !smallest.IsFogEnabled;
    }

    private static double PolygonArea(List<Point<double>> points)
    {
        double area = 0;
        int n = points.Count;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            area += points[i].X * points[j].Y;
            area -= points[j].X * points[i].Y;
        }
        return Math.Abs(area) / 2.0;
    }
}
