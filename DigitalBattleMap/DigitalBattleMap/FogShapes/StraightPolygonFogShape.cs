using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBattleMap.FogShapes;

public class StraightPolygonFogShape : FogShape
{
    private Stack<Point<double>> previousMoves;
    public StraightPolygonFogShape(Action applyShapeCallback, IMapSize mapSize) : base(applyShapeCallback, mapSize)
    {
        IsFogEnabled = true;
        SnapToGrid = true;
        previousMoves = new Stack<Point<double>>();
    }

    public override FogShape Clone()
    {
        return new StraightPolygonFogShape(_applyShapeCallback, _mapSize) { SnapToGrid = SnapToGrid, IsFogEnabled = IsFogEnabled };
    }

    protected override void ButtonUp(Point<double> position)
    {
        previousMoves.Clear(); // new history from latest point.
        var snappedPosition = SnapToGrid
            ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2)
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
        // does nothing
    }

    protected override void MouseMove(Point<double> position, bool mouseDelta)
    {
        // does nothing
    }
}
