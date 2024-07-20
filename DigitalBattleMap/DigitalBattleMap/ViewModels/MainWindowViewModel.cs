using DigitalBattleMap.Common;
using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace DigitalBattleMap.ViewModels;

public class MainWindowViewModel : ViewModelBase, IMapSize
{
    private IWindowService _windowService;
    private MapWindowViewModel _mapWindowViewModel;
    private Settings _settings;
    private ConnectionManager _connectionManager;
    private Size<double> _canvasSize;
    private Action _multiMoveAction;
    private MonsterTokens _monsterTokens = new();
    private int? _returnStepsX;
    private int? _returnStepsY;
    private int? _returnGridSize;

    public MainWindowViewModel(IWindowService windowService)
    {
        _windowService = windowService;
        Initialize();
        OpenMapWindow();
    }

    public MainWindowViewModel()
    {
        // This is required to render MainWindow in editor
        IO.Initialize(new Directory(), new File(), new ZipFile());
        CampaignController = new();
        BackgroundController = new();
        DrawingController = new();
        TokenController = new();
        InitializeProperties();
    }

    public event EventHandler OnGridSizeChanged;
    public event EventHandler OnGridSizeZoomAndEnhance;
    public event EventHandler<CanvasSizeChangedEventArgs> OnCanvasSizeChanged;


    public int SelectedTabIndex { get => Get<int>(); set => Set(value, SelectedTabChanged); }
    public int SelectedMapTabIndex { get => Get<int>(); set => Set(value); }
    public int DrawingCanvasZIndex { get => Get<int>(); set => Set(value); }
    public int MultiMoveCount { get => Get<int>(); set => Set(value); }
    public bool IsShowMapLocked { get => Get<bool>(); set => Set(value, IsShowMapLockedChanged); }
    public bool ServerConnectionButtonEnabled { get => Get<bool>(); set => Set(value); }
    public bool IsConfigurationMenuExpanded { get => Get<bool>(); set => Set(value); }
    public bool IsMultiMove { get => Get<bool>(); set => Set(value); }
    public bool HideDungeonMasterFeatures { get => Get<bool>(); set => Set(value); }
    public string ServerConnectionButtonText { get => Get<string>(); set => Set(value); }
    public string ServerConnectionStatus { get => Get<string>(); set => Set(value); }
    public Visibility DrawingCanvasVisibility { get => Get<Visibility>(); set => Set(value); }
    public Visibility MouseInputCanvasVisibility { get => Get<Visibility>(); set => Set(value); }
    public Visibility TokenVisibility { get => Get<Visibility>(); set => Set(value); }
    public System.Windows.Media.Brush ServerConnectionStatusColor { get => Get<System.Windows.Media.Brush>(); set => Set(value); }
    public MouseCanvasViewModel MouseCanvas { get => Get<MouseCanvasViewModel>(); set => Set(value); }

    public CampaignControllerViewModel CampaignController { get; set; }
    public BackgroundControllerViewModel BackgroundController { get; set; }
    public DrawingControllerViewModel DrawingController { get; set; }
    public TokenControllerViewModel TokenController { get; set; }
    public BitmapSource MapArrowUpBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Up).ToBitmapImage(); }
    public BitmapSource MapArrowDownBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Down).ToBitmapImage(); }
    public BitmapSource MapArrowLeftBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Left).ToBitmapImage(); }
    public BitmapSource MapArrowRightBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Right).ToBitmapImage(); }
    public BitmapSource MapZoomInBitmapSource { get => BitmapTools.CreateZoomButton(true).ToBitmapImage(); }
    public BitmapSource MapZoomOutBitmapSource { get => BitmapTools.CreateZoomButton(false).ToBitmapImage(); }
    public BitmapSource MapCropBitmapSource { get => BitmapTools.CreateCropButton().ToBitmapImage(); }
    public BitmapSource MapZoomBackgroundBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.UndoIcon.png")).ToBitmapImage(); }
    public BitmapSource CampaignEmblemBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.CampaignEmblem.png")).ToBitmapImage(); }
    public BitmapSource BackgroundEmblemBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.BackgroundEmblem.png")).ToBitmapImage(); }
    public BitmapSource DrawingEmblemBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.DrawingEmblem.png")).ToBitmapImage(); }
    public BitmapSource TokenEmblemBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.TokenEmblem.png")).ToBitmapImage(); }

    int IMapSize.Width => Constants.MapSize.Width;
    int IMapSize.Height => Constants.MapSize.Height;
    int IMapSize.GridSize => BackgroundController.GridSize;
    double IMapSize.CanvasWidth => _canvasSize.Width;
    double IMapSize.CanvasHeight => _canvasSize.Height;
    double IMapSize.CanvasGridSize => CalculateCanvasGridSize();

    public ICommand ShowMapCommand { get; set; }
    public ICommand WindowClosingCommand { get; set; }
    public ICommand CanvasSizeOnStartupCommand { get; set; }
    public ICommand CanvasSizeChangedCommand { get; set; }
    public ICommand ClearAllCommand { get; set; }
    public ICommand SettingsCommand { get; set; }
    public ICommand MoveMapArrowCommand { get; set; }
    public ICommand SaveMapCommand { get; set; }
    public ICommand OpenMapCommand { get; set; }
    public ICommand ServerConnectionCommand { get; set; }
    public ICommand MapZoomInCommand { get; set; }
    public ICommand MapZoomOutCommand { get; set; }
    public ICommand MapCropCommand { get; set; }
    public ICommand MapZoomBackgroundCommand { get; set; }
    public ICommand HideConfigurationCommand { get; set; }
    public ICommand KeyDownCommand { get; set; }
    public ICommand KeyUpCommand { get; set; }

    public void Initialize()
    {
        InitializeProperties();
        _settings = Settings.Load();
        _settings.OnSettingChanged += SettingChanged;
        _monsterTokens.ReloadTokens();
        _connectionManager = new ConnectionManager();
        _connectionManager.OnConnected += ConnectionManagerConnected;
        _connectionManager.OnDisconnect += ConnectionManagerDisconnected;
        CampaignController = new CampaignControllerViewModel(_windowService, _connectionManager, _monsterTokens, _settings);
        BackgroundController = new BackgroundControllerViewModel(_windowService, this, _settings);
        BackgroundController.OnGridSizeChanged += OnBackgroundGridSizeChanged;
        BackgroundController.OnGridSizeZoomAndEnhance += OnBackgroundGridSizeZoomAndEnhance;
        BackgroundController.OnBackgroundUpdated += OnBackgroundUpdated;
        TokenController = new TokenControllerViewModel(_windowService, _connectionManager, this, _monsterTokens, CampaignController, _settings);
        TokenController.OnGridSizeZoomAndEnhance += OnBackgroundGridSizeZoomAndEnhance;
        TokenController.OnTokenBitmapUpdated += TokenBitmapUpdated;
        DrawingController = new DrawingControllerViewModel(this, TokenController);
        DrawingController.OnGridSizeZoomAndEnhance += OnBackgroundGridSizeZoomAndEnhance;
        DrawingController.OnDrawingShapesUpdated += DrawingShapesUpdated;
        HideDungeonMasterFeatures = _settings.HideDungeonMasterFeatures;
    }

    private void InitializeProperties()
    {
        DrawingCanvasVisibility = Visibility.Hidden;
        MouseInputCanvasVisibility = Visibility.Hidden;
        TokenVisibility = Visibility.Hidden;
        ServerConnectionButtonText = "Connect";
        ServerConnectionStatus = "Disconnected";
        ServerConnectionStatusColor = System.Windows.Media.Brushes.Red;
        ServerConnectionButtonEnabled = true;
    }

    protected override void InitializeCommands()
    {
        ShowMapCommand = new RelayCommand(p => ShowMap());
        WindowClosingCommand = new RelayCommand(p => WindowClosing());
        CanvasSizeOnStartupCommand = new RelayCommand(p => SetCanvasSize((double)p));
        CanvasSizeChangedCommand = new RelayCommand(p => CanvasSizeChanged((SizeChangedEventArgs)p));
        ClearAllCommand = new RelayCommand(p => ClearMap());
        SettingsCommand = new RelayCommand(p => OpenSettings());
        MoveMapArrowCommand = new RelayCommand(p => MoveMap((string)p));
        SaveMapCommand = new RelayCommand(p => SaveMap());
        OpenMapCommand = new RelayCommand(p => OpenMap());
        ServerConnectionCommand = new RelayCommand(p => ServerConnectionButton());
        MapZoomInCommand = new RelayCommand(p => ZoomIn());
        MapZoomOutCommand = new RelayCommand(p => ZoomOut());
        MapCropCommand = new RelayCommand(p => SetCropMode());
        MapZoomBackgroundCommand = new RelayCommand(p => ZoomBackground());
        HideConfigurationCommand = new RelayCommand(p => { IsConfigurationMenuExpanded = false; });
        KeyDownCommand = new RelayCommand(p => KeyDown((KeyEventArgs)p));
        KeyUpCommand = new RelayCommand(p => KeyUp((KeyEventArgs)p));
    }

    private void OnBackgroundGridSizeChanged(object? sender, GridSizeChangedEventArgs e)
    {
        OnGridSizeChanged?.Invoke(this, new EventArgs());
        UpdateMap(DrawLayer.GridAndStrokes);
    }

    private void OnBackgroundGridSizeZoomAndEnhance(object? sender, GridSizeZoomAndEnhanceEventArgs e)
    {
        var selectedArea = e.rectangle;

        // Move to the Middle of the selected area (in steps of Gridsize)
        MoveToMiddle(selectedArea);

        // Zoom
        var currentGridSize = BackgroundController.GridSize;
        var ratio = GetSize().Width / selectedArea.Width;
        var newGridSize = (int)Math.Round(currentGridSize * ratio);
        var zoomSize = newGridSize - currentGridSize;

        _returnGridSize = currentGridSize;
        Zoom(zoomSize);

        MouseCanvas.ResetSelection();
        MouseCanvas.ResetMode();
    }

    private void MoveToMiddle(RectangleF selectedArea)
    {
        var middleOfScreenX = GetSize().Width / 2.0;
        var middleOfScreenY = GetSize().Height / 2.0;
        var middleOfSelectionX = selectedArea.X + (selectedArea.Width / 2.0);
        var middleOfSelectionY = selectedArea.Y + (selectedArea.Height / 2.0);

        var stepsX = (int)Math.Round((middleOfSelectionX - middleOfScreenX) / CalculateCanvasGridSize());
        var stepsY = (int)Math.Round((middleOfSelectionY - middleOfScreenY) / CalculateCanvasGridSize());
        _returnStepsX = -stepsX;
        _returnStepsY = -stepsY;

        MoveMap(stepsX, stepsY);
    }

    private void OnBackgroundUpdated(object? sender, EventArgs e)
    {
        UpdateMap(DrawLayer.Background);
    }

    private void DrawingShapesUpdated(object? sender, EventArgs e)
    {
        UpdateMap(DrawLayer.GridAndStrokes);
    }

    private void TokenBitmapUpdated(object? sender, EventArgs e)
    {
        UpdateMap(DrawLayer.Tokens);
    }

    public void OpenMapWindow()
    {
        _mapWindowViewModel = new MapWindowViewModel();
        _windowService.ShowWindow<MapWindow>(_mapWindowViewModel);
        _mapWindowViewModel.ChangeWindowPosition(_settings.MonitorPosition.X);

        if (!_settings.ShowMapWindow)
        {
            _windowService.HideWindow(_mapWindowViewModel);
        }
    }

    private void ShowMap(DrawLayer drawing = 0)
    {
        switch (drawing)
        {
            case DrawLayer.All:
                var gridAndTokenBitmapAll = CreateGridAndDrawingBitmap();
                _mapWindowViewModel.BackgroundBitmapSource = BackgroundController.GetBackGroundBitmapSource();
                _mapWindowViewModel.GridBitmapSource = gridAndTokenBitmapAll.ToBitmapImage();
                _mapWindowViewModel.TokenBitmapSource = TokenController.TokenBitmapSource;
                _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Background, Bitmap = new Bitmap(BackgroundController.GetBackgroundBitmap()) });
                _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.GridAndStrokes, Bitmap = new Bitmap(gridAndTokenBitmapAll) });
                _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Tokens, Bitmap = new Bitmap(TokenController.GetTokenBitmap()) });
                break;
            case DrawLayer.Background:
                _mapWindowViewModel.BackgroundBitmapSource = BackgroundController.GetBackGroundBitmapSource();
                _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Background, Bitmap = new Bitmap(BackgroundController.GetBackgroundBitmap()) });
                break;
            case DrawLayer.GridAndStrokes:
                var gridAndTokenBitmap = CreateGridAndDrawingBitmap();
                _mapWindowViewModel.GridBitmapSource = gridAndTokenBitmap.ToBitmapImage();
                _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.GridAndStrokes, Bitmap = new Bitmap(gridAndTokenBitmap) });
                break;
            case DrawLayer.Tokens:
                _mapWindowViewModel.TokenBitmapSource = TokenController.TokenBitmapSource;
                _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Tokens, Bitmap = new Bitmap(TokenController.GetTokenBitmap()) });
                break;
        }
    }

    private Bitmap CreateGridAndDrawingBitmap()
    {
        return BitmapTools.MergeBitmaps(new() { BackgroundController.GetGridBitmap(), DrawingController.GetDrawingBitmap() });
    }

    private void WindowClosing()
    {
        _connectionManager.Disconnect();
        _windowService.CloseAllWindows();
    }

    private void SetCanvasSize(double width)
    {
        var sizeChangedEventArgs = new CanvasSizeChangedEventArgs
        {
            OldSize = _canvasSize
        };

        // It's enough to only use the width, since everything is done in a 16:9 ratio
        _canvasSize = new Size<double>
        {
            Width = width,
            Height = width / 16 * 9
        };

        sizeChangedEventArgs.NewSize = _canvasSize;
        OnCanvasSizeChanged?.Invoke(this, sizeChangedEventArgs);
    }

    private void CanvasSizeChanged(SizeChangedEventArgs eventArgs)
    {
        SetCanvasSize(eventArgs.NewSize.Width);
    }

    private void ClearMap()
    {
        var confirmed = false;
        var confirmationWindowViewModel = new ConfirmationWindowViewModel
        {
            Content = "Are you sure you want to clear everything?",
            LeftButtonAction = () => { confirmed = true; }
        };
        _windowService.ShowWindowDialog<ConfirmationWindow>(confirmationWindowViewModel);

        if (confirmed)
        {
            BackgroundController.ClearBackground();
            DrawingController.ClearDrawings();
            TokenController.ClearTokens();

            _connectionManager.ClearMap();
        }
    }

    private void OpenSettings()
    {
        var settingsWindowViewModel = new SettingsWindowViewModel(_settings, _windowService);
        _windowService.ShowWindowDialog<SettingsWindow>(settingsWindowViewModel);

        if (settingsWindowViewModel.MonsterTokensDownloaded)
        {
            _monsterTokens.ReloadTokens();
        }
    }

    public void SelectedTabChanged()
    {
        switch (SelectedTabIndex)
        {
            case TabIndex.Campaign:
                MouseInputCanvasVisibility = Visibility.Hidden;
                DrawingCanvasVisibility = Visibility.Visible;
                TokenVisibility = Visibility.Visible;
                DrawingCanvasZIndex = 0;
                break;
            case TabIndex.Background:
                DrawingCanvasVisibility = Visibility.Hidden;
                MouseInputCanvasVisibility = Visibility.Visible;
                MouseCanvas = BackgroundController.MouseCanvas;
                TokenVisibility = Visibility.Hidden;
                break;
            case TabIndex.Drawing:
                DrawingCanvasVisibility = Visibility.Visible;
                MouseInputCanvasVisibility = Visibility.Visible;
                MouseCanvas = DrawingController.MouseCanvas;
                TokenVisibility = Visibility.Visible;
                DrawingCanvasZIndex = 1;
                break;
            case TabIndex.Tokens:
                DrawingCanvasVisibility = Visibility.Visible;
                MouseInputCanvasVisibility = Visibility.Visible;
                MouseCanvas = TokenController.MouseCanvas;
                TokenVisibility = Visibility.Visible;
                DrawingCanvasZIndex = 0;
                break;
        }
    }

    public Size<int> GetSize()
    {
        return Constants.MapSize;
    }

    public Size<double> GetCanvasSize()
    {
        return _canvasSize;
    }

    private void MoveMap(string direction)
    {
        var arrowDirection = Enum.Parse<ArrowDirection>(direction);
        MoveMap(arrowDirection);
    }

    private void MoveMap(ArrowDirection arrowDirection)
    {
        if (!IsMultiMove)
        {
            BackgroundController.Move(arrowDirection, 1);
            DrawingController.Move(arrowDirection, 1);
            TokenController.Move(arrowDirection, 1);
        }
        else
        {
            MultiMoveCount++;
            _multiMoveAction = () =>
            {
                BackgroundController.Move(arrowDirection, MultiMoveCount);
                DrawingController.Move(arrowDirection, MultiMoveCount);
                TokenController.Move(arrowDirection, MultiMoveCount);
            };
        }
    }

    private void MoveMap(int stepsX, int stepsY)
    {
        var directionX = stepsX > 0 ? ArrowDirection.Right : ArrowDirection.Left;
        var directionY = stepsY > 0 ? ArrowDirection.Down : ArrowDirection.Up;
        stepsX = Math.Abs(stepsX);
        stepsY = Math.Abs(stepsY);

        BackgroundController.Move(directionX, stepsX);
        BackgroundController.Move(directionY, stepsY);
        DrawingController.Move(directionX, stepsX);
        DrawingController.Move(directionY, stepsY);
        TokenController.Move(directionX, stepsX);
        TokenController.Move(directionY, stepsY);
    }

    private void SaveMap()
    {
        if (_windowService.ShowSaveFileDialog(out string path, filter: "(*.dbm)|*.dbm"))
        {
            var saveFile = new SaveFile();
            saveFile.CanvasSize = GetCanvasSize();
            BackgroundController.AddToSaveFile(saveFile);
            DrawingController.AddToSaveFile(saveFile);
            TokenController.AddToSaveFile(saveFile);
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
            DrawingController.OpenSaveFile(saveFile);
            TokenController.OpenSaveFile(saveFile);

            DrawingController.OpenObjectLinks(saveFile.ObjectLinks);
            TokenController.OpenObjectLinks(saveFile.ObjectLinks);

            SelectedTabIndex = TabIndex.Tokens;
            SelectedMapTabIndex = 0;

            IsShowMapLocked = currentIsShowMapLocked;
        }
    }

    private void ZoomIn()
    {
        if (!IsMultiMove)
        {
            Zoom(BackgroundController.ZoomSize);
        }
        else
        {
            MultiMoveCount++;
            _multiMoveAction = () =>
            {
                Zoom(BackgroundController.ZoomSize * MultiMoveCount);
            };
        }
    }

    private void ZoomOut()
    {
        if (!IsMultiMove)
        {
            Zoom(-BackgroundController.ZoomSize);
        }
        else
        {
            MultiMoveCount++;
            _multiMoveAction = () =>
            {
                Zoom(-BackgroundController.ZoomSize * MultiMoveCount);
            };
        }
    }

    private void SetCropMode()
    {
        if (BackgroundController.GetFullBackgroundBitmap() != null)
        {
            if (MouseCanvas.GetMode() != MouseCanvasMode.FixedRatioRectangleSelection)
            {
                MouseCanvas.SetMode(MouseCanvasMode.FixedRatioRectangleSelection);
            }
            else
            {
                MouseCanvas.ResetSelection();
            }
        }
    }

    // TODO: improve by showing full background image
    private void ZoomBackground()
    {
        if (BackgroundController.GetFullBackgroundBitmap() == null
            || _returnGridSize == null
            || _returnStepsX == null
            || _returnStepsY == null)
        {
            return;
        }

        MoveMap((int)_returnStepsX, (int)_returnStepsY);

        var zoomSize = _returnGridSize - BackgroundController.GridSize;
        Zoom((int)zoomSize);

        _returnGridSize = null;
        _returnStepsX = null;
        _returnStepsY = null;
    }

    private void Zoom(int gridSizeChange)
    {
        var oldGridSize = BackgroundController.GridSize;
        var currentIsShowMapLocked = IsShowMapLocked;
        IsShowMapLocked = false;
        BackgroundController.UpdateGridSize(gridSizeChange);

        var zoomFactor = (double)BackgroundController.GridSize / (double)oldGridSize;
        BackgroundController.Zoom(zoomFactor);
        DrawingController.Zoom(zoomFactor);
        TokenController.Zoom(zoomFactor);

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

    private void ConnectionManagerDisconnected(object? sender, DisconnectedEventArgs e)
    {
        ServerConnectionButtonText = "Connect";
        ServerConnectionButtonEnabled = true;
        ServerConnectionStatus = "Disconnected";
        ServerConnectionStatusColor = System.Windows.Media.Brushes.Red;

        if (e.IsConnectionLost)
        {
            var confirmationWindowViewModel = new ConfirmationWindowViewModel
            {
                Content = "Connection with the server was lost!",
                IsLeftButtonVisible = false,
                IsRightButtonVisible = false,
                IsMiddleButtonVisible = true
            };

            Application.Current.Dispatcher.Invoke(() =>
            {
                _windowService.ShowWindowDialog<ConfirmationWindow>(confirmationWindowViewModel);
            });
        }
    }

    private void ConnectionManagerConnected(object? sender, EventArgs e)
    {
        ServerConnectionButtonText = "Disconnect";
        ServerConnectionButtonEnabled = true;
        ServerConnectionStatus = "Connected";
        ServerConnectionStatusColor = System.Windows.Media.Brushes.Green;
    }

    private void IsShowMapLockedChanged()
    {
        _connectionManager?.UpdatePlayerControlAllowed(IsShowMapLocked);
        UpdateMap(DrawLayer.All);
    }

    private void KeyDown(KeyEventArgs keyEventArgs)
    {
        if (keyEventArgs.Key == Key.LeftShift && !IsMultiMove)
        {
            IsMultiMove = true;
            MultiMoveCount = 0;
        }
    }

    private void KeyUp(KeyEventArgs keyEventArgs)
    {
        if (keyEventArgs.Key == Key.LeftShift && IsMultiMove)
        {
            IsMultiMove = false;
            if (_multiMoveAction != null)
            {
                _multiMoveAction();
                _multiMoveAction = null;
            }
        }
    }

    private void SettingChanged(object? sender, SettingChangedEventArgs e)
    {
        if (e.SettingName == nameof(Settings.MonitorPosition))
        {
            _mapWindowViewModel.ChangeWindowPosition(_settings.MonitorPosition.X);
        }
        else if (e.SettingName == nameof(Settings.ShowMapWindow))
        {
            if (_settings.ShowMapWindow)
            {
                _windowService.ShowWindow(_mapWindowViewModel);
            }
            else
            {
                _windowService.HideWindow(_mapWindowViewModel);
            }
        }
        else if (e.SettingName == nameof(Settings.HideDungeonMasterFeatures))
        {
            HideDungeonMasterFeatures = _settings.HideDungeonMasterFeatures;
        }
    }

    private double CalculateCanvasGridSize()
    {
        var gridSize = (double)BackgroundController.GridSize;
        return gridSize.Map(0.0, Constants.MapSize.Width, 0.0, _canvasSize.Width);
    }
}
