using DigitalBattleMap.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace DigitalBattleMap.UIElements;

public class HexOverlayElement : FrameworkElement
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(HexCutoutWindowViewModel), typeof(HexOverlayElement),
            new PropertyMetadata(null, OnViewModelChanged));

    public HexCutoutWindowViewModel ViewModel
    {
        get => (HexCutoutWindowViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var el = (HexOverlayElement)d;
        if (e.OldValue is HexCutoutWindowViewModel old)
            old.PropertyChanged -= el.OnVmPropertyChanged;
        if (e.NewValue is HexCutoutWindowViewModel vm)
            vm.PropertyChanged += el.OnVmPropertyChanged;
        el.InvalidateVisual();
    }

    private void OnVmPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HexCutoutWindowViewModel.OverlayPoints))
            InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var vm = ViewModel;
        if (vm == null) return;

        var (cx, cy) = vm.GetDisplayCenter();
        var radius = vm.HexRadius;
        if (radius <= 0) return;

        var geom = HexGeometry.CreateStreamGeometry(cx, cy, radius);
        geom.Freeze();

        var fill = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0x00, 0x00));
        fill.Freeze();
        var stroke = new Pen(Brushes.Red, 2.0);
        stroke.Freeze();

        dc.DrawGeometry(fill, stroke, geom);
    }
}
