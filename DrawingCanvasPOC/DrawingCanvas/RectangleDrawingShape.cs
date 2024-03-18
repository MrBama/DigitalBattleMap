using System;
using System.Windows;

namespace DrawingCanvas;

public class RectangleDrawingShape : DrawingShape
{
    private Point _startPosition;
    private Point _previousMovePosition;
    private double _distanceX;
    private double _distanceY;

    public RectangleDrawingShape(IMouse mouse, Action applyShapeCallback) : base(mouse, applyShapeCallback)
    {
    }

    protected override void ButtonDown(Point position)
    {
        _startPosition = position;
        Points.Add(position);
    }

    protected override void ButtonUp(Point position)
    {
        Points.Clear();
        Points.Add(new Point(_startPosition.X - _distanceX, _startPosition.Y - _distanceY));
        Points.Add(new Point(_startPosition.X + _distanceX, _startPosition.Y - _distanceY));
        Points.Add(new Point(_startPosition.X + _distanceX, _startPosition.Y + _distanceY));
        Points.Add(new Point(_startPosition.X - _distanceX, _startPosition.Y + _distanceY));
        Points.Add(new Point(_startPosition.X - _distanceX, _startPosition.Y - _distanceY));

        BitmapTools.SmoothLine(Points, Size);
        ApplyShape();
    }

    protected override void MouseMove(Point position, bool buttonDown)
    {
        if (buttonDown && position != _previousMovePosition)
        {
            _previousMovePosition = position;
            Points.Clear();

            _distanceX = Math.Abs(_startPosition.X - position.X);
            _distanceY = Math.Abs(_startPosition.Y - position.Y);

            Points.Add(new Point(_startPosition.X - _distanceX, _startPosition.Y - _distanceY));
            Points.Add(new Point(_startPosition.X, _startPosition.Y - _distanceY));
            Points.Add(new Point(_startPosition.X + _distanceX, _startPosition.Y - _distanceY));

            Points.Add(new Point(_startPosition.X - _distanceX, _startPosition.Y));
            Points.Add(new Point(_startPosition.X, _startPosition.Y));
            Points.Add(new Point(_startPosition.X + _distanceX, _startPosition.Y));

            Points.Add(new Point(_startPosition.X - _distanceX, _startPosition.Y + _distanceY));
            Points.Add(new Point(_startPosition.X, _startPosition.Y + _distanceY));
            Points.Add(new Point(_startPosition.X + _distanceX, _startPosition.Y + _distanceY));
        }
    }
}
