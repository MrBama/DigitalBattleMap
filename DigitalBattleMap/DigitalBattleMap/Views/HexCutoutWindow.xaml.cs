using DigitalBattleMap.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DigitalBattleMap.Views;

public partial class HexCutoutWindow : Window
{
    private bool _isDragging;
    private double _dragOffsetX;
    private double _dragOffsetY;

    public HexCutoutWindow()
    {
        InitializeComponent();
    }

    private void ImageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not HexCutoutWindowViewModel vm) return;
        var canvas = (Canvas)sender;
        var pos = e.GetPosition(canvas);
        var (cx, cy) = vm.GetDisplayCenter();
        _dragOffsetX = pos.X - cx;
        _dragOffsetY = pos.Y - cy;
        _isDragging = true;
        canvas.CaptureMouse();
        e.Handled = true;
    }

    private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || DataContext is not HexCutoutWindowViewModel vm) return;
        var pos = e.GetPosition((Canvas)sender);
        vm.SetHexCenterFromDisplayCoords(pos.X - _dragOffsetX, pos.Y - _dragOffsetY);
        e.Handled = true;
    }

    private void ImageCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        ((Canvas)sender).ReleaseMouseCapture();
        e.Handled = true;
    }
}
