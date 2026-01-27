using DigitalBattleMap.DataClasses;
using DigitalBattleMap.DrawingShapes;
using DigitalBattleMap.FogShapes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace DigitalBattleMap.UIElements;

public class FogCanvas : InkCanvas
{
    private static EventHandler<FogShapeCollectionChangedEventArgs> _shapeCollectionChangedHandler;
    private static PropertyChangedEventHandler? _shapePropertyChangedHandler;
    private static NotifyCollectionChangedEventHandler? _shapePointsChangedHandler;
    private Dictionary<FogShape, Stroke> _strokes = new();

    public FogCanvas()
    {
        _shapeCollectionChangedHandler = OnFogShapeCollectionChanged;
        _shapePropertyChangedHandler = OnShapePropertyChanged;
        _shapePointsChangedHandler = OnShapePointsChanged;
        ClipToBounds = true;
    }

    public static readonly DependencyProperty ActiveShapeProperty = DependencyProperty.Register(nameof(ActiveShape), typeof(FogShape), typeof(FogCanvas), new PropertyMetadata(OnActiveShapeDependencyPropertyChanged));
    public static readonly DependencyProperty ShapeCollectionProperty = DependencyProperty.Register(nameof(ShapeCollection), typeof(FogShapeCollection), typeof(FogCanvas), new PropertyMetadata(OnShapesDependencyPropertyChanged));


    public FogShape ActiveShape
    {
        get => (FogShape)GetValue(ActiveShapeProperty);
        set => SetValue(ActiveShapeProperty, value);
    }

    public FogShapeCollection ShapeCollection
    {
        get => (FogShapeCollection)GetValue(ShapeCollectionProperty);
        set => SetValue(ShapeCollectionProperty, value);
    }

    private static void OnActiveShapeDependencyPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var canvas = (FogCanvas)dependencyObject;

        if (eventArgs.OldValue is FogShape oldShape)
        {
            oldShape.PropertyChanged -= _shapePropertyChangedHandler;
            oldShape.OnPointsChanged -= _shapePointsChangedHandler;
            canvas.EraseActiveShape(oldShape);
        }

        if (eventArgs.NewValue is FogShape newShape)
        {
            newShape.PropertyChanged += _shapePropertyChangedHandler;
            newShape.OnPointsChanged += _shapePointsChangedHandler;
        }
    }

    private static void OnShapesDependencyPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var canvas = (FogCanvas)dependencyObject;

        if (eventArgs.OldValue is FogShapeCollection oldShapeCollection)
        {
            oldShapeCollection.OnFogShapeCollectionChanged -= _shapeCollectionChangedHandler;
            oldShapeCollection.OnFogShapePropertyChanged -= _shapePropertyChangedHandler;
            oldShapeCollection.OnFogShapePointsChanged -= _shapePointsChangedHandler;
            canvas.EraseAll();
        }

        if (eventArgs.NewValue is FogShapeCollection newShapeCollection)
        {
            newShapeCollection.OnFogShapeCollectionChanged += _shapeCollectionChangedHandler;
            newShapeCollection.OnFogShapePropertyChanged += _shapePropertyChangedHandler;
            newShapeCollection.OnFogShapePointsChanged += _shapePointsChangedHandler;
            canvas.DrawShapes(newShapeCollection.GetFogShapes());
        }
    }

    private void OnFogShapeCollectionChanged(object? sender, FogShapeCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case CollectionChangedAction.Add:
                DrawShape(e.ChangedShape);
                break;
            case CollectionChangedAction.Insert:
                InsertShape(e.Index, e.ChangedShape);
                break;
            case CollectionChangedAction.Remove:
                EraseShape(e.ChangedShape);
                break;
            case CollectionChangedAction.Clear:
                EraseAll();
                break;
            default:
                break;
        }
    }

    private void OnShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var properties = new List<string> { nameof(FogShape.PenSize), nameof(FogShape.Color), nameof(FogShape.Points) };
        if (sender is FogShape shape)
        {
            if (properties.Contains(e.PropertyName))
            {
                if (_strokes.ContainsKey(shape))
                {
                    var index = Strokes.IndexOf(_strokes[shape]);
                    EraseShape(shape);
                    InsertShape(index, shape);
                }
                else
                {
                    EraseShape(shape);
                    DrawShape(shape);
                }
            }
        }
    }

    private void OnShapePointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is FogShape shape)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    DrawPoints(shape);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    ErasePoints(shape);
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
        Strokes.Clear();
        _strokes.Clear();
    }

    private void EraseShape(FogShape shape)
    {
        if (_strokes.ContainsKey(shape))
        {
            Strokes.Remove(_strokes[shape]);
            _strokes.Remove(shape);
        }
    }

    private void EraseActiveShape(FogShape shape)
    {
        if (!ShapeCollection.GetFogShapes().Contains(shape))
        {
            EraseShape(shape);
        }
    }

    private void ErasePoints(FogShape shape)
    {
        if (shape.Points.Count > 0)
        {
            if (_strokes.ContainsKey(shape))
            {
                _strokes[shape].StylusPoints = ConvertToStylusPointCollection(shape);
            }
        }
        else
        {
            EraseShape(shape);
        }
    }

    private void DrawShape(FogShape shape)
    {
        if (!_strokes.ContainsKey(shape) && shape.Points.Count > 0)
        {
            var stroke = CreateStroke(shape);
            Strokes.Add(stroke);
            _strokes[shape] = stroke;
        }
    }

    private void InsertShape(int index, FogShape shape)
    {
        if (!_strokes.ContainsKey(shape) && shape.Points.Count > 0)
        {
            var stroke = CreateStroke(shape);
            Strokes.Insert(index, stroke);
            _strokes[shape] = stroke;
        }
    }

    private void DrawShapes(IEnumerable<FogShape> shapes)
    {
        foreach (var shape in shapes)
        {
            DrawShape(shape);
        }
    }

    private void DrawPoints(FogShape shape)
    {
        if (shape.Points.Count > 0)
        {
            if (_strokes.ContainsKey(shape))
            {
                _strokes[shape].StylusPoints = ConvertToStylusPointCollection(shape);
            }
            else
            {
                DrawShape(shape);
            }
        }
    }

    private Stroke CreateStroke(FogShape shape)
    {
        var stroke = new Stroke(ConvertToStylusPointCollection(shape));
        stroke.DrawingAttributes.Width = shape.PenSizeCanvas;
        stroke.DrawingAttributes.Height = shape.PenSizeCanvas;
        stroke.DrawingAttributes.Color = shape.Color;
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
