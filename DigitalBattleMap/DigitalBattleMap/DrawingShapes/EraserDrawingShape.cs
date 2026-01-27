using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace DigitalBattleMap.DrawingShapes;

public class EraserDrawingShape : DrawingShape
{
    private DrawingShapeCollection _drawingShapeCollection;

    public EraserDrawingShape(DrawingShapeCollection drawingShapeCollection, IMapSize mapSize) : base(() => { }, null, mapSize)
    {
        _drawingShapeCollection = drawingShapeCollection;
    }

    public override Cursor Cursor { get => CursorCreator.Create(Brushes.White, new Pen(Brushes.Black, 1), (int)Math.Max(8, PenSize)); }

    protected override void ButtonDown(Point<double> position)
    {
        Erase(position);
    }

    protected override void ButtonUp(Point<double> position)
    {
        RenderShape();
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
        if (shape.Points.First() == point || shape.Points.Last() == point)
        {
            shape.Points.Remove(point);
            if (shape.Points.Count == 0)
            {
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
    }
}
