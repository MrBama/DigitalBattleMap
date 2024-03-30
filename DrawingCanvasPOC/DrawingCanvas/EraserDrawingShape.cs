using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawingCanvas;

public class EraserDrawingShape : DrawingShape
{
    private DrawingShapeCollection _drawingShapeCollection;

    public EraserDrawingShape(DrawingShapeCollection drawingShapeCollection) : base(() => { })
    {
        _drawingShapeCollection = drawingShapeCollection;
    }

    public override Cursor Cursor { get => CursorHelper.CreateCursor(Brushes.White, new Pen(Brushes.Black, 1), Size); }

    protected override void ButtonDown(Point position)
    {
        Erase(position);
    }

    protected override void ButtonUp(Point position)
    {
    }

    protected override void MouseMove(Point position, bool buttonDown)
    {
        if (buttonDown)
        {
            Erase(position);
        }
    }

    private void Erase(Point position)
    {
        foreach (var shape in _drawingShapeCollection.GetDrawingShapes().ToList())
        {
            if (shape.IsErasable)
            {
                foreach (var point in shape.Points.ToList())
                {
                    var radius = Size / 2;
                    var zeroBasedPoint = new Point(point.X - position.X, point.Y - position.Y);

                    if (Math.Sqrt(Math.Pow(zeroBasedPoint.X, 2) + Math.Pow(zeroBasedPoint.Y, 2)) < radius)
                    {
                        shape.Points.Remove(point);

                        if (shape.Points.Count == 0)
                        {
                            _drawingShapeCollection.Remove(shape);
                        }
                    }
                }
            }
        }
    }
}
