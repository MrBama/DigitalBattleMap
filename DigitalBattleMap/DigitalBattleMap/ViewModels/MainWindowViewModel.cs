using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
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
        private ScreenPosition _selectedMonitorPosition;
        private double _penSize = 5;
        private IWindowService _windowService = null;
        private MapWindowViewModel _mapWindow = null;
        private ICommand _gridSizeEnterCommand;
        private ICommand _showMapCommand;
        private ICommand _windowClosingCommand;
        private ICommand _drawingColorChangedCommand;
        private ICommand _inkCanvasSizeOnStartupCommand;
        private ICommand _clearCommand;

        public MainWindowViewModel()
        {
            _gridBitmap = BitmapTools.CreateGrid(GridSize);
            _inkCanvasBitmap = BitmapTools.CreateEmptyBitmap();

            _gridSizeEnterCommand = new RelayCommand(p => GridSizeChanged());
            _showMapCommand = new RelayCommand(p => ShowMap());
            _windowClosingCommand = new RelayCommand(p => WindowClosing());
            _drawingColorChangedCommand = new RelayCommand(p => DrawingColorChanged((string)p));
            _inkCanvasSizeOnStartupCommand = new RelayCommand(p => InkCanvasSizeOnStartup((double)p));
            _clearCommand = new RelayCommand(p => ClearMap());

            foreach (var screenPosition in ScreenWrapper.GetScreenPositions())
            {
                MonitorPositions.Add(screenPosition);
            }

            InkCanvasDrawingAttributes.Width = PenSize;
            InkCanvasDrawingAttributes.Height = PenSize;
            EraserShape = new RectangleStylusShape(PenSize, PenSize);

            InitializeColorButtons();

            _selectedMonitorPosition = MonitorPositions.SingleOrDefault(pos => pos.X == 2560);
            _selectedMonitorPosition = _selectedMonitorPosition == null ? MonitorPositions.First() : _selectedMonitorPosition;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public BitmapSource GridBitmapSource
        {
            get => _gridBitmap.ToBitmapImage();
        }

        public BitmapSource InkCanvasBackgroundBitmapSource
        {
            get => _inkCanvasBitmap.ToBitmapImage();
        }

        public ObservableCollection<ScreenPosition> MonitorPositions { get; private set; } = new ObservableCollection<ScreenPosition>();

        public ScreenPosition SelectedMonitorPosition
        {
            get => _selectedMonitorPosition;

            set
            {
                if (value != _selectedMonitorPosition)
                {
                    _selectedMonitorPosition = value;
                    MonitorPositionChanged();
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
                    NotifyPropertyChange();
                }
            }
        }

        public int GridSize { get; set; } = 65;
        public DrawingAttributes InkCanvasDrawingAttributes { get; set; } = new DrawingAttributes();
        public InkCanvasEditingMode EditingMode { get; set; } = InkCanvasEditingMode.Ink;
        public StylusShape EraserShape { get; set; }
        public StrokeCollection Strokes { get; set; } = new StrokeCollection();
        public BitmapSource BlackButtonBitmapSource { get; set; }
        public BitmapSource RedButtonBitmapSource { get; set; }
        public BitmapSource GreenButtonBitmapSource { get; set; }
        public BitmapSource BlueButtonBitmapSource { get; set; }
        public BitmapSource EraserButtonBitmapSource { get; set; }
        public BitmapSource BlackButtonSelectedBitmapSource { get; set; }
        public BitmapSource RedButtonSelectedBitmapSource { get; set; }
        public BitmapSource GreenButtonSelectedBitmapSource { get; set; }
        public BitmapSource BlueButtonSelectedBitmapSource { get; set; }
        public BitmapSource EraserButtonSelectedBitmapSource { get; set; }
        public Visibility BlackButtonSelectedVisibility { get; set; } = Visibility.Visible;
        public Visibility RedButtonSelectedVisibility { get; set; } = Visibility.Hidden;
        public Visibility GreenButtonSelectedVisibility { get; set; } = Visibility.Hidden;
        public Visibility BlueButtonSelectedVisibility { get; set; } = Visibility.Hidden;
        public Visibility EraserButtonSelectedVisibility { get; set; } = Visibility.Hidden;

        public ICommand GridSizeEnterCommand { get => _gridSizeEnterCommand; }
        public ICommand ShowMapCommand { get => _showMapCommand; }
        public ICommand WindowClosingCommand { get => _windowClosingCommand; }
        public ICommand DrawingColorChangedCommand { get => _drawingColorChangedCommand; }
        public ICommand InkCanvasSizeOnStartupCommand { get => _inkCanvasSizeOnStartupCommand; }
        public ICommand ClearCommand { get => _clearCommand; }

        public void SetWindowService(IWindowService windowService)
        {
            _windowService = windowService;
            _mapWindow = new MapWindowViewModel();
            _windowService.ShowWindow<MapWindow>(_mapWindow);
            _mapWindow.ChangeWindowPosition(SelectedMonitorPosition.X);
        }

        private void NotifyPropertyChange([CallerMemberName] string propertyname = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        private void GridSizeChanged()
        {
            GridSize = Math.Max(GridSize, 1);
            _gridBitmap = BitmapTools.CreateGrid(GridSize);
            NotifyPropertyChange(nameof(GridBitmapSource));
        }

        private void MonitorPositionChanged()
        {
            _mapWindow.ChangeWindowPosition(SelectedMonitorPosition.X);
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
            _penSize = Math.Clamp(_penSize, 1, 100);
            InkCanvasDrawingAttributes.Width = _penSize;
            InkCanvasDrawingAttributes.Height = _penSize;
            EraserShape = new EllipseStylusShape(_penSize, _penSize);
            
            NotifyPropertyChange(nameof(EraserShape));
        }

        private void InitializeColorButtons()
        {
            BlackButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 0), false).ToBitmapImage();
            RedButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 255, 0, 0), false).ToBitmapImage();
            GreenButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 255, 0), false).ToBitmapImage();
            BlueButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 255), false).ToBitmapImage();
            EraserButtonBitmapSource = BitmapTools.CreateEraserButton(false).ToBitmapImage();

            BlackButtonSelectedBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 0), true).ToBitmapImage();
            RedButtonSelectedBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 255, 0, 0), true).ToBitmapImage();
            GreenButtonSelectedBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 255, 0), true).ToBitmapImage();
            BlueButtonSelectedBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 255), true).ToBitmapImage();
            EraserButtonSelectedBitmapSource = BitmapTools.CreateEraserButton(true).ToBitmapImage();
        }

        private void DrawingColorChanged(string color)
        {
            BlackButtonSelectedVisibility = Visibility.Hidden;
            RedButtonSelectedVisibility = Visibility.Hidden;
            GreenButtonSelectedVisibility = Visibility.Hidden;
            BlueButtonSelectedVisibility = Visibility.Hidden;
            EraserButtonSelectedVisibility = Visibility.Hidden;

            if (EditingMode == InkCanvasEditingMode.EraseByPoint)
            {
                EditingMode = InkCanvasEditingMode.Ink;
                NotifyPropertyChange(nameof(EditingMode));
            }

            switch (color)
            {
                case "Black":
                    InkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 0, 0);
                    BlackButtonSelectedVisibility = Visibility.Visible;
                    break;
                case "Red":
                    InkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(255, 0, 0);
                    RedButtonSelectedVisibility = Visibility.Visible;
                    break;
                case "Green":
                    InkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 255, 0);
                    GreenButtonSelectedVisibility = Visibility.Visible;
                    break;
                case "Blue":
                    InkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 0, 255);
                    BlueButtonSelectedVisibility = Visibility.Visible;
                    break;
                case "Eraser":
                    EditingMode = InkCanvasEditingMode.EraseByPoint;
                    EraserButtonSelectedVisibility = Visibility.Visible;
                    NotifyPropertyChange(nameof(EditingMode));
                    break;
            }

            NotifyPropertyChange(nameof(BlackButtonSelectedVisibility));
            NotifyPropertyChange(nameof(RedButtonSelectedVisibility));
            NotifyPropertyChange(nameof(GreenButtonSelectedVisibility));
            NotifyPropertyChange(nameof(BlueButtonSelectedVisibility));
            NotifyPropertyChange(nameof(EraserButtonSelectedVisibility));
        }

        private void InkCanvasSizeOnStartup(double width)
        {
            // It's enought to only use the width, since everything is done in a 16:9 ratio
            _inkCanvasWidth = width;
            _inkCanvasHeight = width / 16 * 9;
        }

        private void ClearMap()
        {
            var confirmationWindowViewModel = new ConfirmationWindowViewModel();
            confirmationWindowViewModel.Content = "Are you sure you want to clear everything?";
            _windowService.ShowWindowDialog<ConfirmationWindow>(confirmationWindowViewModel);

            if(confirmationWindowViewModel.Confirmed)
            {
                Strokes.Clear();
            }
        }
    }
}
