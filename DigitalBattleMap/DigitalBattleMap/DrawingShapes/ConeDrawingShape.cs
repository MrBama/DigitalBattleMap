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

                // Calculate the lenght of the triangle and the sloped side
                var zeroPosition = new Point<double>(_startPosition.X - snappedPosition.X, _startPosition.Y - snappedPosition.Y);
                var middleLength = Math.Sqrt(Math.Pow(zeroPosition.X, 2) + Math.Pow(zeroPosition.Y, 2));
                var slopeLenght = Math.Sqrt(Math.Pow(middleLength, 2) + Math.Pow(middleLength/2, 2));
        
                // Calculate from middle to zero and from middle to slope
                var angleToZero = Math.Atan2(snappedPosition.Y - _startPosition.Y, snappedPosition.X - _startPosition.X);
                var angleOfTriangle = Math.Acos(middleLength / slopeLenght);

                // Calculate x and y of point 1 by adding the angles
                var x = Math.Cos(angleToZero + angleOfTriangle) * slopeLenght;
                var y = Math.Sin(angleToZero + angleOfTriangle) * slopeLenght;

                if (double.IsNaN(x) || double.IsNaN(y))
                    return;

                var point1 = new Point<double>(_startPosition.X + x, _startPosition.Y + y);

                // Calculate x and y of point 1 by subtracting the angles
                x = Math.Cos(angleToZero - angleOfTriangle) * slopeLenght;
                y = Math.Sin(angleToZero - angleOfTriangle) * slopeLenght;

                if (double.IsNaN(x) || double.IsNaN(y))
                    return;

                var point2 = new Point<double>(_startPosition.X + x, _startPosition.Y + y);

                // Add points for triangle using point 1, point 2 and start position
                Points.Add(_startPosition);
                Points.Add(point1);
                Points.Add(point2);
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
