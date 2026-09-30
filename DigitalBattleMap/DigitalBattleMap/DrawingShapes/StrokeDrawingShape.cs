using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Linq;

namespace DigitalBattleMap.DrawingShapes;

public class StrokeDrawingShape : DrawingShape
{
    public StrokeDrawingShape(Action applyShapeCallback, ITokenLinker tokenLinker, IMapSize mapSize) : base(applyShapeCallback, tokenLinker, mapSize)
    {
    }

    public override bool ShowInShapesOverview => false;

    protected override void ButtonDown(Point<double> position)
    {
        var point = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize) : position;
        Points.Add(point);
    }

    protected override void ButtonUp(Point<double> position)
    {
        ApplyShape();
    }

    protected override void MouseMove(Point<double> position, bool buttonDown)
    {
        if (buttonDown)
        {
            if (SnapToGrid)
            {
                var snappedPoint = Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize);

                if (!Points.Last().Equals(snappedPoint))
                {
                    // Check if the point equals the point before the last one.
                    // If this is the case, then mouse went back and the last point can be removed.
                    if (Points.Count > 1 && Points.ElementAt(Points.Count - 2).Equals(snappedPoint))
                    {
                        Points.RemoveAt(Points.Count - 1);
                    }
                    else
                    {
                        // Sometimes the mouse positions are too far apart (when moving the mouse fast) and we get a diagonal line.
                        // In this case another point is inserted to avoid the diagonal line.
                        // -----        P1---       P1---
                        // |   |    ->  |   |   ->  |   |
                        // -----        -----       ---P2
                        if(!Points.Last().X.Equals(snappedPoint.X) && !Points.Last().Y.Equals(snappedPoint.Y))
                        {
                            Points.Add(new Point<double>(snappedPoint.X, Points.Last().Y));
                        }

                        Points.Add(snappedPoint);
                    }
                }
            }
            else
            {
                if (!Points.Contains(position))
                {
                    Points.Add(position);
                }
            }
        }
    }
}
