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
    public class MapWindowViewModel : INotifyPropertyChanged
    {
        private double _left;
        private WindowState _windowState;
        private BitmapSource _backgroundBitmapSource;
        private BitmapSource _gridBitmapSource;

        public event PropertyChangedEventHandler? PropertyChanged;

        public double Left 
        { 
            get => _left; 
            set
            {
                if(value != _left)
                {
                    _left = value;
                    NotifyPropertyChange();
                }
            }
        }

        public WindowState WindowState
        {
            get => _windowState;
            set
            {
                if (value != _windowState)
                {
                    _windowState = value;
                    NotifyPropertyChange();
                }
            }
        }

        public BitmapSource BackgroundBitmapSource
        {
            get => _backgroundBitmapSource;
            set
            {
                if (value != _backgroundBitmapSource)
                {
                    _backgroundBitmapSource = value;
                    NotifyPropertyChange();
                }
            }
        }

        public BitmapSource GridBitmapSource
        {
            get => _gridBitmapSource;
            set
            {
                if (value != _gridBitmapSource)
                {
                    _gridBitmapSource = value;
                    NotifyPropertyChange();
                }
            }
        }

        public void ChangeWindowPosition(int x)
        {
            WindowState = WindowState.Normal;
            Left = x + 10;
            WindowState = WindowState.Maximized;
        }

        private void NotifyPropertyChange([CallerMemberName] string propertyname = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }
    }
}
