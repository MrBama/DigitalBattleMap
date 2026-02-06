using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBattleMap.FogShapes;

internal class NGonFogShape : FogShape
{
    private Point<double> _startPosition;
    private Point<double> _previousMovePosition;
    private NType currentType;

    public NGonFogShape(Action applyShapeCallback, IMapSize mapSize, bool isFogEnabled = true, NType type = NType.Triangle) : base(applyShapeCallback, mapSize)
    {
        currentType = type;
        ShapeType = currentType.ToString() + " Fog";
        SnapToGrid = true;
        IsFogEnabled = isFogEnabled;
    }

    public override FogShape Clone()
    {
        var shape = new NGonFogShape(_applyShapeCallback, _mapSize, IsFogEnabled, currentType) { SnapToGrid = SnapToGrid };
        shape.OnControlUpdated += NotifyControlUpdated;
        return shape;
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
        if (buttonDown)
        {
            var snappedPosition = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2) : position;
            if (snappedPosition != _previousMovePosition)
            {
                _previousMovePosition = snappedPosition;
                UpdateShapePoints(snappedPosition);
            }
        }
    }

    private void UpdateShapePoints(Point<double> snappedPosition)
    {
        Points.Clear();
        var startOfCircle = new Point<double>(snappedPosition);
        var stepSize = 360 / (((int)currentType) + 3);
        for (int i = 0; i <= 360; i += stepSize)
        {
            Points.Add(startOfCircle.Rotate(_startPosition, i));
        }

        Points.Insert(0, _startPosition); // Draw middle
    }

    protected override void MouseWheel(Point<double> position, int mouseDelta)
    {
        if (mouseDelta < 0)
        {
            SetLowerNType();
        }
        else if (mouseDelta > 0)
        {
            SetHigherNType();
        }
        UpdateControls();
        UpdateShapePoints(_previousMovePosition);
    }

    private void SetLowerNType()
    {
        if(currentType != NType.Triangle)
        {
            currentType = currentType - 1;
        }
    }

    private void SetHigherNType()
    {
        if (currentType != NType.Octagon)
        {
            currentType = currentType + 1;
        }
    }

    public override void UpdateControls()
    {
        var infoBlock1 = new InfoBlock(ControlType.LMB, ControlType.Down, "Start drawing the " + currentType.ToString() + " from the center of the start position");
        var infoBlock2 = new InfoBlock(ControlType.LMB, ControlType.Up, "Complete drawing the " + currentType.ToString());
        var infoBlock3 = new InfoBlock(ControlType.Scroll, "Scroll down to create the Triangle shape, scroll up the increase in edges up to Octagon shape");
        NotifyControlUpdated(currentType.ToString() + " drawing", new List<InfoBlock> { infoBlock1, infoBlock2, infoBlock3 });
    }


}

internal enum NType
{
    Triangle,
    Tetragon,
    Pentagon,
    Hexagon,
    Heptagon,
    Octagon
}
