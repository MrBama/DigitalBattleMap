using DrawingCanvas.HelperClasses;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawingCanvas;
public abstract class DrawingShape : PropertyHandler
{
    private Action _applyShapeCallback;
    MouseButtonState _previousMouseButtonState;
    private DrawingShapeInfo _editInfo;

    public DrawingShape(Action applyShapeCallback)
    {
        _applyShapeCallback = applyShapeCallback;

        Color = Brushes.Black;
        Size = 15;
        Points = new();

        LeftButtonDownCommand = new RelayCommand(p => LeftButtonDown((MouseDataEventArgs)p));
        LeftButtonUpCommand = new RelayCommand(p => LeftButtonUp((MouseDataEventArgs)p));
        MouseMoveCommand = new RelayCommand(p => MouseMove((MouseDataEventArgs)p));
    }

    public event NotifyCollectionChangedEventHandler OnPointsChanged;

    public Brush Color { get => Get<Brush>(); set => Set(value, () => NotifyPropertyChange(nameof(Cursor))); }
    public int Size { get => Get<int>(); set => Set(value, () => NotifyPropertyChange(nameof(Cursor))); }
    public bool IsEditing { get => Get<bool>(); set => Set(value); }
    public virtual Cursor Cursor { get => CursorHelper.CreateCursor(Color, Size); }
    public virtual bool IsErasable => false;
    public ObservableCollection<Point> Points
    {
        get => Get<ObservableCollection<Point>>();
        set
        {
            var oldValue = Get<ObservableCollection<Point>>();
            if (oldValue != null)
            {
                oldValue.CollectionChanged -= OnPointsCollectionChanged;
            }

            Set(value);

            if (value != null)
            {
                value.CollectionChanged += OnPointsCollectionChanged;
            }
        }
    }

    public ICommand LeftButtonDownCommand { get; set; }
    public ICommand LeftButtonUpCommand{ get; set; }
    public ICommand MouseMoveCommand { get; set; }

    public void ApplyShape()
    {
        IsEditing = false;
        _applyShapeCallback();
    }

    public void EditShape()
    {
        IsEditing = true;
        _editInfo = new DrawingShapeInfo(this);
    }

    public void CancelEditShape()
    {
        IsEditing = false;
        Size = _editInfo.Size;
        Color = _editInfo.Color;
        Points = new ObservableCollection<Point>(_editInfo.Points);
    }

    protected abstract void ButtonDown(Point position);
    protected abstract void ButtonUp(Point position);
    protected abstract void MouseMove(Point position, bool buttonDown);

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPointsChanged?.Invoke(this, e);
    }

    private void LeftButtonDown(MouseDataEventArgs mouseDataEventArgs)
    {
        if(_previousMouseButtonState == MouseButtonState.Released)
        {
            _previousMouseButtonState = MouseButtonState.Pressed;
            ButtonDown(mouseDataEventArgs.Position);
        }
    }

    private void LeftButtonUp(MouseDataEventArgs mouseDataEventArgs)
    {
        if (_previousMouseButtonState == MouseButtonState.Pressed)
        {
            _previousMouseButtonState = MouseButtonState.Released;
            ButtonUp(mouseDataEventArgs.Position);
        }
    }

    private void MouseMove(MouseDataEventArgs mouseDataEventArgs)
    {
        var directlyOver = mouseDataEventArgs.MouseEventArgs.MouseDevice.DirectlyOver;
        if (directlyOver is CustomCanvas || directlyOver is PointEllipse)
        {
            var mouseButtonState = mouseDataEventArgs.MouseEventArgs.LeftButton;
            if (_previousMouseButtonState != mouseButtonState)
            {
                _previousMouseButtonState = mouseButtonState;

                if (mouseButtonState == MouseButtonState.Pressed)
                {
                    ButtonDown(mouseDataEventArgs.Position);
                }
                else
                {
                    ButtonUp(mouseDataEventArgs.Position);
                }
            }
            else
            {
                MouseMove(mouseDataEventArgs.Position, mouseButtonState == MouseButtonState.Pressed);
            }
        }
    }
}

