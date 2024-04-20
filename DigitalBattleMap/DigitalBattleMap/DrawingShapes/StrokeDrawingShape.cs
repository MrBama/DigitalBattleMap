using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DigitalBattleMap.DrawingShapes;

public class StrokeDrawingShape : DrawingShape
{
    public StrokeDrawingShape(Action applyShapeCallback, ITokenLinker tokenLinker, ICanvasSize canvasSize, int gridSize) : base(applyShapeCallback, tokenLinker, canvasSize, gridSize)
    {
    }

    public override bool IsErasable => true;

    protected override void ButtonDown(Point<double> position)
    {
        Points.Add(position);
    }

    protected override void ButtonUp(Point<double> position)
    {
        if (SnapToGrid)
        {
            var snappedPoints = Mathematics.SnapPointsToCanvasGrid(Points.ToList(), _canvasSize, _gridSize);
            Points = new ObservableCollection<Point<double>>(snappedPoints);
        }
        ApplyShape();
    }

    protected override void MouseMove(Point<double> position, bool buttonDown)
    {
        if (buttonDown)
        {
            if (!Points.Contains(position))
            {
                Points.Add(position);
            }
        }
    }    
}
