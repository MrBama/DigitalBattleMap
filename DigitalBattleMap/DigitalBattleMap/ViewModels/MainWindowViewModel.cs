using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap
{
    public class MainWindowViewModel : PropertyHandler
    {
        private Bitmap _gridBitmap;
        private Bitmap _inkCanvasBitmap;
        private double _inkCanvasWidth;
        private double _inkCanvasHeight;
        private IWindowService _windowService;
        private MapWindowViewModel _mapWindowViewModel;
        private Settings _settings;
        private BackgroundController _backgroundController;
        private TokenController _tokenController;
        private ConnectionManager _connectionManager;

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

        public double PenSize { get => Get<double>(); set => Set(Math.Clamp(value, 1, 100), PenSizeChanged); }
        public int SelectedTabIndex { get => Get<int>(); set => Set(value, SelectedTabChanged); }
        public int GridSize { get => Get<int>(); set => Set(value); }
        public int InkCanvasZIndex { get => Get<int>(); set => Set(value); }
        public bool IsGridShown { get => Get<bool>(); set => Set(value, GridShownChanged); }
        public bool IsShowMapLocked { get => Get<bool>(); set => Set(value, () => UpdateMap(DrawLayer.All)); }
        public bool ServerConnectionButtonEnabled { get => Get<bool>(); set => Set(value); }
        public double BackgroundZoomPercentage { get => Get<double>(); set => Set(value, () => NotifyPropertyChange(nameof(BackgroundZoomPercentageLabel))); }
        public string ServerConnectionButtonText { get => Get<string>(); set => Set(value); }
        public string ServerConnectionStatus { get => Get<string>(); set => Set(value); }
        public Visibility BlackButtonSelectedVisibility { get => Get<Visibility>(); set => Set(value); }
        public Visibility RedButtonSelectedVisibility { get => Get<Visibility>(); set => Set(value); }
        public Visibility GreenButtonSelectedVisibility { get => Get<Visibility>(); set => Set(value); }
        public Visibility BlueButtonSelectedVisibility { get => Get<Visibility>(); set => Set(value); }
        public Visibility EraserButtonSelectedVisibility { get => Get<Visibility>(); set => Set(value); }
        public Visibility GridVisibility { get => Get<Visibility>(); set => Set(value); }
        public Visibility InkCanvasVisibility { get => Get<Visibility>(); set => Set(value); }
        public Visibility MouseInputCanvasVisibility { get => Get<Visibility>(); set => Set(value); }
        public Visibility TokenVisibility { get => Get<Visibility>(); set => Set(value); }
        public StylusShape EraserShape { get => Get<StylusShape>(); set => Set(value); }
        public InkCanvasEditingMode EditingMode { get => Get<InkCanvasEditingMode>(); set => Set(value); }
        public StrokeCollection Strokes { get => Get<StrokeCollection>(); set => Set(value); }
        public System.Windows.Media.Brush ServerConnectionStatusColor { get => Get<System.Windows.Media.Brush>(); set => Set(value); }

        public BitmapSource BackgroundBitmapSource { get => _backgroundController.GetBackgroundBitmapSource(); }
        public BitmapSource TokenBitmapSource { get => _tokenController.GetTokenBitmapSource(); }
        public BitmapSource TokenSelectionBitmapSource { get => _tokenController.GetTokenSelectionBitmapSource(); }
        public BitmapSource GridBitmapSource { get => _gridBitmap.ToBitmapImage(); }
        public BitmapSource InkCanvasBackgroundBitmapSource { get => _inkCanvasBitmap.ToBitmapImage(); }
        public BitmapSource MapArrowUpBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Up).ToBitmapImage(); }
        public BitmapSource MapArrowDownBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Down).ToBitmapImage(); }
        public BitmapSource MapArrowLeftBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Left).ToBitmapImage(); }
        public BitmapSource MapArrowRightBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Right).ToBitmapImage(); }
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
        public DrawingAttributes InkCanvasDrawingAttributes { get; set; } = new DrawingAttributes();
        public ObservableCollection<TokenListItem> TokenList { get => _tokenController.TokenList; }
        public TokenListItem SelectedToken { get => _tokenController.SelectedToken; set => _tokenController.SelectedToken = value; }
        public string BackgroundZoomPercentageLabel { get => $"{BackgroundZoomPercentage}%"; }
        public double MouseInputX { get; set; }
        public double MouseInputY { get; set; }
        public bool HasOpenedBackground { get => _backgroundController.HasOpenedBackground(); }
        public bool IsTokenSelected { get => _tokenController.IsTokenSelected(); }
        public bool IsTokenUpButtonEnabled { get => _tokenController.IsUpButtonEnabled(); }
        public bool IsTokenDownButtonEnabled { get => _tokenController.IsDownButtonEnabled(); }
        public int GridCellsWidth { get => _backgroundController.GridCellsWidth; set => _backgroundController.GridCellsWidth = value; }
        public int GridCellsHeight { get => _backgroundController.GridCellsHeight; set => _backgroundController.GridCellsHeight = value; }
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
        public ICommand MouseInputCanvasDownCommand { get; set; }
        public ICommand MouseInputCanvasUpCommand { get; set; }
        public ICommand BackgroundZoomInCommand { get; set; }
        public ICommand BackgroundZoomOutCommand { get; set; }
        public ICommand MoveMapArrowCommand { get; set; }
        public ICommand SaveMapCommand { get; set; }
        public ICommand OpenMapCommand { get; set; }
        public ICommand AddTokenCommand { get; set; }
        public ICommand RemoveTokenCommand { get; set; }
        public ICommand ClearTokensCommand { get; set; }
        public ICommand TokenUpCommand { get; set; }
        public ICommand TokenDownCommand { get; set; }
        public ICommand CustomTokensCommand { get; set; }
        public ICommand ServerConnectionCommand { get; set; }
        public ICommand FitBackgroundToGridCommand { get; set; }

        public void Initialize()
        {
            InitializeProperties();
            _settings = Settings.Load();
            GridSize = _settings.DefaultGridSize;
            _gridBitmap = BitmapTools.CreateGrid(GridSize);
            _inkCanvasBitmap = BitmapTools.CreateEmptyBitmap();
            _backgroundController = new BackgroundController(_windowService);
            _backgroundController.BackgroundEditorUpdated += BackgroundEditorUpdated;
            _backgroundController.BackgroundUpdated += BackgroundUpdated;
            _tokenController = new TokenController(_windowService, _settings, GridSize);
            _tokenController.TokenEditorUpdated += TokenEditorUpdated;
            _tokenController.TokenBitmapUpdated += TokenBitmapUpdated;
            _tokenController.SelectedTokenBitmapUpdated += SelectedTokenBitmapUpdated;
            _connectionManager = new ConnectionManager();
            _connectionManager.Connected += ConnectionManagerConnected;
            _connectionManager.Disconnected += ConnectionManagerDisconnected;
            _connectionManager.MoveTokenAction += _tokenController.OnMoveTokenAction;
            Strokes.StrokesChanged += OnStrokesChanged;

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
            MouseInputCanvasDownCommand = new RelayCommand(p => MouseDown());
            MouseInputCanvasUpCommand = new RelayCommand(p => MouseUp());
            BackgroundZoomInCommand = new RelayCommand(p => _backgroundController.ZoomIn(BackgroundZoomPercentage));
            BackgroundZoomOutCommand = new RelayCommand(p => _backgroundController.ZoomOut(BackgroundZoomPercentage));
            MoveMapArrowCommand = new RelayCommand(p => MoveMap((string)p));
            SaveMapCommand = new RelayCommand(p => SaveMap());
            OpenMapCommand = new RelayCommand(p => OpenMap());
            AddTokenCommand = new RelayCommand(p => _tokenController.AddToken());
            RemoveTokenCommand = new RelayCommand(p => _tokenController.RemoveToken());
            ClearTokensCommand = new RelayCommand(p => _tokenController.ClearTokens());
            TokenUpCommand = new RelayCommand(p => _tokenController.TokenUp());
            TokenDownCommand = new RelayCommand(p => _tokenController.TokenDown());
            CustomTokensCommand = new RelayCommand(p => _tokenController.CustomTokens());
            ServerConnectionCommand = new RelayCommand(p => ServerConnectionButton());
            FitBackgroundToGridCommand = new RelayCommand(p => _backgroundController.FitToGrid(GridSize));

            InkCanvasDrawingAttributes.Width = PenSize;
            InkCanvasDrawingAttributes.Height = PenSize;
            InkCanvasDrawingAttributes.IgnorePressure = true;
            EraserShape = new EllipseStylusShape(PenSize, PenSize);

            InitializeColorButtons();
        }

        private void InitializeProperties()
        {
            PenSize = 5;
            IsGridShown = true;
            IsShowMapLocked = false;
            BackgroundZoomPercentage = 10;
            BlackButtonSelectedVisibility = Visibility.Visible;
            RedButtonSelectedVisibility = Visibility.Hidden;
            GreenButtonSelectedVisibility = Visibility.Hidden;
            BlueButtonSelectedVisibility = Visibility.Hidden;
            EraserButtonSelectedVisibility = Visibility.Hidden;
            GridVisibility = Visibility.Visible;
            InkCanvasVisibility = Visibility.Hidden;
            MouseInputCanvasVisibility = Visibility.Visible;
            TokenVisibility = Visibility.Hidden;
            EditingMode = InkCanvasEditingMode.Ink;
            Strokes = new StrokeCollection();
            ServerConnectionButtonText = "Connect";
            ServerConnectionStatus = "Disconnected";
            ServerConnectionStatusColor = System.Windows.Media.Brushes.Red;
            ServerConnectionButtonEnabled = true;
        }

        private void OnStrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            UpdateMap(DrawLayer.GridAndStrokes);
        }

        private void BackgroundEditorUpdated(object? sender, EventArgs e)
        {
            NotifyPropertyChange(nameof(GridCellsWidth));
            NotifyPropertyChange(nameof(GridCellsHeight));
        }

        private void BackgroundUpdated(object? sender, EventArgs e)
        {
            NotifyPropertyChange(nameof(BackgroundBitmapSource));
            NotifyPropertyChange(nameof(HasOpenedBackground));
            UpdateMap(DrawLayer.Background);
        }

        private void TokenEditorUpdated(object? sender, EventArgs e)
        {
            NotifyPropertyChange(nameof(IsTokenSelected));
            NotifyPropertyChange(nameof(IsTokenUpButtonEnabled));
            NotifyPropertyChange(nameof(IsTokenDownButtonEnabled));
            NotifyPropertyChange(nameof(SelectedToken));
        }

        private void TokenBitmapUpdated(object? sender, EventArgs e)
        {
            NotifyPropertyChange(nameof(TokenBitmapSource));
            UpdateMap(DrawLayer.Tokens);
        }

        private void SelectedTokenBitmapUpdated(object? sender, EventArgs e)
        {
            NotifyPropertyChange(nameof(TokenSelectionBitmapSource));
        }

        public void OpenMapWindow()
        {
            _mapWindowViewModel = new MapWindowViewModel();
            _windowService.ShowWindow<MapWindow>(_mapWindowViewModel);
            _mapWindowViewModel.ChangeWindowPosition(_settings.MonitorPosition.X);
        }

        private void GridSizeChanged()
        {
            GridSize = Math.Max(GridSize, 1);
            _gridBitmap = BitmapTools.CreateGrid(GridSize);
            _tokenController.UpdateGridSize(GridSize);
            NotifyPropertyChange(nameof(GridBitmapSource));
            UpdateMap(DrawLayer.GridAndStrokes);
        }

        private void ShowMap(DrawLayer drawing = 0)
        {
            switch (drawing)
            {
                case DrawLayer.All:
                    var gridAndTokenBitmapAll = CreateGridAndDrawingBitmap();
                    _mapWindowViewModel.BackgroundBitmapSource = _backgroundController.GetBackgroundBitmapSource();
                    _mapWindowViewModel.GridBitmapSource = gridAndTokenBitmapAll.ToBitmapImage();
                    _mapWindowViewModel.TokenBitmapSource = _tokenController.GetTokenBitmapSource();
                    _connectionManager.SendMapUpdate(new MapUpdate(DrawLayer.Background, _backgroundController.GetBackgroundBitmap()));
                    _connectionManager.SendMapUpdate(new MapUpdate(DrawLayer.GridAndStrokes, gridAndTokenBitmapAll));
                    _connectionManager.SendMapUpdate(new MapUpdate(DrawLayer.Tokens, _tokenController.GetTokenBitmap()));
                    break;
                case DrawLayer.Background:
                    _mapWindowViewModel.BackgroundBitmapSource = _backgroundController.GetBackgroundBitmapSource();
                    _connectionManager.SendMapUpdate(new MapUpdate(DrawLayer.Background, _backgroundController.GetBackgroundBitmap()));
                    break;
                case DrawLayer.GridAndStrokes:
                    var gridAndTokenBitmap = CreateGridAndDrawingBitmap();
                    _mapWindowViewModel.GridBitmapSource = gridAndTokenBitmap.ToBitmapImage();
                    _connectionManager.SendMapUpdate(new MapUpdate(DrawLayer.GridAndStrokes, gridAndTokenBitmap));
                    break;
                case DrawLayer.Tokens:
                    _mapWindowViewModel.TokenBitmapSource = _tokenController.GetTokenBitmapSource();
                    _connectionManager.SendMapUpdate(new MapUpdate(DrawLayer.Tokens, _tokenController.GetTokenBitmap()));
                    break;
            }
        }

        private Bitmap CreateGridAndDrawingBitmap()
        {
            var gridBitmap = IsGridShown ? _gridBitmap : BitmapTools.CreateEmptyBitmap();
            return BitmapTools.CreateGridAndStrokesBitmap(gridBitmap, Strokes, new Size<int>((int)_inkCanvasWidth, (int)_inkCanvasHeight));
        }

        private void WindowClosing()
        {
            _connectionManager.Disconnect();
            _windowService.CloseAllWindows();
        }

        private void PenSizeChanged()
        {
            InkCanvasDrawingAttributes.Width = PenSize;
            InkCanvasDrawingAttributes.Height = PenSize;

            if (EditingMode == InkCanvasEditingMode.EraseByPoint)
            {
                EraserShape = new EllipseStylusShape(PenSize, PenSize);
            }
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
                    EraserShape = new EllipseStylusShape(PenSize, PenSize);
                    break;
            }
        }

        private void InkCanvasSizeOnStartup(double width)
        {
            // It's enought to only use the width, since everything is done in a 16:9 ratio
            _inkCanvasWidth = width;
            _inkCanvasHeight = width / 16 * 9;
            _backgroundController.SetCanvasSize(new Size<double>(_inkCanvasWidth, _inkCanvasHeight));
            _tokenController.SetCanvasSize(new Size<double>(_inkCanvasWidth, _inkCanvasHeight));
        }

        private void ClearMap()
        {
            var confirmationWindowViewModel = new ConfirmationWindowViewModel();
            confirmationWindowViewModel.Content = "Are you sure you want to clear everything?";
            _windowService.ShowWindowDialog<ConfirmationWindow>(confirmationWindowViewModel);

            if (confirmationWindowViewModel.Confirmed)
            {
                Strokes.Clear();
                _backgroundController.ClearBackground();
                _tokenController.ClearTokens();
                IsGridShown = true;
                GridSize = _settings.DefaultGridSize;
                GridCellsWidth = 10;
                GridCellsHeight = 10;
                GridSizeChanged();
            }
        }

        private void OpenSettings()
        {
            var settingsWindowViewModel = new SettingsWindowViewModel(_settings, _windowService);
            _windowService.ShowWindowDialog<SettingsWindow>(settingsWindowViewModel);

            if (settingsWindowViewModel.MonitorChanged)
            {
                _mapWindowViewModel.ChangeWindowPosition(_settings.MonitorPosition.X);
            }

            if (settingsWindowViewModel.MonsterTokensDownloaded)
            {
                _tokenController.ReloadMonsterTokens();
            }
        }

        public void ClearInkCanvas()
        {
            Strokes.Clear();
        }

        public void SelectedTabChanged()
        {
            switch (SelectedTabIndex)
            {
                case TabIndex.Background:
                    InkCanvasVisibility = Visibility.Hidden;
                    MouseInputCanvasVisibility = Visibility.Visible;
                    TokenVisibility = Visibility.Hidden;
                    break;
                case TabIndex.Grid:
                    InkCanvasVisibility = Visibility.Hidden;
                    MouseInputCanvasVisibility = Visibility.Hidden;
                    TokenVisibility = Visibility.Hidden;
                    break;
                case TabIndex.Drawing:
                    InkCanvasVisibility = Visibility.Visible;
                    MouseInputCanvasVisibility = Visibility.Hidden;
                    TokenVisibility = Visibility.Visible;
                    InkCanvasZIndex = 1;
                    break;
                case TabIndex.Tokens:
                    InkCanvasVisibility = Visibility.Visible;
                    MouseInputCanvasVisibility = Visibility.Visible;
                    TokenVisibility = Visibility.Visible;
                    InkCanvasZIndex = 0;
                    break;
            }
        }

        public void GridShownChanged()
        {
            if (IsGridShown)
            {
                GridVisibility = Visibility.Visible;
            }
            else
            {
                GridVisibility = Visibility.Hidden;
            }
            UpdateMap(DrawLayer.GridAndStrokes);
        }

        public void MouseDown()
        {
            switch (SelectedTabIndex)
            {
                case TabIndex.Background:
                    _backgroundController.MouseDown(new Point<double>(MouseInputX, MouseInputY));
                    break;
                case TabIndex.Tokens:
                    _tokenController.MouseDown(new Point<double>(MouseInputX, MouseInputY));
                    break;
            }
        }

        public void MouseUp()
        {
            switch (SelectedTabIndex)
            {
                case TabIndex.Background:
                    _backgroundController.MouseUp(new Point<double>(MouseInputX, MouseInputY));
                    break;
            }
        }

        private void MoveMap(string direction)
        {
            var arrowDirection = Enum.Parse<ArrowDirection>(direction);
            var matrix = new System.Windows.Media.Matrix();
            var windowSize = BitmapTools.GetBitmapSize();
            double gridSize = GridSize;
            var distanceX = gridSize.Map(0, windowSize.Width, 0, _inkCanvasWidth);
            var distanceY = gridSize.Map(0, windowSize.Height, 0, _inkCanvasHeight);

            switch (arrowDirection)
            {
                case ArrowDirection.Up:
                    matrix.Translate(0, distanceY);
                    break;
                case ArrowDirection.Down:
                    matrix.Translate(0, -distanceY);
                    break;
                case ArrowDirection.Left:
                    matrix.Translate(distanceX, 0);
                    break;
                case ArrowDirection.Right:
                    matrix.Translate(-distanceX, 0);
                    break;
            }

            Strokes.Transform(matrix, false);
            UpdateMap(DrawLayer.GridAndStrokes);
            _backgroundController.MoveBackground(arrowDirection, GridSize);
            _tokenController.MoveTokens(arrowDirection);
        }

        private void SaveMap()
        {
            if (_windowService.ShowSaveFileDialog(out string path, "(*.dbm)|*.dbm"))
            {
                var saveFile = new SaveFile();
                saveFile.GridSize = GridSize;
                saveFile.IsGridShown = IsGridShown;
                saveFile.Strokes = Strokes;
                _backgroundController.AddToSaveFile(saveFile);
                _tokenController.AddToSaveFile(saveFile);
                saveFile.Save(path);
            }
        }

        private void OpenMap()
        {
            if (_windowService.ShowOpenFileDialog(out string path, "(*.dbm)|*.dbm"))
            {
                var saveFile = SaveFile.Open(path);
                _backgroundController.OpenSaveFile(saveFile);
                GridSize = saveFile.GridSize;
                GridSizeChanged();
                IsGridShown = saveFile.IsGridShown;
                Strokes = saveFile.Strokes;
                _tokenController.OpenSaveFile(saveFile);
                SelectedTabIndex = TabIndex.Tokens;
            }
        }

        private void Zoom(double newGridSize)
        {
            var currentIsShowMapLocked = IsShowMapLocked;
            IsShowMapLocked = false;
            var zoomFactor = newGridSize / GridSize;

            _backgroundController.Zoom(zoomFactor);

            GridSize = (int)newGridSize;
            GridSizeChanged();

            var matrix = new System.Windows.Media.Matrix();
            matrix.Translate(-(_inkCanvasWidth / 2), -(_inkCanvasHeight / 2));
            matrix.Scale(zoomFactor, zoomFactor);
            matrix.Translate((_inkCanvasWidth / 2), (_inkCanvasHeight / 2));
            Strokes.Transform(matrix, false);

            _tokenController.Zoom(zoomFactor);

            IsShowMapLocked = currentIsShowMapLocked;
        }

        private void UpdateMap(DrawLayer layer)
        {
            if (IsShowMapLocked)
            {
                ShowMap(layer);
            }
        }

        private void ServerConnectionButton()
        {
            ServerConnectionButtonEnabled = false;
            ServerConnectionStatusColor = System.Windows.Media.Brushes.Orange;

            if (ServerConnectionStatus == "Disconnected")
            {
                ServerConnectionStatus = "Connecting...";
                _connectionManager.Connect(_settings.ServerIp, _settings.ServerPort);
            }
            else
            {
                ServerConnectionStatus = "Disconnecting...";
                _connectionManager.Disconnect();
            }
        }

        private void ConnectionManagerDisconnected(object? sender, EventArgs e)
        {
            ServerConnectionButtonText = "Connect";
            ServerConnectionButtonEnabled = true;
            ServerConnectionStatus = "Disconnected";
            ServerConnectionStatusColor = System.Windows.Media.Brushes.Red;
        }

        private void ConnectionManagerConnected(object? sender, EventArgs e)
        {
            ServerConnectionButtonText = "Disconnect";
            ServerConnectionButtonEnabled = true;
            ServerConnectionStatus = "Connected";
            ServerConnectionStatusColor = System.Windows.Media.Brushes.Green;
        }
    }
}
