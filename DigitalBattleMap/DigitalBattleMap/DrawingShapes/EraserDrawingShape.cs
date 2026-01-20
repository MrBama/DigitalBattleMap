using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace DigitalBattleMap.DrawingShapes;

public class EraserDrawingShape : DrawingShape
{
    private DrawingShapeCollection _drawingShapeCollection;
    private List<DrawingShape> _originalDrawingShapes = new();
    private List<DrawingShape> _addedShapes = new();
    private List<DrawingShape> _removedShapes = new();
    private Dictionary<DrawingShape, DrawingShapeInfo> _editedShapes = new();

    public EraserDrawingShape(DrawingShapeCollection drawingShapeCollection, IMapSize mapSize) : base(() => { }, null, mapSize)
    {
        _drawingShapeCollection = drawingShapeCollection;
    }

    public event EventHandler<DrawingShapeErasedEventArgs> OnErased;

    public override Cursor Cursor { get => CursorCreator.Create(Brushes.White, new Pen(Brushes.Black, 1), (int)Math.Max(8, PenSize)); }

    protected override void ButtonDown(Point<double> position)
    {
        _addedShapes.Clear();
        _removedShapes.Clear();
        _editedShapes.Clear();
        _originalDrawingShapes = _drawingShapeCollection.GetShapes().ToList();
        Erase(position);
    }

    protected override void ButtonUp(Point<double> position)
    {
        NotifyErased();
    }

    protected override void MouseMove(Point<double> position, bool buttonDown)
    {
        if (buttonDown)
        {
            Erase(position);
        }
    }

    private void Erase(Point<double> position)
    {
        foreach (var shape in _drawingShapeCollection.GetDrawingShapes().ToList())
        {
            if (shape.IsErasable)
            {
                foreach (var point in shape.Points.ToList())
                {
                    if (DoesPointOverlapWithEraser(point, shape.PenSizeCanvas, position))
                    {
                        RemovePointFromShape(shape, point, out var isSplit);
                        if (isSplit)
                            break;
                    }
                }
            }
        }
    }

    private bool DoesPointOverlapWithEraser(Point<double> point, double eraserSize, Point<double> mousePosition)
    {
        var radius = (PenSizeCanvas / 2) + (eraserSize / 2);
        var distance = new Point<double>(point.X - mousePosition.X, point.Y - mousePosition.Y);
        return Math.Sqrt(Math.Pow(distance.X, 2) + Math.Pow(distance.Y, 2)) < radius;
    }

    private void RemovePointFromShape(DrawingShape shape, Point<double> point, out bool isSplit)
    {
        isSplit = false;

        if(!_editedShapes.ContainsKey(shape))
        {
            _editedShapes[shape] = new DrawingShapeInfo(shape);
        }

        if (shape.Points.First() == point || shape.Points.Last() == point)
        {
            shape.Points.Remove(point);
            if (shape.Points.Count == 0)
            {
                _removedShapes.Add(shape);
                _drawingShapeCollection.Remove(shape);
            }
        }
        else
        {
            SplitShape(shape, point);
            isSplit = true;
        }
    }

    private void SplitShape(DrawingShape shape, Point<double> point)
    {
        var pointIndex = shape.Points.IndexOf(point);

        var newShape = new StrokeDrawingShape(() => { }, null, _mapSize)
        {
            PenSize = shape.PenSize,
            Color = shape.Color,
            Points = new ObservableCollection<Point<double>>(shape.Points.Skip(pointIndex))
        };

        shape.Points = new ObservableCollection<Point<double>>(shape.Points.Take(pointIndex));

        var shapeIndex = _drawingShapeCollection.IndexOf(shape);
        _drawingShapeCollection.Insert(shapeIndex, newShape);
        _addedShapes.Add(newShape);
    }

    private void NotifyErased()
    {
        var eraseCommands = new List<DrawingShapeCommand>();

        // Add commands for shapes that were added because of a split.
        // Make sure the split shape is not erased again afterwards.
        foreach (var addedShape in _addedShapes)
        {
            if(!_removedShapes.Contains(addedShape))
            {
                eraseCommands.Add(new DrawingShapeCommand(addedShape, DrawingShapeCommandAction.Add));
            }
        }

        // Add commands for shapes that where fully erased.
        // If the shape was added (because of a split) in the same erase move there is no need to do anything
        // The points of the shape need to be reverted because the points of the shape will first be removed before the shape is removed
        var removeCommands = new List<DrawingShapeCommand>();
        foreach (var removedShape in _removedShapes)
        {
            if(!_addedShapes.Contains(removedShape))
            {
                removedShape.Points = new ObservableCollection<Point<double>>(_editedShapes[removedShape].Points);
                removeCommands.Add(new DrawingShapeCommand(removedShape, DrawingShapeCommandAction.Remove) { RemovedAtIndex = _originalDrawingShapes.IndexOf(removedShape) });
            }
        }
        removeCommands.OrderCurrentBy(c => c.RemovedAtIndex);
        eraseCommands.AddRange(removeCommands);

        // Add commands for shapes that were partially erased
        // Only add an edit action if the shape was not added or removed in the same erase move
        foreach ((var editedShape, var editedShapeInfo) in _editedShapes)
        {
            if(!_addedShapes.Contains(editedShape) && !_removedShapes.Contains(editedShape))
            {
                eraseCommands.Add(new DrawingShapeCommand(editedShape, DrawingShapeCommandAction.Edit)
                {
                    OldInfo = editedShapeInfo,
                    NewInfo = new DrawingShapeInfo(editedShape)
                });
            }
        }

        OnErased?.Invoke(this, new DrawingShapeErasedEventArgs { EraseCommands = eraseCommands });
    }
}
