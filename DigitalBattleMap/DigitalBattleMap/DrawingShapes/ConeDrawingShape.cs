using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;

namespace DigitalBattleMap.DrawingShapes;

public class ConeDrawingShape : DrawingShape
{
    private Point<double> _startPosition;
    private Point<double> _previousMovePosition;

    public ConeDrawingShape(Action applyShapeCallback, ITokenLinker tokenLinker, ICanvasSize canvasSize, int gridSize) : base(applyShapeCallback, tokenLinker, canvasSize, gridSize)
    {
        Name = "Cone";
    }

    protected override void ButtonDown(Point<double> position)
    {
        _startPosition = Mathematics.SnapPointToCanvasGrid(position, _canvasSize, _gridSize, _gridSize / 2);
        Points.Add(position);
    }

    protected override void ButtonUp(Point<double> position)
    {
        ApplyShape();
    }

    protected override void MouseMove(Point<double> position, bool buttonDown)
    {
        if (buttonDown)
        {
            var snappedPosition = Mathematics.SnapPointToCanvasGrid(position, _canvasSize, _gridSize, _gridSize / 2);
            if (snappedPosition != _previousMovePosition)
            {
                _previousMovePosition = snappedPosition;
                Points.Clear();

                var startOfCone = new Point<double>(snappedPosition).Rotate(_startPosition, -30);
                Points.Add(_startPosition);
                Points.Add(startOfCone);

                for (int i = 0; i <= 60; i += 2)
                {
                    Points.Add(startOfCone.Rotate(_startPosition, i));
                }

                Points.Add(_startPosition);
            }
        }
    }
}
