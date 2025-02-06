using DigitalBattleMap.Common;
using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using ArrowDirection = DigitalBattleMap.DataClasses.ArrowDirection;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

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
    public event EventHandler<CanvasSizeChangedEventArgs> OnCanvasSizeChanged;

    public int SelectedTabIndex { get => Get<int>(); set => Set(value, SelectedTabChanged); }
    public int SelectedMapTabIndex { get => Get<int>(); set => SetWhenChanged(value, GenerateMapOverview); }
    public int DrawingCanvasZIndex { get => Get<int>(); set => Set(value); }
    public int MultiMoveCount { get => Get<int>(); set => Set(value); }
    public bool IsShowMapLocked { get => Get<bool>(); set => Set(value, IsShowMapLockedChanged); }
    public bool IsBackgroundOpen { get => Get<bool>(); set => Set(value); }
    public bool ServerConnectionButtonEnabled { get => Get<bool>(); set => Set(value); }
    public bool IsConfigurationMenuExpanded { get => Get<bool>(); set => Set(value); }
    public bool IsMultiMove { get => Get<bool>(); set => Set(value); }
    public bool HideDungeonMasterFeatures { get => Get<bool>(); set => Set(value); }
    public bool HasBlackBackground { get => Get<bool>(); set => Set(value); }
    public string ServerConnectionButtonText { get => Get<string>(); set => Set(value); }
    public string ServerConnectionStatus { get => Get<string>(); set => Set(value); }
    public Visibility DrawingCanvasVisibility { get => Get<Visibility>(); set => Set(value); }
    public Visibility MouseInputCanvasVisibility { get => Get<Visibility>(); set => Set(value); }
    public Visibility TokenVisibility { get => Get<Visibility>(); set => Set(value); }
    public System.Windows.Media.Brush ServerConnectionStatusColor { get => Get<System.Windows.Media.Brush>(); set => Set(value); }
    public System.Windows.Media.Brush CropColor { get => Get<System.Windows.Media.Brush>(); set => Set(value); }
    public MouseCanvasViewModel MouseCanvas { get => Get<MouseCanvasViewModel>(); set => Set(value); }

    public CampaignControllerViewModel CampaignController { get; set; }
    public BackgroundControllerViewModel BackgroundController { get; set; }
    public DrawingControllerViewModel DrawingController { get; set; }
    public TokenControllerViewModel TokenController { get; set; }
    public MapOverviewViewModel MapOverview { get; set; }
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
        MapOverview = new MapOverviewViewModel(this);
        MapOverview.OnGridSizeZoomAndEnhance += OnBackgroundGridSizeZoomAndEnhance;
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
        HasBlackBackground = _settings.HasBlackBackground;
        CropColor = System.Windows.Media.Brushes.LightGray;
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

        // Zoom to new grid size
        var ratio = _canvasSize.Width / selectedArea.Width;
        var newGridSize = (int)Math.Round((double)BackgroundController.GridSize * ratio, 0);
        newGridSize = Math.Min(newGridSize, Constants.MaxGridSize);

        _returnGridSize = BackgroundController.GridSize;
        ZoomToGridSize(newGridSize);

        // Reset mouse canvas
        MouseCanvas.ResetSelection();
        MouseCanvas.ResetMode();
        MapOverview.MouseCanvas.ResetSelection();
        MapOverview.MouseCanvas.ResetMode();
        CropColor = System.Windows.Media.Brushes.LightGray;
        SelectedMapTabIndex = TabMapIndex.Map;
    }

    private void MoveToMiddle(RectangleF selectedArea)
    {
        // find center of screen and selection
        var middleOfScreenX = _canvasSize.Width / 2.0;
        var middleOfScreenY = _canvasSize.Height / 2.0;
        var middleOfSelectionX = selectedArea.X + (selectedArea.Width / 2.0);
        var middleOfSelectionY = selectedArea.Y + (selectedArea.Height / 2.0);

        // find number of positive or negative steps need to reach the center of selection
        var stepsX = (int)Math.Round((middleOfSelectionX - middleOfScreenX) / CalculateCanvasGridSize());
        var stepsY = (int)Math.Round((middleOfSelectionY - middleOfScreenY) / CalculateCanvasGridSize());

        _returnStepsX = -stepsX;
        _returnStepsY = -stepsY;
        MoveMap(stepsX, stepsY);
    }

    private void OnBackgroundUpdated(object? sender, EventArgs e)
    {
        IsBackgroundOpen = BackgroundController.HasOpenedBackground;
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
                _mapWindowViewModel.BackgroundBitmapSource = GetBackgroundBitmapSource();
                _mapWindowViewModel.GridBitmapSource = gridAndTokenBitmapAll.ToBitmapImage();
                _mapWindowViewModel.TokenBitmapSource = TokenController.TokenBitmapSource;
                _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Background, Bitmap = new Bitmap(GetBackgroundBitmap()) });
                _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.GridAndStrokes, Bitmap = new Bitmap(gridAndTokenBitmapAll) });
                _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Tokens, Bitmap = new Bitmap(TokenController.GetTokenBitmap()) });
                break;
            case DrawLayer.Background:
                _mapWindowViewModel.BackgroundBitmapSource = GetBackgroundBitmapSource();
                _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Background, Bitmap = new Bitmap(GetBackgroundBitmap()) });
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

    private BitmapSource GetBackgroundBitmapSource()
    {
        if (HasBlackBackground)
        {
            return BitmapTools.MergeBitmaps(BitmapTools.CreateBlackBitmap(), BackgroundController.GetBackgroundBitmap()).ToBitmapImage();
        }
        return BackgroundController.GetBackgroundBitmapSource();
    }

    private Bitmap GetBackgroundBitmap()
    {
        if (HasBlackBackground)
        {
            return BitmapTools.MergeBitmaps(BitmapTools.CreateBlackBitmap(), BackgroundController.GetBackgroundBitmap());
        }
        return BackgroundController.GetBackgroundBitmap();
    }

    private Bitmap CreateGridAndDrawingBitmap()
    {
        return BitmapTools.MergeBitmaps(BackgroundController.GetGridBitmap(), DrawingController.GetDrawingBitmap());
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

            GenerateMapOverview();
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

    private void SelectedTabChanged()
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

        BackgroundController.Move(directionX, stepsX, false);
        BackgroundController.Move(directionY, stepsY, false);
        DrawingController.Move(directionX, stepsX, false);
        DrawingController.Move(directionY, stepsY, false);
        TokenController.Move(directionX, stepsX, false);
        TokenController.Move(directionY, stepsY, false);
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
            OpenMap(path);
        }
    }

    private void OpenMap(string path)
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
            if(SelectedMapTabIndex == TabMapIndex.Map)
            {
                if (MouseCanvas.GetMode() != MouseCanvasMode.FixedRatioRectangleSelection)
                {
                    MouseCanvas.SetMode(MouseCanvasMode.FixedRatioRectangleSelection);
                    CropColor = System.Windows.Media.Brushes.LightBlue;
                }
                else
                {
                    MouseCanvas.ResetSelection();
                    CropColor = System.Windows.Media.Brushes.LightGray;
                }
            }
            else if (SelectedMapTabIndex == TabMapIndex.Overview)
            {
                MapOverview.MouseCanvas.SetMode(MouseCanvasMode.FixedRatioRectangleSelection);
            }
        }
    }

    private void ZoomBackground()
    {
        if (BackgroundController.GetFullBackgroundBitmap() != null
            && _returnGridSize != null
            && _returnStepsX != null
            && _returnStepsY != null)
        {
            MoveMap((int)_returnStepsX, (int)_returnStepsY);
            ZoomToGridSize((int)_returnGridSize);

            _returnGridSize = null;
            _returnStepsX = null;
            _returnStepsY = null;
        }
        else
        {
            ReturnToCenterOfFullBackground();
            ReturnToZoomOfFullBackground();
        }
    }

    private void ReturnToCenterOfFullBackground()
    {
        // find center of canvas area (background)
        var background = BackgroundController.GetArea();
        var middleOfScreenX = background.X + (background.Width / 2.0);
        var middleOfScreenY = background.Y + (background.Height / 2.0);
        // find center of background full
        var backgroundFull = BackgroundController.GetFullBackgroundBitmap();
        var middleOfCanvasX = backgroundFull.Width / 2.0;
        var middleOfCanvasY = backgroundFull.Height / 2.0;

        double preciseGridSize = BackgroundController.GridSize;
        var mapSizeWidth = Constants.MapSize.Width;
        var mapSizeHeight = Constants.MapSize.Height;
        var areaWidth = background.Width;
        var areaHeight = background.Height;
        var distanceX = (int)Math.Round(preciseGridSize.Map(0, mapSizeWidth, 0, areaWidth));
        var distanceY = (int)Math.Round(preciseGridSize.Map(0, mapSizeHeight, 0, areaHeight));

        var stepsX = (int)Math.Round((middleOfCanvasX - middleOfScreenX) / distanceX);
        var stepsY = (int)Math.Round((middleOfCanvasY - middleOfScreenY) / distanceY);
        MoveMap(stepsX, stepsY);
    }

    private void ReturnToZoomOfFullBackground()
    {
        double ratio = 0.0;
        var background = BackgroundController.GetArea();
        var backgroundFull = BackgroundController.GetFullBackgroundBitmap();
        if (backgroundFull.Width / backgroundFull.Height >= 1.78)
        {
            ratio = background.Width / backgroundFull.Width;
        }
        else
        {
            ratio = background.Height / backgroundFull.Height;
        }
        var newGridSize = (int)Math.Round((double)BackgroundController.GridSize * ratio, 0);
        ZoomToGridSize(newGridSize);
    }

    private void ZoomToGridSize(int newGridSize)
    {
        var gridSizeChange = newGridSize - BackgroundController.GridSize;
        Zoom(gridSizeChange, false);
    }

    private void Zoom(int gridSizeChange, bool update = true)
    {
        var oldGridSize = BackgroundController.GridSize;
        var currentIsShowMapLocked = IsShowMapLocked;
        IsShowMapLocked = false;
        BackgroundController.UpdateGridSize(gridSizeChange, update);

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
        else if (e.SettingName == nameof(Settings.HasBlackBackground))
        {
            HasBlackBackground = _settings.HasBlackBackground;
        }
    }

    private double CalculateCanvasGridSize()
    {
        var gridSize = (double)BackgroundController.GridSize;
        return gridSize.Map(0.0, Constants.MapSize.Width, 0.0, _canvasSize.Width);
    }

    private void GenerateMapOverview()
    {
        if(SelectedMapTabIndex == TabMapIndex.Overview)
        {
            var zoomFactor = BackgroundController.GetZoomFactor();
            var containsBackgroundOverview = false;

            var overviewBitmaps = new List<OverviewBitmap>();
            if (BackgroundController.GetOverviewBitmap(out var backgroundOverview))
            {
                containsBackgroundOverview = true;
                overviewBitmaps.Add(backgroundOverview);
            }
            if (DrawingController.GetOverviewBitmap(zoomFactor, out var drawingOverview))
            {
                overviewBitmaps.Add(drawingOverview);
            }
            if (TokenController.GetOverviewBitmap(zoomFactor, out var tokenOverview))
            {
                overviewBitmaps.Add(tokenOverview);
            }

            MapOverview.CreateOverview(overviewBitmaps, zoomFactor, containsBackgroundOverview, BackgroundController.IsGridShown);
        }
        else
        {
            MapOverview.ClearMap();
        }
    }
}
