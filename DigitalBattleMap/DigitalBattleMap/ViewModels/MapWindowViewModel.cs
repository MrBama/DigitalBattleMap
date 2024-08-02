using System.Windows;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels;

public class MapWindowViewModel : ViewModelBase
{
    public double Left { get => Get<double>(); set => Set(value); }
    public WindowState WindowState { get => Get<WindowState>(); set => Set(value); }
    public BitmapSource BackgroundBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource GridBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource TokenBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public void ChangeWindowPosition(int x)
    {
        WindowState = WindowState.Normal;
        Left = x + 10;
        WindowState = WindowState.Maximized;
    }

    protected override void InitializeCommands()
    {
    }
}
