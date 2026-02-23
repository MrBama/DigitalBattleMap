using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBattleMap.FogShapes;

public class AngularPolygonFogShape : FogShape
{
    private Stack<Point<double>> previousMoves;
    
    public AngularPolygonFogShape(Action applyShapeCallback, IMapSize mapSize, bool isFogEnable, bool isSnapToGridEnabled) : base(applyShapeCallback, mapSize)
    {
        ShapeType = "Angular Polygon";
        SnapToGrid = isSnapToGridEnabled;
        previousMoves = new Stack<Point<double>>();
        IsFogEnabled = isFogEnable;
    }

    public override FogShape Clone()
    {
        var shape = new AngularPolygonFogShape(_applyShapeCallback, _mapSize, IsFogEnabled, SnapToGrid);
        shape.OnControlUpdated += NotifyControlUpdated;
        return shape;
    }

    protected override void ButtonUp(Point<double> position)
    {
        previousMoves.Clear(); // new history from latest point.
        var snappedPosition = SnapToGrid
            ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2)
            : position;

        // start equals end, early exit
        if (SnapToGrid && Points.Any() && snappedPosition.Equals(Points.First()))
        {
            Points.Add(Points.First());
            ApplyShape();
        }

        Points.Add(snappedPosition);
    }

    protected override void CancelButton()
    {
        if (!Points.Any())
        {
            return;
        }
        Points.Add(Points.First());
        ApplyShape();
    }

    protected override void MouseWheel(Point<double> position, int mouseDelta)
    {
        if (!Points.Any()) {
            return; // no points, no history
        }

        if (mouseDelta < 0)
        {
            var lastPoint = Points.Last();
            previousMoves.Push(lastPoint);
            Points.RemoveAt(Points.Count - 1);
        } 
        else if (mouseDelta > 0 && previousMoves.Any())
        {
            var nextMove = previousMoves.Pop();
            Points.Add(nextMove);
        }  
    }

    protected override void ButtonDown(Point<double> position)
    {
        IsDrawingFog = true;
        var infoBlock1 = new InfoBlock("Placing a point at the start point to auto complete the fog shape");
        var infoBlock2 = new InfoBlock(ControlType.LMB, "Adds points for line segments");
        var infoBlock3 = new InfoBlock(ControlType.RMB, "Completes drawing, connects last point to first");
        var infoBlock4 = new InfoBlock(ControlType.Scroll, ControlType.Down, "Revert last added points");
        var infoBlock5 = new InfoBlock(ControlType.Scroll, ControlType.Up, "Redo last reverted point");
        NotifyControlUpdated("Angular polygon drawing", new List<InfoBlock> { infoBlock1, infoBlock2, infoBlock3, infoBlock4, infoBlock5 });
    }

    public override void UpdateControls()
    {
        var infoBlock1 = new InfoBlock(ControlType.LMB, ControlType.Click, "Start drawing angular polygon");
        NotifyControlUpdated("Angular polygon drawing", new List<InfoBlock> { infoBlock1 });
    }

    protected override void MouseMove(Point<double> position, bool mouseDelta)
    {
        // does nothing
    }
}
