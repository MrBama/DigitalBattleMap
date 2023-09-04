using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class MouseCanvasViewModel : ViewModelBase, IMouseCanvas
{
    private int _selectedTabIndex;
    private Dictionary<int, List<Action<Point<double>>>> _mouseDownEvents = new();
    private Dictionary<int, List<Action<Point<double>>>> _mouseUpEvents = new();
    private MouseCanvasMode _mode;
    private Point<double> _selectionStartPosition = new();
    private bool _selectionStarted = false;

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
    public bool SelectionVisible { get => Get<bool>(); set => Set(value); }

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
        if(_mode == MouseCanvasMode.Selection)
        {
            SelectionVisible = true;
        }
        else
        {
            SelectionVisible = false;
            _selectionStarted = false;
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

    public void SubscribeAreaSelected(int tabIndex, Action<Point<double>> action)
    {
    }

    private void MouseDown()
    {
        switch (_mode)
        {
            case MouseCanvasMode.Click:
                MouseDownClick();
                return;
            case MouseCanvasMode.Selection:
                MouseDownSelection();
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
            case MouseCanvasMode.Selection:
                MouseUpSelection();
                return;
        }
    }

    private void MouseMove()
    {
        switch (_mode)
        {
            case MouseCanvasMode.Click:
                return;
            case MouseCanvasMode.Selection:
                MouseMoveSelection();
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

    private void MouseDownSelection()
    {
        var point = new Point<double>(MouseX, MouseY);
        _selectionStartPosition = point;
        _selectionStarted = true;
        SelectionX = point.X;
        SelectionY = point.Y;
    }

    private void MouseMoveSelection()
    {
        var point = new Point<double>(MouseX, MouseY);
        if (_selectionStarted)
        {
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
            // Add clipping
        }
    }

    private void MouseUpSelection()
    {
        _selectionStarted = false;
    }
}
