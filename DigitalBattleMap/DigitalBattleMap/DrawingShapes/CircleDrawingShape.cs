using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;

namespace DigitalBattleMap.DrawingShapes;

internal class CircleDrawingShape : DrawingShape
{
    private Point<double> _startPosition;
    private Point<double> _previousMovePosition;

    public CircleDrawingShape(Action applyShapeCallback, ITokenLinker tokenLinker, ICanvasSize canvasSize, int gridSize) : base(applyShapeCallback, tokenLinker, canvasSize, gridSize)
    {
        Name = "Circle";
    }

    protected override void ButtonDown(Point<double> position)
    {
        _startPosition = Mathematics.SnapPointToCanvasGrid(position, _canvasSize, _gridSize, _gridSize / 2);
        Points.Add(position);
    }

    protected override void ButtonUp(Point<double> position)
    {
        Points.RemoveAt(0); // Remove middle
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

                var startOfCircle = new Point<double>(snappedPosition);
                for (int i = 0; i <= 360; i += 2)
                {
                    Points.Add(startOfCircle.Rotate(_startPosition, i));
                }

                Points.Insert(0, _startPosition); // Draw middle
            }
        }
    }
}
