using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBattleMap.FogShapes;

public class DrawPolygonFogShape : FogShape
{
    public DrawPolygonFogShape(Action applyShapeCallback, IMapSize mapSize, bool isFogEnable, bool isSnapToGridEnabled) : base(applyShapeCallback, mapSize)
    {
        ShapeType = "Drawn Fog";
        SnapToGrid = isSnapToGridEnabled;
        IsFogEnabled = isFogEnable;
    }

    public override FogShape Clone()
    {
        var shape = new DrawPolygonFogShape(_applyShapeCallback, _mapSize, IsFogEnabled, SnapToGrid);
        shape.OnControlUpdated += NotifyControlUpdated;
        return shape;
    }

    protected override void ButtonDown(Point<double> position)
    {
        IsDrawingFog = true;
        var snappedPosition = SnapToGrid
            ? Mathematics.SnapPointToCanvasGridExact(position, _mapSize, _mapSize.GridSize)
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
                ? Mathematics.SnapPointToCanvasGridExact(position, _mapSize, _mapSize.GridSize)
                : position;

            if (!Points.Contains(snappedPosition))
            {
                Points.Add(snappedPosition);
            }
        }
    }

    public override void UpdateControls()
    {
        var infoBlock1 = new InfoBlock(ControlType.LMB, ControlType.Down, "Start free drawing the fog polygon");
        var infoBlock2 = new InfoBlock(ControlType.LMB, ControlType.Up, "Complete drawing shape, end will connect to start");
        NotifyControlUpdated("Free polygon drawing", new List<InfoBlock> { infoBlock1, infoBlock2 });
    }
}
