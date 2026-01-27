using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DigitalBattleMap.FogShapes;

public class PolygonFogShape : FogShape
{
    public PolygonFogShape(Action applyShapeCallback, IMapSize mapSize) : base(applyShapeCallback, mapSize)
    {
    }

    public override bool IsErasable => true;

    public override FogShape Clone()
    {
        return new PolygonFogShape(_applyShapeCallback, _mapSize) { SnapToGrid = SnapToGrid };
    }

    protected override void ButtonDown(Point<double> position)
    {
        var snappedPosition = SnapToGrid
            ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize)
            : position;
        Points.Add(snappedPosition);
    }

    protected override void ButtonUp(Point<double> position)
    {
        Points.Add(Points.First());
        ApplyShape();
    }

    protected override void MouseMove(Point<double> position, bool buttonDown)
    {
        if (buttonDown)
        {
            var snappedPosition = SnapToGrid
                ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize)
                : position;

            if (!Points.Contains(snappedPosition))
            {
                Points.Add(snappedPosition);
            }
        }
    }
}
