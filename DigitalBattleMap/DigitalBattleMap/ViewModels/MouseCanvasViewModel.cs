using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace DigitalBattleMap.ViewModels;

public class MouseCanvasViewModel : ViewModelBase
{
    private MouseCanvasMode _mode;
    private MouseCanvasMode _previousMode;
    private Point<double> _selectionStartPosition = new();
    MouseButtonState _previousLeftButtonState;
    MouseButtonState _previousRightButtonState;

    public MouseCanvasViewModel()
    {
        Cursor = Cursors.Hand;
        PolygonSelectionPoints = new();
    }

    protected override void InitializeCommands()
    {
        LeftButtonDownCommand = new RelayCommand(p => LeftButtonDown((MouseDataEventArgs)p));
        LeftButtonUpCommand = new RelayCommand(p => LeftButtonUp((MouseDataEventArgs)p));
        RightButtonDownCommand = new RelayCommand(p => RightButtonDown((MouseDataEventArgs)p));
        RightButtonUpCommand = new RelayCommand(p => RightButtonUp((MouseDataEventArgs)p));
        MouseMoveCommand = new RelayCommand(p => Move((MouseDataEventArgs)p));
        MouseWheelCommand = new RelayCommand(p => MouseWheel((MouseWheelDataEventArgs)p));
    }

    public event EventHandler<MouseButtonDataEventArgs> OnLeftButtonDown;
    public event EventHandler<MouseButtonDataEventArgs> OnLeftButtonUp;
    public event EventHandler<MouseButtonDataEventArgs> OnRightButtonDown;
    public event EventHandler<MouseButtonDataEventArgs> OnRightButtonUp;
    public event EventHandler<MouseMoveDataEventArgs> OnMouseMove;
    public event EventHandler<MouseWheelDataEventArgs> OnMouseWheel;
    public event EventHandler<RectangleF> OnRectangleAreaSelected;
    public event EventHandler<RectangleF> OnFixRatioRectangleAreaSelected;
    public event EventHandler<Polygon> OnPolygonAreaSelected;

    public double SelectionWidth { get => Get<double>(); set => Set(value); }
    public double SelectionHeight { get => Get<double>(); set => Set(value); }
    public double SelectionX { get => Get<double>(); set => Set(value); }
    public double SelectionY { get => Get<double>(); set => Set(value); }
    public bool IsSelectionAreaVisible { get => Get<bool>(); set => Set(value); }
    public bool IsSelectionStarted { get => Get<bool>(); set => Set(value); }
    public PointCollection PolygonSelectionPoints { get => Get<PointCollection>(); set => Set(value); }
    public Cursor Cursor { get => Get<Cursor>(); set => Set(value); }

    public ICommand LeftButtonDownCommand { get; set; }
    public ICommand LeftButtonUpCommand { get; set; }
    public ICommand MouseMoveCommand { get; set; }
    public ICommand RightButtonDownCommand { get; set; }
    public ICommand RightButtonUpCommand { get; set; }
    public ICommand MouseWheelCommand { get; set; }

    public MouseCanvasMode GetMode()
    {
        return _mode;
    }

    public void SetMode(MouseCanvasMode mode)
    {
        _previousMode = _mode;
        _mode = mode;
        ResetMouseCanvas();

        switch (mode)
        {
            case MouseCanvasMode.Click:
                break;
            case MouseCanvasMode.RectangleSelection:
                IsSelectionAreaVisible = true;
                break;
            case MouseCanvasMode.PolygonSelection:
                IsSelectionAreaVisible = true;
                break;
            case MouseCanvasMode.FixedRatioRectangleSelection:
                Cursor = Cursors.SizeAll;
                IsSelectionAreaVisible = true;
                break;
        }
    }

    public void ResetSelection()
    {
        SetMode(MouseCanvasMode.Click);
    }

    private void ResetMouseCanvas()
    {
        SelectionWidth = 0;
        SelectionHeight = 0;
        IsSelectionStarted = false;
        PolygonSelectionPoints = new();
        Cursor = Cursors.Hand;
    }

    private void LeftButtonDown(MouseDataEventArgs e)
    {
        switch (_mode)
        {
            case MouseCanvasMode.Click:
                LeftButtonDownClick(e);
                return;
            case MouseCanvasMode.RectangleSelection:
            case MouseCanvasMode.FixedRatioRectangleSelection:
                MouseDownRectangleSelection(e);
                return;
            case MouseCanvasMode.PolygonSelection:
                MouseDownPolygonSelection(e);
                return;
        }
    }

    private void LeftButtonUp(MouseDataEventArgs e)
    {
        switch (_mode)
        {
            case MouseCanvasMode.Click:
                LeftButtonUpClick(e);
                return;
            case MouseCanvasMode.RectangleSelection:
                MouseUpRectangleSelection(OnRectangleAreaSelected);
                return;
            case MouseCanvasMode.PolygonSelection:
                MouseUpPolygonSelection();
                return;
            case MouseCanvasMode.FixedRatioRectangleSelection:
                MouseUpRectangleSelection(OnFixRatioRectangleAreaSelected);
                return;
        }
    }

    private void RightButtonDown(MouseDataEventArgs e)
    {
        switch (_mode)
        {
            case MouseCanvasMode.Click:
                RightButtonDownClick(e);
                return;
            case MouseCanvasMode.RectangleSelection:
                return;
            case MouseCanvasMode.PolygonSelection:
                return;
        }
    }

    private void RightButtonUp(MouseDataEventArgs e)
    {
        switch (_mode)
        {
            case MouseCanvasMode.Click:
                RightButtonUpClick(e);
                return;
            case MouseCanvasMode.RectangleSelection:
                return;
            case MouseCanvasMode.PolygonSelection:
                return;
        }
    }

    private void Move(MouseDataEventArgs e)
    {
        switch (_mode)
        {
            case MouseCanvasMode.Click:
                MouseMoveClick(e);
                return;
            case MouseCanvasMode.RectangleSelection:
                MouseMoveRectangleSelection(e, false);
                return;
            case MouseCanvasMode.PolygonSelection:
                MouseMovePolygonSelection(e);
                return;
            case MouseCanvasMode.FixedRatioRectangleSelection:
                MouseMoveRectangleSelection(e, true);
                return;
        }
    }

    private void MouseWheel(MouseWheelDataEventArgs e)
    {
        OnMouseWheel?.Invoke(this, e);
    }

    private void MouseDownRectangleSelection(MouseDataEventArgs e)
    {
        var point = e.Position;
        _selectionStartPosition = point;
        IsSelectionStarted = true;
        SelectionX = point.X;
        SelectionY = point.Y;
    }

    private void MouseMoveRectangleSelection(MouseDataEventArgs e, bool fixedRatio)
    {

        if (IsSelectionStarted)
        {
            var point = e.Position;

            SelectionX = Math.Min(_selectionStartPosition.X, point.X);
            SelectionY = Math.Min(_selectionStartPosition.Y, point.Y);

            SelectionWidth = Math.Max(_selectionStartPosition.X, point.X) - SelectionX;
            SelectionHeight = Math.Max(_selectionStartPosition.Y, point.Y) - SelectionY;

            var minSize = 20;
            SelectionWidth = Math.Max(SelectionWidth, minSize);
            SelectionHeight = Math.Max(SelectionHeight, minSize);

            if (fixedRatio)
            {
                var widthRatio = 0.5625;
                var heightRatio = 1.77777777778;
                if (SelectionHeight * widthRatio <= SelectionWidth)
                {
                    SelectionWidth = SelectionHeight / widthRatio;
                }
                else if (SelectionWidth * heightRatio <= SelectionHeight)
                {
                    SelectionHeight = SelectionWidth / heightRatio;
                }

                if (SelectionX < _selectionStartPosition.X)
                {
                    SelectionX = _selectionStartPosition.X - SelectionWidth;
                }
                if (SelectionY < _selectionStartPosition.Y)
                {
                    SelectionY = _selectionStartPosition.Y - SelectionHeight;
                }
            }
        }
    }

    private void MouseUpRectangleSelection(EventHandler<RectangleF> handler)
    {
        IsSelectionStarted = false;
        var rectangle = new RectangleF((float)SelectionX, (float)SelectionY, (float)SelectionWidth, (float)SelectionHeight);
        handler?.Invoke(this, rectangle);
    }

    private void MouseDownPolygonSelection(MouseDataEventArgs e)
    {
        IsSelectionStarted = true;
        var point = e.Position;
        var newPoints = new PointCollection(PolygonSelectionPoints)
        {
            new System.Windows.Point(point.X, point.Y)
        };
        PolygonSelectionPoints = newPoints;
    }

    private void MouseMovePolygonSelection(MouseDataEventArgs e)
    {
        if (IsSelectionStarted)
        {
            var point = e.Position;
            var newPoints = new PointCollection(PolygonSelectionPoints)
            {
                new System.Windows.Point(point.X, point.Y)
            };
            PolygonSelectionPoints = newPoints;
        }
    }

    private void MouseUpPolygonSelection()
    {
        IsSelectionStarted = false;
        if (PolygonSelectionPoints.Count >= 3)
        {
            var polygon = new Polygon
            {
                Points = PolygonSelectionPoints.Select(p => new Point<double>(p.X, p.Y)).ToList()
            };

            OnPolygonAreaSelected?.Invoke(this, polygon);
        }
    }

    private void LeftButtonDownClick(MouseDataEventArgs e)
    {
        if (_previousLeftButtonState == MouseButtonState.Released)
        {
            _previousLeftButtonState = MouseButtonState.Pressed;
            OnLeftButtonDown?.Invoke(this, new MouseButtonDataEventArgs { Position = e.Position });
        }
    }

    private void LeftButtonUpClick(MouseDataEventArgs e)
    {
        if (_previousLeftButtonState == MouseButtonState.Pressed)
        {
            _previousLeftButtonState = MouseButtonState.Released;
            OnLeftButtonUp?.Invoke(this, new MouseButtonDataEventArgs { Position = e.Position });
        }
    }

    private void RightButtonDownClick(MouseDataEventArgs e)
    {
        if (_previousRightButtonState == MouseButtonState.Released)
        {
            _previousRightButtonState = MouseButtonState.Pressed;
            OnRightButtonDown?.Invoke(this, new MouseButtonDataEventArgs { Position = e.Position });
        }
    }

    private void RightButtonUpClick(MouseDataEventArgs e)
    {
        if (_previousRightButtonState == MouseButtonState.Pressed)
        {
            _previousRightButtonState = MouseButtonState.Released;
            OnRightButtonUp?.Invoke(this, new MouseButtonDataEventArgs { Position = e.Position });
        }
    }

    private void MouseMoveClick(MouseDataEventArgs e)
    {
        var leftButtonState = e.MouseEventArgs.LeftButton;
        var rightButtonState = e.MouseEventArgs.RightButton;
        if (_previousLeftButtonState != leftButtonState)
        {
            _previousLeftButtonState = leftButtonState;

            if (leftButtonState == MouseButtonState.Pressed)
            {
                OnLeftButtonDown?.Invoke(this, new MouseButtonDataEventArgs { Position = e.Position });
            }
            else
            {
                OnLeftButtonUp?.Invoke(this, new MouseButtonDataEventArgs { Position = e.Position });
            }
        }
        else if (_previousRightButtonState != rightButtonState)
        {
            _previousRightButtonState = rightButtonState;

            if (rightButtonState == MouseButtonState.Pressed)
            {
                OnRightButtonDown?.Invoke(this, new MouseButtonDataEventArgs { Position = e.Position });
            }
            else
            {
                OnRightButtonUp?.Invoke(this, new MouseButtonDataEventArgs { Position = e.Position });
            }
        }
        else
        {
            OnMouseMove?.Invoke(this, new MouseMoveDataEventArgs
            {
                Position = e.Position,
                LeftButtonDown = leftButtonState == MouseButtonState.Pressed,
                RightButtonDown = rightButtonState == MouseButtonState.Pressed
            });
        }
    }
}
