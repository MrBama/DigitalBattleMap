using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class MouseCanvasViewModel : ViewModelBase, IMouseCanvas
{
    private int _selectedTabIndex;
    private Dictionary<int, List<Action<Point<double>>>> _mouseDownEvents = new();
    private Dictionary<int, List<Action<Point<double>>>> _mouseUpEvents = new();
    private Dictionary<int, List<Action<RectangleF>>> _rectangleAreaSelectedEvents = new();
    private Dictionary<int, List<Action<Polygon>>> _polygonAreaSelectedEvents = new();
    private MouseCanvasMode _mode;
    private Point<double> _selectionStartPosition = new();

    public MouseCanvasViewModel()
    {
        PolygonSelectionPoints = new();
    }

    protected override void InitializeCommands()
    {
        MouseDownCommand = new RelayCommand(p => MouseDown());
        MouseUpCommand = new RelayCommand(p => MouseUp());
        MouseMoveCommand = new RelayCommand(p => MouseMove());
    }

    public double MouseX { get; set; }
    public double MouseY { get; set; }
    public double SelectionWidth { get => Get<double>(); set => Set(value); }
    public double SelectionHeight { get => Get<double>(); set => Set(value); }
    public double SelectionX { get => Get<double>(); set => Set(value); }
    public double SelectionY { get => Get<double>(); set => Set(value); }
    public bool IsSelectionAreaVisible { get => Get<bool>(); set => Set(value); }
    public bool IsRectangleSelectionStarted { get => Get<bool>(); set => Set(value); }
    public System.Windows.Media.PointCollection PolygonSelectionPoints { get => Get<System.Windows.Media.PointCollection>(); set => Set(value); }

    public ICommand MouseDownCommand { get; set; }
    public ICommand MouseUpCommand { get; set; }
    public ICommand MouseMoveCommand { get; set; }

    public void SetSelectedTabIndex(int tabIndex)
    {
        _selectedTabIndex = tabIndex;
    }

    public void SetMode(MouseCanvasMode mode)
    {
        _mode = mode;

        switch (mode)
        {
            case MouseCanvasMode.Click:
                ResetSelection();
                break;
            case MouseCanvasMode.RectangleSelection:
                IsSelectionAreaVisible = true;
                break;
            case MouseCanvasMode.PolygonSelection:
                IsSelectionAreaVisible = true;
                IsRectangleSelectionStarted = false;
                break;
        }
    }

    public void SubscribeMouseDown(int tabIndex, Action<Point<double>> action)
    {
        if (!_mouseDownEvents.ContainsKey(tabIndex))
        {
            _mouseDownEvents[tabIndex] = new();
        }

        _mouseDownEvents[tabIndex].Add(action);
    }

    public void SubscribeMouseUp(int tabIndex, Action<Point<double>> action)
    {
        if (!_mouseUpEvents.ContainsKey(tabIndex))
        {
            _mouseUpEvents[tabIndex] = new();
        }

        _mouseUpEvents[tabIndex].Add(action);
    }

    public void SubscribeRectangleAreaSelected(int tabIndex, Action<RectangleF> action)
    {
        if (!_rectangleAreaSelectedEvents.ContainsKey(tabIndex))
        {
            _rectangleAreaSelectedEvents[tabIndex] = new();
        }

        _rectangleAreaSelectedEvents[tabIndex].Add(action);
    }

    public void SubscribePolygonAreaSelected(int tabIndex, Action<Polygon> action)
    {
        if (!_polygonAreaSelectedEvents.ContainsKey(tabIndex))
        {
            _polygonAreaSelectedEvents[tabIndex] = new();
        }

        _polygonAreaSelectedEvents[tabIndex].Add(action);
    }

    public void ResetSelection()
    {
        SelectionWidth = 0;
        SelectionHeight = 0;
        IsRectangleSelectionStarted = false;
        PolygonSelectionPoints = new();
    }

    private void MouseDown()
    {
        switch (_mode)
        {
            case MouseCanvasMode.Click:
                MouseDownClick();
                return;
            case MouseCanvasMode.RectangleSelection:
                MouseDownRectangleSelection();
                return;
            case MouseCanvasMode.PolygonSelection:
                return;
        }
    }

    private void MouseUp()
    {
        switch (_mode)
        {
            case MouseCanvasMode.Click:
                MouseUpClick();
                return;
            case MouseCanvasMode.RectangleSelection:
                MouseUpRectangleSelection();
                return;
            case MouseCanvasMode.PolygonSelection:
                MouseUpPolygonSelection();
                return;
        }
    }

    private void MouseMove()
    {
        switch (_mode)
        {
            case MouseCanvasMode.Click:
                return;
            case MouseCanvasMode.RectangleSelection:
                MouseMoveRectangleSelection();
                return;
            case MouseCanvasMode.PolygonSelection:
                return;
        }
    }

    private void MouseDownClick()
    {
        var point = new Point<double>(MouseX, MouseY);
        if (_mouseDownEvents.ContainsKey(_selectedTabIndex))
        {
            foreach (var action in _mouseDownEvents[_selectedTabIndex])
            {
                action(point);
            }
        }
    }

    private void MouseUpClick()
    {
        var point = new Point<double>(MouseX, MouseY);
        if (_mouseUpEvents.ContainsKey(_selectedTabIndex))
        {
            foreach (var action in _mouseUpEvents[_selectedTabIndex])
            {
                action(point);
            }
        }
    }

    private void MouseDownRectangleSelection()
    {
        var point = new Point<double>(MouseX, MouseY);
        _selectionStartPosition = point;
        IsRectangleSelectionStarted = true;
        SelectionX = point.X;
        SelectionY = point.Y;
    }

    private void MouseMoveRectangleSelection()
    {
        if (IsRectangleSelectionStarted)
        {
            var point = new Point<double>(MouseX, MouseY);

            var distanceX = point.X - _selectionStartPosition.X;
            var distanceY = point.Y - _selectionStartPosition.Y;

            SelectionWidth = distanceX;
            SelectionHeight = distanceY;

            if (point.X < _selectionStartPosition.X)
            {
                SelectionX = point.X;
                SelectionWidth *= -1;
            }

            if (point.Y < _selectionStartPosition.Y)
            {
                SelectionY = point.Y;
                SelectionHeight *= -1;
            }
        }
    }

    private void MouseUpRectangleSelection()
    {
        IsRectangleSelectionStarted = false;
        var area = new RectangleF((float)SelectionX, (float)SelectionY, (float)SelectionWidth, (float)SelectionHeight);

        if (_rectangleAreaSelectedEvents.ContainsKey(_selectedTabIndex))
        {
            foreach (var action in _rectangleAreaSelectedEvents[_selectedTabIndex])
            {
                action(area);
            }
        }
    }

    private void MouseUpPolygonSelection()
    {
        var point = new Point<double>(MouseX, MouseY);
        var newPoints = new System.Windows.Media.PointCollection(PolygonSelectionPoints)
        {
            new System.Windows.Point(point.X, point.Y)
        };
        PolygonSelectionPoints = newPoints;

        if(PolygonSelectionPoints.Count >= 3)
        {
            var polygon = new Polygon
            {
                Points = PolygonSelectionPoints.Select(p => new Point<double>(p.X, p.Y)).ToList()
            };

            if (_polygonAreaSelectedEvents.ContainsKey(_selectedTabIndex))
            {
                foreach (var action in _polygonAreaSelectedEvents[_selectedTabIndex])
                {
                    action(polygon);
                }
            }
        }
    }
}
