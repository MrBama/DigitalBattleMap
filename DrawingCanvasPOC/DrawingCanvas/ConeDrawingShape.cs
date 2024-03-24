using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Xps.Serialization;

namespace DrawingCanvas;

public class ConeDrawingShape : DrawingShape
{
    private Point _startPosition;
    private Point _previousMovePosition;
    private double _radius;
    private double _circleStart;

    public ConeDrawingShape(IMouse mouse, Action applyShapeCallback) : base(mouse, applyShapeCallback)
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
        Points.Add(_startPosition);
        DeterminePointsInQuarterCircle(0.05);
        Points.Add(_startPosition);
        BitmapTools.SmoothLine(Points, Size);
        ApplyShape();
    }

    protected override void MouseMove(Point position, bool buttonDown)
    {
        if (buttonDown && position != _previousMovePosition)
        {
            _previousMovePosition = position;
            Points.Clear();

            Point point1 = new Point(0, 1); // up vector
            Point point2 = new Point(_startPosition.X - position.X, _startPosition.Y - position.Y); // normalized position vector

            double dot = point1.X * point2.X + point1.Y * point2.Y; // dot product
            double det = point1.X * point2.Y - point1.Y * point2.X; // determinant
            double angle = Math.Atan2(det, dot); // angle between up and postion
            _circleStart = angle - (Math.PI*3)/4 ; // set start equal to center of the quarter circle to draw

            var distanceX = Math.Abs(_startPosition.X - position.X);
            var distanceY = Math.Abs(_startPosition.Y - position.Y);
            _radius = Math.Sqrt(Math.Pow(distanceX, 2) + Math.Pow(distanceY, 2));

            DeterminePointsInQuarterCircle(0.1);
        }
    }

    private void DeterminePointsInQuarterCircle(double stepsize)
    {
        for (double i = _circleStart; i <= _circleStart + Math.PI / 2; i += stepsize) //devide by 2 to draw one quarter of the full circle
        {
            var x = _startPosition.X + _radius * Math.Cos(i);
            var y = _startPosition.Y + _radius * Math.Sin(i);
            Points.Add(new Point(x, y));
        }
    }
}
