using DigitalBattleMap.DataClasses;
using System;
using System.Drawing;

namespace DigitalBattleMap.Interfaces;

public interface IMouseCanvas
{
    public void SubscribeMouseDown(int tabIndex, Action<Point<double>> action);
    public void SubscribeMouseUp(int tabIndex, Action<Point<double>> action);
    public void SubscribeAreaSelected(int tabIndex, Action<Rectangle> action);
    public void SetMode(MouseCanvasMode mode);
    public void ResetSelection();
}
