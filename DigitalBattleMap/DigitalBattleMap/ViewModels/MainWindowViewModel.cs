using DigitalBattleMap.Common;
using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private Bitmap _gridBitmap;
        private IWindowService _windowService;
        private MapWindowViewModel _mapWindowViewModel;
        private Settings _settings;
        private TokenController _tokenController;
        private ConnectionManager _connectionManager;
        private Size<double> _canvasSize;

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

        public int SelectedTabIndex { get => Get<int>(); set => Set(value, SelectedTabChanged); }
        public int GridSize { get => Get<int>(); set => Set(value); }
        public int InkCanvasZIndex { get => Get<int>(); set => Set(value); }
        public bool IsGridShown { get => Get<bool>(); set => Set(value, GridShownChanged); }
        public bool IsShowMapLocked { get => Get<bool>(); set => Set(value, () => UpdateMap(DrawLayer.All)); }
        public bool ServerConnectionButtonEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsConfigurationMenuExpanded { get => Get<bool>(); set => Set(value); }
        public string ServerConnectionButtonText { get => Get<string>(); set => Set(value); }
        public string ServerConnectionStatus { get => Get<string>(); set => Set(value); }
        public Visibility GridVisibility { get => Get<Visibility>(); set => Set(value); }
        public Visibility InkCanvasVisibility { get => Get<Visibility>(); set => Set(value); }
        public Visibility MouseInputCanvasVisibility { get => Get<Visibility>(); set => Set(value); }
        public Visibility TokenVisibility { get => Get<Visibility>(); set => Set(value); }
        public System.Windows.Media.Brush ServerConnectionStatusColor { get => Get<System.Windows.Media.Brush>(); set => Set(value); }

        public BackgroundControllerViewModel BackgroundController { get; set; }
        public DrawingControllerViewModel DrawingController { get; set; }
        public BitmapSource TokenBitmapSource { get => _tokenController.GetTokenBitmapSource(); }
        public BitmapSource TokenSelectionBitmapSource { get => _tokenController.GetTokenSelectionBitmapSource(); }
        public BitmapSource GridBitmapSource { get => _gridBitmap.ToBitmapImage(); }
        public BitmapSource MapArrowUpBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Up).ToBitmapImage(); }
        public BitmapSource MapArrowDownBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Down).ToBitmapImage(); }
        public BitmapSource MapArrowLeftBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Left).ToBitmapImage(); }
        public BitmapSource MapArrowRightBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Right).ToBitmapImage(); }
        public BitmapSource MapZoomInBitmapSource { get => BitmapTools.CreateZoomButton(true).ToBitmapImage(); }
        public BitmapSource MapZoomOutBitmapSource { get => BitmapTools.CreateZoomButton(false).ToBitmapImage(); }
        public ObservableCollection<TokenListItem> TokenList { get => _tokenController.TokenList; }
        public TokenListItem SelectedToken { get => _tokenController.SelectedToken; set => _tokenController.SelectedToken = value; }
        public double MouseInputX { get; set; }
        public double MouseInputY { get; set; }
        public bool IsTokenSelected { get => _tokenController.IsTokenSelected(); }
        public bool IsTokenUpButtonEnabled { get => _tokenController.IsUpButtonEnabled(); }
        public bool IsTokenDownButtonEnabled { get => _tokenController.IsDownButtonEnabled(); }

        public ICommand GridSizeEnterCommand { get; set; }
        public ICommand ShowMapCommand { get; set; }
        public ICommand WindowClosingCommand { get; set; }
        public ICommand InkCanvasSizeOnStartupCommand { get; set; }
        public ICommand ClearAllCommand { get; set; }
        public ICommand SettingsCommand { get; set; }
        public ICommand MouseInputCanvasDownCommand { get; set; }
        public ICommand MouseInputCanvasUpCommand { get; set; }
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
        public ICommand MapZoomInCommand { get; set; }
        public ICommand MapZoomOutCommand { get; set; }
        public ICommand HideConfigurationCommand { get; set; }
        public ICommand SortInitiativeCommand { get; set; }

        public void Initialize()
        {
            InitializeProperties();
            _settings = Settings.Load();
            GridSize = _settings.DefaultGridSize;
            _gridBitmap = BitmapTools.CreateGrid(GridSize);
            BackgroundController = new BackgroundControllerViewModel(_windowService, GridSize);
            BackgroundController.OnBackgroundUpdated += OnBackgroundUpdated;
            DrawingController = new DrawingControllerViewModel(GridSize);
            DrawingController.OnDrawingStrokesUpdated += DrawingStrokesUpdated;
            _tokenController = new TokenController(_windowService, _settings, GridSize);
            _tokenController.OnTokenEditorUpdated += TokenEditorUpdated;
            _tokenController.OnTokenBitmapUpdated += TokenBitmapUpdated;
            _tokenController.OnSelectedTokenBitmapUpdated += SelectedTokenBitmapUpdated;

            _connectionManager = new ConnectionManager();
            _connectionManager.OnConnected += ConnectionManagerConnected;
            _connectionManager.OnDisconnect += ConnectionManagerDisconnected;
            _connectionManager.OnMoveToken += ConnectionManagerOnMoveToken;
            _connectionManager.OnToggleCondition += ConnectionManagerOnToggleCondition;
        }

        private void InitializeProperties()
        {
            IsGridShown = true;
            IsShowMapLocked = false;
            GridVisibility = Visibility.Visible;
            InkCanvasVisibility = Visibility.Hidden;
            MouseInputCanvasVisibility = Visibility.Visible;
            TokenVisibility = Visibility.Hidden;
            ServerConnectionButtonText = "Connect";
            ServerConnectionStatus = "Disconnected";
            ServerConnectionStatusColor = System.Windows.Media.Brushes.Red;
            ServerConnectionButtonEnabled = true;
        }

        protected override void InitializeCommands()
        {
            GridSizeEnterCommand = new RelayCommand(p => GridSizeChanged());
            ShowMapCommand = new RelayCommand(p => ShowMap());
            WindowClosingCommand = new RelayCommand(p => WindowClosing());
            InkCanvasSizeOnStartupCommand = new RelayCommand(p => InkCanvasSizeOnStartup((double)p));
            ClearAllCommand = new RelayCommand(p => ClearMap());
            SettingsCommand = new RelayCommand(p => OpenSettings());
            MouseInputCanvasDownCommand = new RelayCommand(p => MouseDown());
            MouseInputCanvasUpCommand = new RelayCommand(p => MouseUp());
            MoveMapArrowCommand = new RelayCommand(p => MoveMap((string)p));
            SaveMapCommand = new RelayCommand(p => SaveMap());
            OpenMapCommand = new RelayCommand(p => OpenMap());
            AddTokenCommand = new RelayCommand(p => _tokenController.AddToken());
            RemoveTokenCommand = new RelayCommand(p => _tokenController.RemoveToken());
            ClearTokensCommand = new RelayCommand(p => _tokenController.ClearTokens());
            TokenUpCommand = new RelayCommand(p => _tokenController.InitiativeUp());
            TokenDownCommand = new RelayCommand(p => _tokenController.InitiativeDown());
            CustomTokensCommand = new RelayCommand(p => _tokenController.CustomTokens());
            ServerConnectionCommand = new RelayCommand(p => ServerConnectionButton());
            MapZoomInCommand = new RelayCommand(p => Zoom(GridSize + 10));
            MapZoomOutCommand = new RelayCommand(p => Zoom(GridSize - 10));
            HideConfigurationCommand = new RelayCommand(p => { IsConfigurationMenuExpanded = false; });
            SortInitiativeCommand = new RelayCommand(p => _tokenController.SortInitiative());
        }

        private void OnBackgroundUpdated(object? sender, EventArgs e)
        {
            UpdateMap(DrawLayer.Background);
        }

        private void DrawingStrokesUpdated(object? sender, EventArgs e)
        {
            UpdateMap(DrawLayer.GridAndStrokes);
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

        private void ConnectionManagerOnMoveToken(object sender, MoveTokenActionEventArgs e)
        {
            if (IsShowMapLocked)
            {
                _tokenController.OnMoveTokenAction(sender, e);
            }
        }

        private void ConnectionManagerOnToggleCondition(object sender, ToggleConditionActionEventArgs e)
        {
            if (IsShowMapLocked)
            {
                _tokenController.OnToggleConditionAction(sender, e);
            }
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
            BackgroundController.UpdateGridSize(GridSize);
            DrawingController.UpdateGridSize(GridSize);
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
                    _mapWindowViewModel.BackgroundBitmapSource = BackgroundController.BackgroundBitmapSource;
                    _mapWindowViewModel.GridBitmapSource = gridAndTokenBitmapAll.ToBitmapImage();
                    _mapWindowViewModel.TokenBitmapSource = _tokenController.GetTokenBitmapSource();
                    _connectionManager.SendMapUpdate(new MapUpdate{ Layer = DrawLayer.Background, Bitmap = new Bitmap(BackgroundController.GetBackgroundBitmap()) });
                    _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.GridAndStrokes, Bitmap = new Bitmap(gridAndTokenBitmapAll) });
                    _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Tokens, Bitmap = new Bitmap(_tokenController.GetTokenBitmap()) });
                    break;
                case DrawLayer.Background:
                    _mapWindowViewModel.BackgroundBitmapSource = BackgroundController.BackgroundBitmapSource;
                    _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Background, Bitmap = new Bitmap(BackgroundController.GetBackgroundBitmap()) });
                    break;
                case DrawLayer.GridAndStrokes:
                    var gridAndTokenBitmap = CreateGridAndDrawingBitmap();
                    _mapWindowViewModel.GridBitmapSource = gridAndTokenBitmap.ToBitmapImage();
                    _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.GridAndStrokes, Bitmap = new Bitmap(gridAndTokenBitmap) });
                    break;
                case DrawLayer.Tokens:
                    _mapWindowViewModel.TokenBitmapSource = _tokenController.GetTokenBitmapSource();
                    _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Tokens, Bitmap = new Bitmap(_tokenController.GetTokenBitmap()) });
                    break;
            }
        }

        private Bitmap CreateGridAndDrawingBitmap()
        {
            var gridBitmap = IsGridShown ? _gridBitmap : BitmapTools.CreateEmptyBitmap();
            return BitmapTools.CreateGridAndStrokesBitmap(gridBitmap, DrawingController.Strokes, Size<int>.Create(_canvasSize));
        }

        private void WindowClosing()
        {
            _connectionManager.Disconnect();
            _windowService.CloseAllWindows();
        }

        private void InkCanvasSizeOnStartup(double width)
        {
            // It's enough to only use the width, since everything is done in a 16:9 ratio
            _canvasSize = new Size<double>();
            _canvasSize.Width = width;
            _canvasSize.Height = width / 16 * 9;

            BackgroundController.SetCanvasSize(_canvasSize);
            DrawingController.SetCanvasSize(_canvasSize);
            _tokenController.SetCanvasSize(_canvasSize);
        }

        private void ClearMap()
        {
            var confirmationWindowViewModel = new ConfirmationWindowViewModel();
            confirmationWindowViewModel.Content = "Are you sure you want to clear everything?";
            _windowService.ShowWindowDialog<ConfirmationWindow>(confirmationWindowViewModel);

            if (confirmationWindowViewModel.Confirmed)
            {
                BackgroundController.ClearBackground();
                DrawingController.ClearDrawings();
                _tokenController.ClearTokens();
                IsGridShown = true;
                GridSize = _settings.DefaultGridSize;
                GridSizeChanged();

                _connectionManager.ClearMap();
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
                    BackgroundController.MouseDown(new Point<double>(MouseInputX, MouseInputY));
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
                    BackgroundController.MouseUp(new Point<double>(MouseInputX, MouseInputY));
                    break;
            }
        }

        private void MoveMap(string direction)
        {
            var arrowDirection = Enum.Parse<ArrowDirection>(direction);
            BackgroundController.Move(arrowDirection);
            DrawingController.Move(arrowDirection); 
            _tokenController.MoveTokens(arrowDirection);
        }

        private void SaveMap()
        {
            if (_windowService.ShowSaveFileDialog(out string path, "(*.dbm)|*.dbm"))
            {
                var saveFile = new SaveFile();
                saveFile.GridSize = GridSize;
                saveFile.IsGridShown = IsGridShown;
                BackgroundController.AddToSaveFile(saveFile);
                DrawingController.AddToSaveFile(saveFile);
                _tokenController.AddToSaveFile(saveFile);
                saveFile.Save(path);
            }
        }

        private void OpenMap()
        {
            if (_windowService.ShowOpenFileDialog(out string path, "(*.dbm)|*.dbm"))
            {
                var currentIsShowMapLocked = IsShowMapLocked;
                IsShowMapLocked = false;

                var saveFile = SaveFile.Open(path);
                BackgroundController.OpenSaveFile(saveFile);
                GridSize = saveFile.GridSize;
                GridSizeChanged();
                IsGridShown = saveFile.IsGridShown;
                DrawingController.OpenSaveFile(saveFile);
                _tokenController.OpenSaveFile(saveFile);
                SelectedTabIndex = TabIndex.Tokens;

                IsShowMapLocked = currentIsShowMapLocked;
            }
        }

        private void Zoom(double newGridSize)
        {
            var currentIsShowMapLocked = IsShowMapLocked;
            IsShowMapLocked = false;
            var zoomFactor = newGridSize / GridSize;

            BackgroundController.Zoom(zoomFactor);

            GridSize = (int)newGridSize;
            GridSizeChanged();

            DrawingController.Zoom(zoomFactor);
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
                _connectionManager.Connect(_settings.ServerAddress);
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
