using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;

namespace DigitalBattleMap.FogShapes;

internal class CircleFogShape : FogShape
{
    private Point<double> _startPosition;
    private Point<double> _previousMovePosition;

    public CircleFogShape(Action applyShapeCallback, IMapSize mapSize) : base(applyShapeCallback, mapSize)
    {
        Name = "Circle";
        SnapToGrid = true;
    }

    public override FogShape Clone()
    {
        return new CircleFogShape(_applyShapeCallback, _mapSize) { SnapToGrid = SnapToGrid, IsFogEnabled = IsFogEnabled };
    }

    protected override void ButtonDown(Point<double> position)
    {
        _startPosition = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2) : position;
        Points.Add(position);
    }

    protected override void ButtonUp(Point<double> position)
    {
        Points.RemoveAt(0); // Remove middle
        ApplyShape();
    }

    protected override void MouseMove(Point<double> position, bool buttonDown)
    {
        if (buttonDown)
        {
            var snappedPosition = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2) : position;
            if (snappedPosition != _previousMovePosition)
            {
                _previousMovePosition = snappedPosition;
                Points.Clear();

                var startOfCircle = new Point<double>(snappedPosition);
                for (int i = 0; i <= 360; i += 2)
                {
                    Points.Add(startOfCircle.Rotate(_startPosition, i));
                }

                Points.Insert(0, _startPosition); // Draw middle
                CalculateSize();
            }
        }
    }

    private void CalculateSize()
    {
        var distance = new Point<double>(_startPosition.X - _previousMovePosition.X, _startPosition.Y - _previousMovePosition.Y);
        var radius = Math.Sqrt(Math.Pow(distance.X, 2) + Math.Pow(distance.Y, 2));
        var gridCells = radius * 2 / _mapSize.CanvasGridSize;
        Size = $"{Math.Round(gridCells * Constants.FeetPerGridCell)}";
    }
}
