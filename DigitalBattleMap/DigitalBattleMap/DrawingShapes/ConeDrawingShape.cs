using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;

namespace DigitalBattleMap.DrawingShapes;

public class ConeDrawingShape : DrawingShape
{
    private Point<double> _startPosition;
    private Point<double> _previousMovePosition;

    public ConeDrawingShape(Action applyShapeCallback, ITokenLinker tokenLinker, IMapSize mapSize) : base(applyShapeCallback, tokenLinker, mapSize)
    {
        Name = "Cone";
        SnapToGrid = true;
    }

    public override bool IsRotateShapeSupported => true;

    protected override void ButtonDown(Point<double> position)
    {
        _startPosition = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2) : position;
        Points.Add(position);
    }

    protected override void ButtonUp(Point<double> position)
    {
        CentersOfRotation.Add(_startPosition);
        RotationMarkers.Add(_previousMovePosition);
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

                var startOfCone = new Point<double>(snappedPosition).Rotate(_startPosition, -30);
                Points.Add(_startPosition);
                Points.Add(startOfCone);

                for (int i = 0; i <= 60; i += 2)
                {
                    Points.Add(startOfCone.Rotate(_startPosition, i));
                }

                Points.Add(_startPosition);
                CalculateSize();
            }
        }
    }

    private void CalculateSize()
    {
        var distance = new Point<double>(_startPosition.X - _previousMovePosition.X, _startPosition.Y - _previousMovePosition.Y);
        var radius = Math.Sqrt(Math.Pow(distance.X, 2) + Math.Pow(distance.Y, 2));
        var gridCells = radius / _mapSize.CanvasGridSize;
        SizeLabel = $"{Math.Round(gridCells * Constants.FeetPerGridCell)} ft";
    }
}
