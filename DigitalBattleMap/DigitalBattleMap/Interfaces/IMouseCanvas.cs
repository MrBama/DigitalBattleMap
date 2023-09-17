using DigitalBattleMap.DataClasses;
using System;
using System.Drawing;

namespace DigitalBattleMap.Interfaces;

public interface IMouseCanvas
{
    public void SubscribeMouseDown(int tabIndex, Action<Point<double>> action);
    public void SubscribeMouseUp(int tabIndex, Action<Point<double>> action);
    public void SubscribeRectangleAreaSelected(int tabIndex, Action<RectangleF> action);
    public void SubscribePolygonAreaSelected(int tabIndex, Action<Polygon> action);
    public void SetMode(MouseCanvasMode mode);
    public void ResetSelection();
}
