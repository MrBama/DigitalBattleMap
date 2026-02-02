using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Linq;

namespace DigitalBattleMap.FogShapes;

public class DrawPolygonFogShape : FogShape
{
    public DrawPolygonFogShape(Action applyShapeCallback, IMapSize mapSize, bool isFogEnable = true) : base(applyShapeCallback, mapSize)
    {
        ShapeType = "Drawn Fog";
        IsFogEnabled = isFogEnable;
    }

    public override FogShape Clone()
    {
        return new DrawPolygonFogShape(_applyShapeCallback, _mapSize) { SnapToGrid = SnapToGrid, IsFogEnabled = IsFogEnabled };
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
