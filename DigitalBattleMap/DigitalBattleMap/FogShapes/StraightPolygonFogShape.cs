using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Linq;

namespace DigitalBattleMap.FogShapes;

public class StraightPolygonFogShape : FogShape
{
    public StraightPolygonFogShape(Action applyShapeCallback, IMapSize mapSize) : base(applyShapeCallback, mapSize)
    {
        IsFogEnabled = true;
        SnapToGrid = true;
    }

    public override FogShape Clone()
    {
        return new StraightPolygonFogShape(_applyShapeCallback, _mapSize) { SnapToGrid = SnapToGrid, IsFogEnabled = IsFogEnabled };
    }

    protected override void ButtonDown(Point<double> position)
    {
    }

    protected override void ButtonUp(Point<double> position)
    {
        var snappedPosition = SnapToGrid
            ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize)
            : position;

        // early exit
        if (SnapToGrid && Points.Any() && snappedPosition.Equals(Points.First()))
        {
            Points.Add(Points.First());
            ApplyShape();
        }

        Points.Add(snappedPosition);
    }

    protected override void CancelButton()
    {
        Points.Add(Points.First());
        ApplyShape();
    }

    protected override void MouseMove(Point<double> position, bool buttonDown)
    {
    }
}
