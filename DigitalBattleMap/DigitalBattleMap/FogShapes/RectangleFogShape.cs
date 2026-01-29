using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;

namespace DigitalBattleMap.FogShapes;

public class RectangleFogShape : FogShape
{
    private Point<double> _startPosition;
    private Point<double> _previousMovePosition;

    public RectangleFogShape(Action applyShapeCallback, IMapSize mapSize) : base(applyShapeCallback, mapSize)
    {
        Name = "Fog Rectangle";
        SnapToGrid = true;
        IsFogEnabled = true;
    }

    public override FogShape Clone()
    {
        return new RectangleFogShape(_applyShapeCallback, _mapSize) { SnapToGrid = SnapToGrid, IsFogEnabled = IsFogEnabled }; 
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

    protected override void CancelButton()
    {
    }

    /**
     * Rectangle from corner to corner
     */
    protected override void MouseMove(Point<double> position, bool buttonDown)
    {
        if (buttonDown)
        {
            var snappedPosition = SnapToGrid
                ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2)
                : position;

            if (snappedPosition != _previousMovePosition)
            {
                _previousMovePosition = snappedPosition;
                Points.Clear();

                double x1 = _startPosition.X;
                double y1 = _startPosition.Y;
                double x2 = snappedPosition.X;
                double y2 = snappedPosition.Y;

                double left = Math.Min(x1, x2);
                double right = Math.Max(x1, x2);
                double top = Math.Min(y1, y2);
                double bottom = Math.Max(y1, y2);

                // Rectangle corners (closed path)
                Points.Add(new Point<double>(left, top));
                Points.Add(new Point<double>(right, top));
                Points.Add(new Point<double>(right, bottom));
                Points.Add(new Point<double>(left, bottom));
                Points.Add(new Point<double>(left, top));
            }
        }
    }
}
