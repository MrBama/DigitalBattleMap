using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;

namespace DigitalBattleMap.DrawingShapes;

public class LineDrawingShape : DrawingShape
{
    private Point<double> _startPosition;
    private Point<double> _previousMovePosition;

    public LineDrawingShape(Action applyShapeCallback, ITokenLinker tokenLinker, IMapSize mapSize) : base(applyShapeCallback, tokenLinker, mapSize)
    {
        Name = "Line";
        SnapToGrid = true;
    }

    protected override void ButtonDown(Point<double> position)
    {
        _startPosition = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2) : position;
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
            var snappedPosition = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2) : position;
            if (snappedPosition != _previousMovePosition)
            {
                _previousMovePosition = snappedPosition;
                Points.Clear();

                var distanceX = _startPosition.X - snappedPosition.X;
                var distanceY = _startPosition.Y - snappedPosition.Y;

                var lineAngle = Math.Atan2(distanceY, distanceX);
                var lineAngleInDegrees = lineAngle / Math.PI * 180;

                var halfGridSizeCanvas = _mapSize.CanvasGridSize / 2;

                // Add a point on the line that will be rotated to the corners
                var startPosition = new Point<double>(_startPosition.X + halfGridSizeCanvas, _startPosition.Y);
                var endPosition = new Point<double>(snappedPosition.X + halfGridSizeCanvas, snappedPosition.Y);

                // Rotate the point to be perpendicular to the original line while keeping in mind the original angle of the line
                Points.Add(startPosition.Rotate(_startPosition, lineAngleInDegrees + 90));
                Points.Add(startPosition.Rotate(_startPosition, lineAngleInDegrees - 90));
                Points.Add(endPosition.Rotate(snappedPosition, lineAngleInDegrees - 90));
                Points.Add(endPosition.Rotate(snappedPosition, lineAngleInDegrees + 90));
                Points.Add(startPosition.Rotate(_startPosition, lineAngleInDegrees + 90));

                CalculateSize();
            }

        }
    }

    private void CalculateSize()
    {
        var distance = new Point<double>(_startPosition.X - _previousMovePosition.X, _startPosition.Y - _previousMovePosition.Y);
        var radius = Math.Sqrt(Math.Pow(distance.X, 2) + Math.Pow(distance.Y, 2));
        var gridCells = radius / _mapSize.CanvasGridSize;
        Size = $"{Math.Round(gridCells * Constants.FeetPerGridCell)}";
    }
}
