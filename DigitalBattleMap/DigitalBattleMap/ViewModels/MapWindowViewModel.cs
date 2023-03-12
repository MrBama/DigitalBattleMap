using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap
{
    public class MapWindowViewModel : PropertyHandler
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
    }
}
