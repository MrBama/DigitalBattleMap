using DigitalBattleMap.DataClasses;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using DigitalBattleMap.DrawingShapes;
using System.Windows.Ink;

namespace DigitalBattleMap.UIElements;

public class DrawingCanvas : InkCanvas
{
    private static EventHandler<DrawingShapeCollectionChangedEventArgs> _shapeCollectionChangedHandler;
    private static PropertyChangedEventHandler? _shapePropertyChangedHandler;
    private static NotifyCollectionChangedEventHandler? _shapePointsChangedHandler;
    private Dictionary<DrawingShape, Stroke> _strokes = new();

    public DrawingCanvas()
    {
        _shapeCollectionChangedHandler = OnDrawingShapeCollectionChanged;
        _shapePropertyChangedHandler = OnShapePropertyChanged;
        _shapePointsChangedHandler = OnShapePointsChanged;
        ClipToBounds = true;
    }

    public static readonly DependencyProperty ActiveShapeProperty = DependencyProperty.Register(nameof(ActiveShape), typeof(DrawingShape), typeof(DrawingCanvas), new PropertyMetadata(OnActiveShapeDependencyPropertyChanged));
    public static readonly DependencyProperty ShapeCollectionProperty = DependencyProperty.Register(nameof(ShapeCollection), typeof(DrawingShapeCollection), typeof(DrawingCanvas), new PropertyMetadata(OnShapesDependencyPropertyChanged));


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
        var canvas = (DrawingCanvas)dependencyObject;

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
        var canvas = (DrawingCanvas)dependencyObject;

        if (eventArgs.OldValue is DrawingShapeCollection oldShapeCollection)
        {
            oldShapeCollection.OnDrawingShapeCollectionChanged -= _shapeCollectionChangedHandler;
            oldShapeCollection.OnDrawingShapePropertyChanged -= _shapePropertyChangedHandler;
            oldShapeCollection.OnDrawingShapePointsChanged -= _shapePointsChangedHandler;
            canvas.EraseAll();
        }

        if (eventArgs.NewValue is DrawingShapeCollection newShapeCollection)
        {
            newShapeCollection.OnDrawingShapeCollectionChanged += _shapeCollectionChangedHandler;
            newShapeCollection.OnDrawingShapePropertyChanged += _shapePropertyChangedHandler;
            newShapeCollection.OnDrawingShapePointsChanged += _shapePointsChangedHandler;
            canvas.DrawShapes(newShapeCollection.GetShapes());
        }
    }

    private void OnDrawingShapeCollectionChanged(object? sender, DrawingShapeCollectionChangedEventArgs e)
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
        var properties = new List<string> { nameof(DrawingShape.Size), nameof(DrawingShape.Color), nameof(DrawingShape.Points) };
        if (sender is DrawingShape drawingShape)
        {
            if (properties.Contains(e.PropertyName))
            {
                if(_strokes.ContainsKey(drawingShape))
                {
                    var index = Strokes.IndexOf(_strokes[drawingShape]);
                    EraseShape(drawingShape);
                    InsertShape(index, drawingShape);
                }
                else
                {
                    EraseShape(drawingShape);
                    DrawShape(drawingShape);
                }
            }
        }
    }

    private void OnShapePointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is DrawingShape shape)
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

    private void EraseShape(DrawingShape shape)
    {
        if (_strokes.ContainsKey(shape))
        {
            Strokes.Remove(_strokes[shape]);
            _strokes.Remove(shape);
        }
    }

    private void EraseActiveShape(DrawingShape shape)
    {
        if (!ShapeCollection.GetShapes().Contains(shape))
        {
            EraseShape(shape);
        }
    }

    private void ErasePoints(DrawingShape shape)
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

    private void DrawShape(DrawingShape shape)
    {
        if (!_strokes.ContainsKey(shape) && shape.Points.Count > 0)
        {
            var stroke = CreateStroke(shape);
            Strokes.Add(stroke);
            _strokes[shape] = stroke;
        }
    }

    private void InsertShape(int index, DrawingShape shape)
    {
        if (!_strokes.ContainsKey(shape) && shape.Points.Count > 0)
        {
            var stroke = CreateStroke(shape);
            Strokes.Insert(index, stroke);
            _strokes[shape] = stroke;
        }
    }

    private void DrawShapes(IEnumerable<DrawingShape> shapes)
    {
        foreach (var shape in shapes)
        {
            DrawShape(shape);
        }
    }

    private void DrawPoints(DrawingShape shape)
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

    private Stroke CreateStroke(DrawingShape shape)
    {
        var stroke = new Stroke(ConvertToStylusPointCollection(shape));
        stroke.DrawingAttributes.Width = shape.Size;
        stroke.DrawingAttributes.Height = shape.Size;
        stroke.DrawingAttributes.Color = shape.Color;

        return stroke;
    }

    private static StylusPointCollection ConvertToStylusPointCollection(DrawingShape drawingShape)
    {
        var stylusPoints = new StylusPointCollection();
        foreach (var point in drawingShape.Points)
        {
            stylusPoints.Add(new StylusPoint(point.X, point.Y, 0.36f));
        }

        return stylusPoints;
    }
}
