using DigitalBattleMap.DataClasses;
using System;
using System.Drawing;

namespace DigitalBattleMap.Interfaces;

public interface IMouseCanvas
{
    public void SubscribeLeftButtonDown(int tabIndex, Action<MouseDataEventArgs> action);
    public void SubscribeLeftButtonUp(int tabIndex, Action<MouseDataEventArgs> action);
    public void SubscribeRightButtonDown(int tabIndex, Action<MouseDataEventArgs> action);
    public void SubscribeMouseMove(int tabIndex, Action<MouseDataEventArgs> action);
    public void SubscribeRectangleAreaSelected(int tabIndex, Action<RectangleF> action);
    public void SubscribePolygonAreaSelected(int tabIndex, Action<Polygon> action);
    public void SetMode(MouseCanvasMode mode);
    public void ResetSelection();
}
