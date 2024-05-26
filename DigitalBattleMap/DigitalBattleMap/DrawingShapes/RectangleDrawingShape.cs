using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;

namespace DigitalBattleMap.DrawingShapes;

public class RectangleDrawingShape : DrawingShape
{
    private Point<double> _startPosition;
    private Point<double> _previousMovePosition;
    private double _radiusX;
    private double _radiusY;

    public RectangleDrawingShape(Action applyShapeCallback, ITokenLinker tokenLinker, IMapSize mapSize) : base(applyShapeCallback, tokenLinker, mapSize)
    {
        Name = "Rectangle";
        SnapToGrid = true;
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
        if(buttonDown)
        {
            var snappedPosition = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2) : position;
            if(snappedPosition != _previousMovePosition)
            {
                _previousMovePosition = snappedPosition;
                Points.Clear();

                _radiusX = Math.Abs(_startPosition.X - snappedPosition.X);
                _radiusY = Math.Abs(_startPosition.Y - snappedPosition.Y);

                Points.Add(new Point<double>(_startPosition.X, _startPosition.Y));
                Points.Add(new Point<double>(_startPosition.X + _radiusX, _startPosition.Y - _radiusY));
                Points.Add(new Point<double>(_startPosition.X + _radiusX, _startPosition.Y + _radiusY));
                Points.Add(new Point<double>(_startPosition.X - _radiusX, _startPosition.Y + _radiusY));
                Points.Add(new Point<double>(_startPosition.X - _radiusX, _startPosition.Y - _radiusY));
                Points.Add(new Point<double>(_startPosition.X + _radiusX, _startPosition.Y - _radiusY));

                CalculateSize();
            }
        }
    }

    private void CalculateSize()
    {
        var gridCellsX = _radiusX * 2 / _mapSize.CanvasGridSize;
        var gridCellsY = _radiusY * 2 / _mapSize.CanvasGridSize;
        var feedX = Math.Round(gridCellsX * Constants.FeetPerGridCell);
        var feedY = Math.Round(gridCellsY * Constants.FeetPerGridCell);
        Size = $"{feedX}x{feedY}";
    }
}
