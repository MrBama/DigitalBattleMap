using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

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

    public override bool IsRotateShapeSupported => true;

    protected override void ButtonDown(Point<double> position)
    {
        _startPosition = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2) : position;
        Points.Add(position);
    }

    protected override void ButtonUp(Point<double> position)
    {
        CentersOfRotation.Add(_startPosition);
        CentersOfRotation.Add(_previousMovePosition);
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

    protected override Point<double> GetCenterOfRotation(Point<double> position)
    {
        var distanceX = CentersOfRotation[0].X - position.X;
        var distanceY = CentersOfRotation[0].Y - position.Y;
        var longestDistance = Math.Sqrt(Math.Pow(distanceX, 2) + Math.Pow(distanceY, 2));
        var index = 0;

        for (int i = 1; i < CentersOfRotation.Count; i++)
        {
            var disX = CentersOfRotation[i].X - position.X;
            var disY = CentersOfRotation[i].Y - position.Y;
            var dis = Math.Sqrt(Math.Pow(disX, 2) + Math.Pow(disY, 2));
            if (dis > longestDistance)
            {
                longestDistance = dis;
                index = i;
            }
        }

        return CentersOfRotation[index];
    }

    private void CalculateSize()
    {
        var distance = new Point<double>(_startPosition.X - _previousMovePosition.X, _startPosition.Y - _previousMovePosition.Y);
        var radius = Math.Sqrt(Math.Pow(distance.X, 2) + Math.Pow(distance.Y, 2));
        var gridCells = radius / _mapSize.CanvasGridSize;
        SizeLabel = $"{Math.Round(gridCells * Constants.FeetPerGridCell)} ft";
    }
}
