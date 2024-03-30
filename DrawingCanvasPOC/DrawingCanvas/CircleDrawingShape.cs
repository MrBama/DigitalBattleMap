using System;
using System.Windows;

namespace DrawingCanvas;

public class CircleDrawingShape : DrawingShape
{
    private Point _startPosition;
    private Point _previousMovePosition;
    private double _radius;

    public CircleDrawingShape(Action applyShapeCallback) : base(applyShapeCallback)
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
        DeterminePointsInCircle(0.05);
        Points.Add(new Point(_startPosition.X + _radius, _startPosition.Y));
        BitmapTools.SmoothLine(Points, Size);
        ApplyShape();
    }

    protected override void MouseMove(Point position, bool buttonDown)
    {
        if (buttonDown && position != _previousMovePosition)
        {
            _previousMovePosition = position;
            Points.Clear();

            var distanceX = Math.Abs(_startPosition.X - position.X);
            var distanceY = Math.Abs(_startPosition.Y - position.Y);
            _radius = Math.Sqrt(Math.Pow(distanceX, 2) + Math.Pow(distanceY, 2));

            DeterminePointsInCircle(0.5);
        }
    }

    private void DeterminePointsInCircle(double stepsize)
    {
        for (double i = 0; i <= 2 * Math.PI; i += stepsize)
        {
            var x = _startPosition.X + _radius * Math.Cos(i);
            var y = _startPosition.Y + _radius * Math.Sin(i);
            Points.Add(new Point(x, y));
        }
    }
}
