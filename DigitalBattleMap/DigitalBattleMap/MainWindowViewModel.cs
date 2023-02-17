using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private Bitmap _gridBitmap;
        private int _selectedMonitor = 0;
        private IWindowService _windowService = null;
        private MapWindowViewModel _mapWindow = null;
        private ICommand _gridSizeEnterCommand;
        private ICommand _showMapCommand;
        private ICommand _windowClosingCommand;

        public MainWindowViewModel()
        {
            _gridBitmap = BitmapTools.CreateGrid(GridSize);

            _gridSizeEnterCommand = new RelayCommand(p => GridSizeChanged());
            _showMapCommand = new RelayCommand(p => ShowMap());
            _windowClosingCommand = new RelayCommand(p => WindowClosing());

            for(int i = 0; i < ScreenWrapper.GetScreenCount(); i++)
            {
                MonitorNumbers.Add(i + 1);
            }
            _selectedMonitor = 1;

        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public BitmapSource GridBitMapSource 
        { 
            get => _gridBitmap.ToBitmapImage();
        }

        public ObservableCollection<int> MonitorNumbers { get; private set; } = new ObservableCollection<int>();

        public int SelectedMonitor
        {
            get => _selectedMonitor;

            set
            {
                if(value != _selectedMonitor)
                {
                    _selectedMonitor = value;
                }
                MonitorNumberChanged();
            }
        }

        public int GridSize { get; set; } = 80;

        public ICommand GridSizeEnterCommand { get => _gridSizeEnterCommand; }
        public ICommand ShowMapCommand { get => _showMapCommand; }
        public ICommand WindowClosingCommand { get => _windowClosingCommand; }

        public void SetWindowService(IWindowService windowService)
        {
            _windowService = windowService;
            _mapWindow = new MapWindowViewModel();
            _windowService.ShowWindow<MapWindow>(_mapWindow);
            (int x, int y) = ScreenWrapper.GetScreenPosition(_selectedMonitor);
            _mapWindow.ChangeWindowPosition(x);
        }

        private void NotifyPropertyChange([CallerMemberName] string propertyname = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        private void GridSizeChanged()
        {
            _gridBitmap = BitmapTools.CreateGrid(GridSize);
            NotifyPropertyChange(nameof(GridBitMapSource));
        }

        private void MonitorNumberChanged()
        {
            (int x, int y) = ScreenWrapper.GetScreenPosition(_selectedMonitor);
            _mapWindow.ChangeWindowPosition(x);
        }

        private void ShowMap()
        {
            _mapWindow.MapBitmapSource = _gridBitmap.ToBitmapImage();
        }

        private void WindowClosing()
        {
            _windowService.CloseAllWindows();
        }
    }
}
