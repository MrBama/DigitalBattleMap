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
    private Dictionary<int, List<Action<Point<double>>>> _leftButtonDownEvents = new();
    private Dictionary<int, List<Action<Point<double>>>> _leftButtonUpEvents = new();
    private Dictionary<int, List<Action<Point<double>>>> _rightButtonDownEvents = new();
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
        LeftButtonDownCommand = new RelayCommand(p => LeftButtonDown());
        LeftButtonUpCommand = new RelayCommand(p => LeftButtonUp());
        LeftButtonMoveCommand = new RelayCommand(p => LeftButtonMove());
        RightButtonDownCommand = new RelayCommand(p => RightButtonDown());
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

    public ICommand LeftButtonDownCommand { get; set; }
    public ICommand LeftButtonUpCommand { get; set; }
    public ICommand LeftButtonMoveCommand { get; set; }
    public ICommand RightButtonDownCommand { get; set; }

    public void SetSelectedTabIndex(int tabIndex)
    {
        _selectedTabIndex = tabIndex;
    }

    public void SetMode(MouseCanvasMode mode)
    {
        _mode = mode;
        ResetSelection();

        switch (mode)
        {
            case MouseCanvasMode.Click:
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

    public void SubscribeLeftButtonDown(int tabIndex, Action<Point<double>> action)
    {
        if (!_leftButtonDownEvents.ContainsKey(tabIndex))
        {
            _leftButtonDownEvents[tabIndex] = new();
        }

        _leftButtonDownEvents[tabIndex].Add(action);
    }

    public void SubscribeLeftButtonUp(int tabIndex, Action<Point<double>> action)
    {
        if (!_leftButtonUpEvents.ContainsKey(tabIndex))
        {
            _leftButtonUpEvents[tabIndex] = new();
        }

        _leftButtonUpEvents[tabIndex].Add(action);
    }

    public void SubscribeRightButtonDown(int tabIndex, Action<Point<double>> action)
    {
        if (!_rightButtonDownEvents.ContainsKey(tabIndex))
        {
            _rightButtonDownEvents[tabIndex] = new();
        }

        _rightButtonDownEvents[tabIndex].Add(action);
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

    private void LeftButtonDown()
    {
        switch (_mode)
        {
            case MouseCanvasMode.Click:
                LeftButtonDownClick();
                return;
            case MouseCanvasMode.RectangleSelection:
                MouseDownRectangleSelection();
                return;
            case MouseCanvasMode.PolygonSelection:
                return;
        }
    }

    private void LeftButtonUp()
    {
        switch (_mode)
        {
            case MouseCanvasMode.Click:
                LeftButtonUpClick();
                return;
            case MouseCanvasMode.RectangleSelection:
                MouseUpRectangleSelection();
                return;
            case MouseCanvasMode.PolygonSelection:
                MouseUpPolygonSelection();
                return;
        }
    }

    private void LeftButtonMove()
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

    private void LeftButtonDownClick()
    {
        var point = new Point<double>(MouseX, MouseY);
        if (_leftButtonDownEvents.ContainsKey(_selectedTabIndex))
        {
            foreach (var action in _leftButtonDownEvents[_selectedTabIndex])
            {
                action(point);
            }
        }
    }

    private void LeftButtonUpClick()
    {
        var point = new Point<double>(MouseX, MouseY);
        if (_leftButtonUpEvents.ContainsKey(_selectedTabIndex))
        {
            foreach (var action in _leftButtonUpEvents[_selectedTabIndex])
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

    private void RightButtonDown()
    {
        var point = new Point<double>(MouseX, MouseY);
        if (_rightButtonDownEvents.ContainsKey(_selectedTabIndex))
        {
            foreach (var action in _rightButtonDownEvents[_selectedTabIndex])
            {
                action(point);
            }
        }
    }
}
