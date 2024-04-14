using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;

namespace DigitalBattleMap.DrawingShapes;

public class RectangleDrawingShape : DrawingShape
{
    private Point<double> _startPosition;
    private Point<double> _previousMovePosition;
    private double _distanceX;
    private double _distanceY;

    public RectangleDrawingShape(Action applyShapeCallback, ICanvasSize canvasSize, int gridSize) : base(applyShapeCallback, canvasSize, gridSize)
    {
        Name = "Rectangle";
    }

    protected override void ButtonDown(Point<double> position)
    {
        _startPosition = Mathematics.SnapPointToCanvasGrid(position, _canvasSize, _gridSize, _gridSize / 2);
        Points.Add(position);
    }

    protected override void ButtonUp(Point<double> position)
    {
        Points.Clear();
        Points.Add(new Point<double>(_startPosition.X + _distanceX, _startPosition.Y - _distanceY));
        Points.Add(new Point<double>(_startPosition.X + _distanceX, _startPosition.Y + _distanceY));
        Points.Add(new Point<double>(_startPosition.X - _distanceX, _startPosition.Y + _distanceY));
        Points.Add(new Point<double>(_startPosition.X - _distanceX, _startPosition.Y - _distanceY));
        Points.Add(new Point<double>(_startPosition.X + _distanceX, _startPosition.Y - _distanceY));

        ApplyShape();
    }

    protected override void MouseMove(Point<double> position, bool buttonDown)
    {
        if(buttonDown)
        {
            var snappedPosition = Mathematics.SnapPointToCanvasGrid(position, _canvasSize, _gridSize, _gridSize / 2);
            if(snappedPosition != _previousMovePosition)
            {
                _previousMovePosition = snappedPosition;
                Points.Clear();

                _distanceX = Math.Abs(_startPosition.X - snappedPosition.X);
                _distanceY = Math.Abs(_startPosition.Y - snappedPosition.Y);

                Points.Add(new Point<double>(_startPosition.X, _startPosition.Y));
                Points.Add(new Point<double>(_startPosition.X + _distanceX, _startPosition.Y - _distanceY));
                Points.Add(new Point<double>(_startPosition.X + _distanceX, _startPosition.Y + _distanceY));
                Points.Add(new Point<double>(_startPosition.X - _distanceX, _startPosition.Y + _distanceY));
                Points.Add(new Point<double>(_startPosition.X - _distanceX, _startPosition.Y - _distanceY));
                Points.Add(new Point<double>(_startPosition.X + _distanceX, _startPosition.Y - _distanceY));
            }
        }
    }
}
