using DigitalBattleMap.DataClasses;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace DigitalBattleMap.UIElements;

public class MouseCanvas : Canvas
{
    public MouseCanvas()
    {
        ClipToBounds = true;
    }

    public static readonly DependencyProperty LeftButtonDownCommandProperty = DependencyProperty.Register(nameof(LeftButtonDownCommand), typeof(ICommand), typeof(MouseCanvas));
    public static readonly DependencyProperty LeftButtonUpCommandProperty = DependencyProperty.Register(nameof(LeftButtonUpCommand), typeof(ICommand), typeof(MouseCanvas));
    public static readonly DependencyProperty RightButtonDownCommandProperty = DependencyProperty.Register(nameof(RightButtonDownCommand), typeof(ICommand), typeof(MouseCanvas));
    public static readonly DependencyProperty RightButtonUpCommandProperty = DependencyProperty.Register(nameof(RightButtonUpCommand), typeof(ICommand), typeof(MouseCanvas));
    public static readonly DependencyProperty MouseMoveCommandProperty = DependencyProperty.Register(nameof(MouseMoveCommand), typeof(ICommand), typeof(MouseCanvas));

    public ICommand LeftButtonDownCommand
    {
        get => (ICommand)GetValue(LeftButtonDownCommandProperty);
        set => SetValue(LeftButtonDownCommandProperty, value);
    }

    public ICommand LeftButtonUpCommand
    {
        get => (ICommand)GetValue(LeftButtonUpCommandProperty);
        set => SetValue(LeftButtonUpCommandProperty, value);
    }

    public ICommand RightButtonDownCommand
    {
        get => (ICommand)GetValue(RightButtonDownCommandProperty);
        set => SetValue(RightButtonDownCommandProperty, value);
    }

    public ICommand RightButtonUpCommand
    {
        get => (ICommand)GetValue(RightButtonUpCommandProperty);
        set => SetValue(RightButtonUpCommandProperty, value);
    }

    public ICommand MouseMoveCommand
    {
        get => (ICommand)GetValue(MouseMoveCommandProperty);
        set => SetValue(MouseMoveCommandProperty, value);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var mousePosition = e.GetPosition(this);
        LeftButtonDownCommand.Execute(new MouseDataEventArgs() { MouseEventArgs = e, Position = new Point<double>(mousePosition.X, mousePosition.Y) });
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        var mousePosition = e.GetPosition(this);
        LeftButtonUpCommand.Execute(new MouseDataEventArgs() { MouseEventArgs = e, Position = new Point<double>(mousePosition.X, mousePosition.Y) });
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var mousePosition = e.GetPosition(this);
        RightButtonDownCommand.Execute(new MouseDataEventArgs() { MouseEventArgs = e, Position = new Point<double>(mousePosition.X, mousePosition.Y) });
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        var mousePosition = e.GetPosition(this);
        RightButtonUpCommand.Execute(new MouseDataEventArgs() { MouseEventArgs = e, Position = new Point<double>(mousePosition.X, mousePosition.Y) });
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var mousePosition = e.GetPosition(this);
        MouseMoveCommand.Execute(new MouseDataEventArgs() { MouseEventArgs = e, Position = new Point<double>(mousePosition.X, mousePosition.Y) });
    }
}
