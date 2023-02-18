using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private Bitmap _gridBitmap;
        private Bitmap _inkCanvasBitmap;
        private double _inkCanvasWidth;
        private double _inkCanvasHeight;
        private int _selectedMonitor = 0;
        private double _penSize = 5;
        private IWindowService _windowService = null;
        private MapWindowViewModel _mapWindow = null;
        private ICommand _gridSizeEnterCommand;
        private ICommand _showMapCommand;
        private ICommand _windowClosingCommand;
        private ICommand _drawingColorChangedCommand;
        private ICommand _inkCanvasSizeOnStartupCommand;

        public MainWindowViewModel()
        {
            _gridBitmap = BitmapTools.CreateGrid(GridSize);
            _inkCanvasBitmap = BitmapTools.CreateEmptyBitmap();

            _gridSizeEnterCommand = new RelayCommand(p => GridSizeChanged());
            _showMapCommand = new RelayCommand(p => ShowMap());
            _windowClosingCommand = new RelayCommand(p => WindowClosing());
            _drawingColorChangedCommand = new RelayCommand(p => DrawingColorChanged((string)p));
            _inkCanvasSizeOnStartupCommand = new RelayCommand(p => InkCanvasSizeOnStartup((double)p));

            for (int i = 0; i < ScreenWrapper.GetScreenCount(); i++)
            {
                MonitorNumbers.Add(i + 1);
            }

            InkCanvasDrawingAttributes.Width = PenSize;
            InkCanvasDrawingAttributes.Height = PenSize;
            EraserShape = new RectangleStylusShape(PenSize, PenSize);

            _selectedMonitor = 1;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public BitmapSource GridBitMapSource
        {
            get => _gridBitmap.ToBitmapImage();
        }

        public BitmapSource InkCanvasBackgroundBitMapSource
        {
            get => _inkCanvasBitmap.ToBitmapImage();
        }

        public ObservableCollection<int> MonitorNumbers { get; private set; } = new ObservableCollection<int>();

        public int SelectedMonitor
        {
            get => _selectedMonitor;

            set
            {
                if (value != _selectedMonitor)
                {
                    _selectedMonitor = value;
                    MonitorNumberChanged();
                }
            }
        }

        public double PenSize
        {
            get => _penSize;
            set
            {
                if (value != _penSize)
                {
                    _penSize = value;
                    PenSizeChanged();
                }
            }
        }

        public int GridSize { get; set; } = 65;
        public DrawingAttributes InkCanvasDrawingAttributes { get; set; } = new DrawingAttributes();
        public InkCanvasEditingMode EditingMode { get; set; } = InkCanvasEditingMode.Ink;
        public StylusShape EraserShape { get; set; }
        public StrokeCollection Strokes { get; set; } = new StrokeCollection();

        public ICommand GridSizeEnterCommand { get => _gridSizeEnterCommand; }
        public ICommand ShowMapCommand { get => _showMapCommand; }
        public ICommand WindowClosingCommand { get => _windowClosingCommand; }
        public ICommand DrawingColorChangedCommand { get => _drawingColorChangedCommand; }
        public ICommand InkCanvasSizeOnStartupCommand { get => _inkCanvasSizeOnStartupCommand; }

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
            GridSize = Math.Max(GridSize, 1);
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
            var map = BitmapTools.CreateMap(_gridBitmap, Strokes, (int)_inkCanvasWidth, (int)_inkCanvasHeight);
            _mapWindow.MapBitmapSource = map.ToBitmapImage();
        }

        private void WindowClosing()
        {
            _windowService.CloseAllWindows();
        }

        private void PenSizeChanged()
        {
            _penSize = Math.Clamp(_penSize, 1, 1000);
            InkCanvasDrawingAttributes.Width = _penSize;
            InkCanvasDrawingAttributes.Height = _penSize;
            EraserShape = new EllipseStylusShape(_penSize, _penSize);
            
            NotifyPropertyChange(nameof(EraserShape));
        }

        private void DrawingColorChanged(string color)
        {
            if (EditingMode == InkCanvasEditingMode.EraseByPoint)
            {
                EditingMode = InkCanvasEditingMode.Ink;
                NotifyPropertyChange(nameof(EditingMode));
            }

            switch (color)
            {
                case "Black":
                    InkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 0, 0);
                    return;
                case "Red":
                    InkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(255, 0, 0);
                    return;
                case "Green":
                    InkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 255, 0);
                    return;
                case "Blue":
                    InkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 0, 255);
                    return;
                case "Eraser":
                    EditingMode = InkCanvasEditingMode.EraseByPoint;
                    NotifyPropertyChange(nameof(EditingMode));
                    return;
            }
        }

        private void InkCanvasSizeOnStartup(double width)
        {
            // It's enought to only use the width, since everything is done in a 16:9 ratio
            _inkCanvasWidth = width;
            _inkCanvasHeight = width / 16 * 9;
        }
    }
}
