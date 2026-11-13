using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels;

public class MapWindowViewModel : ViewModelBase
{
    private Size<double> _windowSize = new Size<double>(Constants.MapSize.Width, Constants.MapSize.Height);

    public MapWindowViewModel()
    {
        PingSize = Constants.PingSize;
    }

    public double Left { get => Get<double>(); set => Set(value); }
    public double PingPositionX { get => Get<double>(); set => Set(value); }
    public double PingPositionY { get => Get<double>(); set => Set(value); }
    public double PingSize { get => Get<double>(); set => Set(value); }
    public bool IsPaused { get => Get<bool>(); set => Set(value); }
    public bool IsPingVisible { get => Get<bool>(); set => Set(value); }
    public WindowState WindowState { get => Get<WindowState>(); set => Set(value); }
    public BitmapSource BackgroundBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource GridBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource TokenBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource FogBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource PauseIconBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.PauseIcon.png")).ToBitmapImage(); }

    public ICommand WindowSizeChangedCommand { get; set; }

    protected override void InitializeCommands()
    {
        WindowSizeChangedCommand = new RelayCommand(p => WindowSizeChanged((SizeChangedEventArgs)p));
    }

    public void ChangeWindowPosition(int x)
    {
        WindowState = WindowState.Normal;
        Left = x + 10;
        WindowState = WindowState.Maximized;
    }

    public void EnablePing(double positionX, double positionY)
    {
        PingSize = ((double)Constants.PingSize).Map(0, Constants.MapSize.Width, 0, _windowSize.Width);

        var posX = positionX.Map(0, Constants.MapSize.Width, 0, _windowSize.Width);
        var posY = positionY.Map(0, Constants.MapSize.Height, 0, _windowSize.Height);

        PingPositionX = posX - (PingSize / 2);
        PingPositionY = posY - (PingSize / 2);
        IsPingVisible = true;
    }

    public void DisablePing()
    {
        IsPingVisible = false;
    }

    private void WindowSizeChanged(SizeChangedEventArgs eventArgs)
    {
        _windowSize.Width = eventArgs.NewSize.Width;
        _windowSize.Height = eventArgs.NewSize.Height;
    }
}
