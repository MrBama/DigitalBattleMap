using DigitalBattleMap.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DigitalBattleMap.Views;

public partial class HexCutoutWindow : Window
{
    public HexCutoutWindow()
    {
        InitializeComponent();
    }

    private void ImageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is HexCutoutWindowViewModel vm)
        {
            var pos = e.GetPosition((Canvas)sender);
            vm.SetHexCenterFromDisplayCoords(pos.X, pos.Y);
        }
    }
}
