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
        private double _penSize = 5;
        private int _selectedTabIndex = 0;
        private bool _isGridEnabled = true;
        private IWindowService _windowService;
        private MapWindowViewModel _mapWindowViewModel;
        private Settings _settings;
        private BackgroundController _backgroundController;

        public MainWindowViewModel(IWindowService windowService)
        {
            _windowService = windowService;
            Initialize();
            OpenMapWindow();
        }

        public MainWindowViewModel()
        {
            Initialize();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public BitmapSource BackgroundBitmapSource
        {
            get => _backgroundController.BackgroundBitmap.ToBitmapImage();
        }

        public BitmapSource GridBitmapSource
        {
            get => _gridBitmap.ToBitmapImage();
        }

        public BitmapSource InkCanvasBackgroundBitmapSource
        {
            get => _inkCanvasBitmap.ToBitmapImage();
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

        public int SelectedTabIndex 
        { 
            get => _selectedTabIndex; 
            set
            {
                if (value != _selectedTabIndex)
                {
                    _selectedTabIndex = value;
                    SelectedTabChanged();
                    NotifyPropertyChange();
                }
            }
        }

        public bool IsGridEnabled 
        { 
            get => _isGridEnabled; 
            set
            {
                if (value != _isGridEnabled)
                {
                    _isGridEnabled = value;
                    GridEnabledChanged();
                    NotifyPropertyChange();
                }
            }
        }

        public int GridSize { get; set; }
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
        public Visibility GridVisibility { get; set; } = Visibility.Visible;
        public Visibility InkCanvasVisibility { get; set; } = Visibility.Hidden;

        public ICommand GridSizeEnterCommand { get; set; }
        public ICommand ShowMapCommand { get; set; }
        public ICommand WindowClosingCommand { get; set; }
        public ICommand DrawingColorChangedCommand { get; set; }
        public ICommand InkCanvasSizeOnStartupCommand { get; set; }
        public ICommand ClearAllCommand { get; set; }
        public ICommand SettingsCommand { get; set; }
        public ICommand OpenBackgroundCommand { get; set; }
        public ICommand ClearBackgroundCommand { get; set; }
        public ICommand ClearDrawingCommand { get; set; }

        public void Initialize()
        {
            _settings = Settings.Load();
            GridSize = _settings.DefaultGridSize;
            _gridBitmap = BitmapTools.CreateGrid(GridSize);
            _inkCanvasBitmap = BitmapTools.CreateEmptyBitmap();
            _backgroundController = new BackgroundController(_windowService);
            _backgroundController.BackgroundUpdated += (sender, e) => NotifyPropertyChange(nameof(BackgroundBitmapSource));

            GridSizeEnterCommand = new RelayCommand(p => GridSizeChanged());
            ShowMapCommand = new RelayCommand(p => ShowMap());
            WindowClosingCommand = new RelayCommand(p => WindowClosing());
            DrawingColorChangedCommand = new RelayCommand(p => DrawingColorChanged((string)p));
            InkCanvasSizeOnStartupCommand = new RelayCommand(p => InkCanvasSizeOnStartup((double)p));
            ClearAllCommand = new RelayCommand(p => ClearMap());
            SettingsCommand = new RelayCommand(p => OpenSettings());
            OpenBackgroundCommand = new RelayCommand(p => _backgroundController.OpenBackground());
            ClearBackgroundCommand = new RelayCommand(p => _backgroundController.ClearBackground());
            ClearDrawingCommand = new RelayCommand(p => ClearInkCanvas());

            InkCanvasDrawingAttributes.Width = PenSize;
            InkCanvasDrawingAttributes.Height = PenSize;
            InkCanvasDrawingAttributes.IgnorePressure = true;
            EraserShape = new RectangleStylusShape(PenSize, PenSize);

            InitializeColorButtons();
        }

        public void OpenMapWindow()
        {
            _mapWindowViewModel = new MapWindowViewModel();
            _windowService.ShowWindow<MapWindow>(_mapWindowViewModel);
            _mapWindowViewModel.ChangeWindowPosition(_settings.MonitorPosition.X);
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

        private void ShowMap()
        {
            var gridBitmap = IsGridEnabled ? _gridBitmap : BitmapTools.CreateEmptyBitmap();
            var map = BitmapTools.CreateMap(gridBitmap, Strokes, (int)_inkCanvasWidth, (int)_inkCanvasHeight);
            _mapWindowViewModel.BackgroundBitmapSource = _backgroundController.BackgroundBitmap.ToBitmapImage();
            _mapWindowViewModel.GridBitmapSource = map.ToBitmapImage();
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
                _backgroundController.ClearBackground();
            }
        }

        private void OpenSettings()
        {
            var settingsWindowViewModel = new SettingsWindowViewModel(_settings);
            _windowService.ShowWindowDialog<SettingsWindow>(settingsWindowViewModel);

            if (settingsWindowViewModel.MonitorChanged)
            {
                _mapWindowViewModel.ChangeWindowPosition(_settings.MonitorPosition.X);
            }
        }

        public void ClearInkCanvas()
        {
            Strokes.Clear();
        }

        public void SelectedTabChanged()
        {
            switch(SelectedTabIndex)
            {
                case 0: // Background
                    InkCanvasVisibility = Visibility.Hidden;
                    break;
                case 1: // Grid
                    InkCanvasVisibility = Visibility.Hidden;
                    break;
                case 2: // Drawing
                    InkCanvasVisibility = Visibility.Visible;
                    break;
            }

            NotifyPropertyChange(nameof(InkCanvasVisibility));
        }

        public void GridEnabledChanged()
        {
            if(IsGridEnabled)
            {
                GridVisibility = Visibility.Visible;
            }
            else
            {
                GridVisibility = Visibility.Hidden;
            }
            NotifyPropertyChange(nameof(GridVisibility));
        }
    }
}
