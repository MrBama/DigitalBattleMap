using DigitalBattleMap.DataClasses;
using System;
using System.Drawing;
using System.Windows.Input;

namespace DigitalBattleMap.Interfaces;

public interface IMouseCanvas
{
    public event EventHandler<MouseButtonDataEventArgs> OnLeftButtonDown;
    public event EventHandler<MouseButtonDataEventArgs> OnLeftButtonUp;
    public event EventHandler<MouseButtonDataEventArgs> OnRightButtonDown;
    public event EventHandler<MouseButtonDataEventArgs> OnRightButtonUp;
    public event EventHandler<MouseMoveDataEventArgs> OnMouseMove;
    public event EventHandler<RectangleF> OnRectangleAreaSelected;
    public event EventHandler<Polygon> OnPolygonAreaSelected;

    public Cursor Cursor { get; set; }

    public void SetMode(MouseCanvasMode mode);
    public void ResetSelection();
}
