using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;

namespace DigitalBattleMap.FogShapes;

internal class NGonFogShape : FogShape
{
    private Point<double> _startPosition;
    private Point<double> _previousMovePosition;
    private bool _buttonDown;

    public NGonFogShape(Action applyShapeCallback, IMapSize mapSize, bool isFogEnabled, bool isSnapToGridEnabled) : base(applyShapeCallback, mapSize)
    {
        SnapToGrid = isSnapToGridEnabled;
        IsFogEnabled = isFogEnabled;
    }

    public override FogShape Clone()
    {
        var shape = new NGonFogShape(_applyShapeCallback, _mapSize, IsFogEnabled, SnapToGrid) { CurrentType = CurrentType};
        shape.OnControlUpdated += NotifyControlUpdated;
        return shape;
    }

    protected override void ButtonDown(Point<double> position)
    {
        IsDrawingFog = true;
        _startPosition = SnapToGrid ? Mathematics.SnapPointToCanvasGrid(position, _mapSize, _mapSize.CanvasGridSize / 2) : position;
        _buttonDown = true;
        Points.Add(position);
    }

    protected override void ButtonUp(Point<double> position)
    {
        Points.RemoveAt(0); // Remove middle
        ShapeType = CurrentType.ToString() + " Fog";
        _buttonDown = false;
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
        var stepSize = 360 / (((int)CurrentType) + 3);
        for (int i = 0; i <= 360; i += stepSize)
        {
            Points.Add(startOfCircle.Rotate(_startPosition, i));
        }

        Points.Insert(0, _startPosition); // Draw middle
    }

    protected override void MouseWheel(Point<double> position, int mouseDelta)
    {
        if (!_buttonDown)
        {
            return;
        }

        if (mouseDelta < 0)
        {
            SetLowerNType();
        }
        else if (mouseDelta > 0)
        {
            SetHigherNType();
        }
        ShapeType = CurrentType.ToString() + " Fog";
        UpdateControls();
        UpdateShapePoints(_previousMovePosition);
    }

    private void SetLowerNType()
    {
        if(CurrentType != NType.Triangle)
        {
            CurrentType = CurrentType - 1;
        }
    }

    private void SetHigherNType()
    {
        if (CurrentType != NType.Octagon)
        {
            CurrentType = CurrentType + 1;
        }
    }

    public override void UpdateControls()
    {
        var infoBlock1 = new InfoBlock(ControlType.LMB, ControlType.Down, "Start drawing the " + CurrentType.ToString() + " from the center of the start position");
        var infoBlock2 = new InfoBlock(ControlType.LMB, ControlType.Up, "Complete drawing the " + CurrentType.ToString());
        var infoBlock3 = new InfoBlock(ControlType.Scroll, "Scroll down to create the Triangle shape, scroll up the increase in edges up to Octagon shape");
        NotifyControlUpdated(CurrentType.ToString() + " drawing", new List<InfoBlock> { infoBlock1, infoBlock2, infoBlock3 });
    }
}
