using DrawingCanvas.HelperClasses;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DrawingCanvas;

public class MouseCanvas : Canvas
{
    public MouseCanvas()
    {
        ClipToBounds = true;
    }

    public static readonly DependencyProperty MouseLeftButtonDownCommandProperty = DependencyProperty.Register(nameof(MouseLeftButtonDownCommand), typeof(ICommand), typeof(MouseCanvas));
    public static readonly DependencyProperty MouseMoveCommandProperty = DependencyProperty.Register(nameof(MouseMoveCommand), typeof(ICommand), typeof(MouseCanvas));
    public static readonly DependencyProperty MouseLeftButtonUpCommandProperty = DependencyProperty.Register(nameof(MouseLeftButtonUpCommand), typeof(ICommand), typeof(MouseCanvas));

    public ICommand MouseLeftButtonDownCommand
    {
        get => (ICommand)GetValue(MouseLeftButtonDownCommandProperty);
        set => SetValue(MouseLeftButtonDownCommandProperty, value);
    }

    public ICommand MouseMoveCommand
    {
        get => (ICommand)GetValue(MouseMoveCommandProperty);
        set => SetValue(MouseMoveCommandProperty, value);
    } 
    
    public ICommand MouseLeftButtonUpCommand
    {
        get => (ICommand)GetValue(MouseLeftButtonUpCommandProperty);
        set => SetValue(MouseLeftButtonUpCommandProperty, value);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        MouseLeftButtonDownCommand.Execute(new MouseDataEventArgs(e, e.GetPosition(this)));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        MouseMoveCommand.Execute(new MouseDataEventArgs(e, e.GetPosition(this)));
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        MouseLeftButtonUpCommand.Execute(new MouseDataEventArgs(e, e.GetPosition(this)));
    }
}
