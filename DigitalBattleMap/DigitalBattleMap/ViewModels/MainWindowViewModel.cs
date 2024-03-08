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
using System.Windows.Threading;

namespace DigitalBattleMap.ViewModels;

public class MainWindowViewModel : ViewModelBase, ICanvasSize
{
    private IWindowService _windowService;
    private MapWindowViewModel _mapWindowViewModel;
    private Settings _settings;
    private ConnectionManager _connectionManager;
    private Size<double> _canvasSize;
    private Action _multiMoveAction;
    private MonsterTokens _monsterTokens = new();

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

    public event EventHandler<CanvasSizeChangedEventArgs> OnCanvasSizeChanged;

    public int SelectedTabIndex { get => Get<int>(); set => Set(value, SelectedTabChanged); }
    public int SelectedMapTabIndex { get => Get<int>(); set => Set(value); }
    public int InkCanvasZIndex { get => Get<int>(); set => Set(value); }
    public int MultiMoveCount { get => Get<int>(); set => Set(value); }
    public bool IsShowMapLocked { get => Get<bool>(); set => Set(value, IsShowMapLockedChanged); }
    public bool ServerConnectionButtonEnabled { get => Get<bool>(); set => Set(value); }
    public bool IsConfigurationMenuExpanded { get => Get<bool>(); set => Set(value); }
    public bool IsMultiMove { get => Get<bool>(); set => Set(value); }
    public string ServerConnectionButtonText { get => Get<string>(); set => Set(value); }
    public string ServerConnectionStatus { get => Get<string>(); set => Set(value); }
    public Visibility InkCanvasVisibility { get => Get<Visibility>(); set => Set(value); }
    public Visibility MouseInputCanvasVisibility { get => Get<Visibility>(); set => Set(value); }
    public Visibility TokenVisibility { get => Get<Visibility>(); set => Set(value); }
    public System.Windows.Media.Brush ServerConnectionStatusColor { get => Get<System.Windows.Media.Brush>(); set => Set(value); }

    public MouseCanvasViewModel MouseCanvas { get; set; }
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
    public BitmapSource CampaignEmblemBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.CampaignEmblem.png")).ToBitmapImage(); }
    public BitmapSource BackgroundEmblemBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.BackgroundEmblem.png")).ToBitmapImage(); }
    public BitmapSource DrawingEmblemBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.DrawingEmblem.png")).ToBitmapImage(); }
    public BitmapSource TokenEmblemBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.TokenEmblem.png")).ToBitmapImage(); }

    double ICanvasSize.Width => _canvasSize.Width;
    double ICanvasSize.Height => _canvasSize.Height;

    public ICommand ShowMapCommand { get; set; }
    public ICommand WindowClosingCommand { get; set; }
    public ICommand InkCanvasSizeOnStartupCommand { get; set; }
    public ICommand InkCanvasSizeChangedCommand { get; set; }
    public ICommand ClearAllCommand { get; set; }
    public ICommand SettingsCommand { get; set; }
    public ICommand MoveMapArrowCommand { get; set; }
    public ICommand SaveMapCommand { get; set; }
    public ICommand OpenMapCommand { get; set; }
    public ICommand ServerConnectionCommand { get; set; }
    public ICommand MapZoomInCommand { get; set; }
    public ICommand MapZoomOutCommand { get; set; }
    public ICommand HideConfigurationCommand { get; set; }
    public ICommand KeyDownCommand { get; set; }
    public ICommand KeyUpCommand { get; set; }

    public void Initialize()
    {
        InitializeProperties();
        _settings = Settings.Load();
        _monsterTokens.ReloadTokens();
        _connectionManager = new ConnectionManager();
        _connectionManager.OnConnected += ConnectionManagerConnected;
        _connectionManager.OnDisconnect += ConnectionManagerDisconnected;
        MouseCanvas = new();
        CampaignController = new CampaignControllerViewModel(_windowService, _connectionManager, _monsterTokens, _settings);
        BackgroundController = new BackgroundControllerViewModel(_windowService, this, MouseCanvas, _settings);
        BackgroundController.OnGridSizeChanged += OnGridSizeChanged;
        BackgroundController.OnBackgroundUpdated += OnBackgroundUpdated;
        TokenController = new TokenControllerViewModel(_windowService, _connectionManager, this, MouseCanvas, _monsterTokens, CampaignController, _settings, BackgroundController.GridSize);
        TokenController.OnTokenBitmapUpdated += TokenBitmapUpdated;
        DrawingController = new DrawingControllerViewModel(this, TokenController, BackgroundController.GridSize);
        DrawingController.OnDrawingStrokesUpdated += DrawingStrokesUpdated;
    }

    private void InitializeProperties()
    {
        IsShowMapLocked = false;
        InkCanvasVisibility = Visibility.Hidden;
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
        InkCanvasSizeOnStartupCommand = new RelayCommand(p => SetInkCanvasSize((double)p));
        InkCanvasSizeChangedCommand = new RelayCommand(p => InkCanvasSizeChanged((SizeChangedEventArgs)p));
        ClearAllCommand = new RelayCommand(p => ClearMap());
        SettingsCommand = new RelayCommand(p => OpenSettings());
        MoveMapArrowCommand = new RelayCommand(p => MoveMap((string)p));
        SaveMapCommand = new RelayCommand(p => SaveMap());
        OpenMapCommand = new RelayCommand(p => OpenMap());
        ServerConnectionCommand = new RelayCommand(p => ServerConnectionButton());
        MapZoomInCommand = new RelayCommand(p => ZoomIn());
        MapZoomOutCommand = new RelayCommand(p => ZoomOut());
        HideConfigurationCommand = new RelayCommand(p => { IsConfigurationMenuExpanded = false; });
        KeyDownCommand = new RelayCommand(p => KeyDown((KeyEventArgs)p));
        KeyUpCommand = new RelayCommand(p => KeyUp((KeyEventArgs)p));
    }

    private void OnGridSizeChanged(object? sender, GridSizeChangedEventArgs e)
    {
        DrawingController.UpdateGridSize(e.NewGridSize);
        TokenController.UpdateGridSize(e.NewGridSize);
        UpdateMap(DrawLayer.GridAndStrokes);
    }

    private void OnBackgroundUpdated(object? sender, EventArgs e)
    {
        UpdateMap(DrawLayer.Background);
    }

    private void DrawingStrokesUpdated(object? sender, EventArgs e)
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
        return BitmapTools.CreateGridAndStrokesBitmap(BackgroundController.GetGridBitmap(), DrawingController.Strokes, Size<int>.Create(_canvasSize));
    }

    private void WindowClosing()
    {
        _connectionManager.Disconnect();
        _windowService.CloseAllWindows();
    }

    private void SetInkCanvasSize(double width)
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

    private void InkCanvasSizeChanged(SizeChangedEventArgs eventArgs)
    {
        SetInkCanvasSize(eventArgs.NewSize.Width);
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

        if (settingsWindowViewModel.MonitorChanged)
        {
            _mapWindowViewModel.ChangeWindowPosition(_settings.MonitorPosition.X);
        }

        if (_settings.ShowMapWindow)
        {
            _windowService.ShowWindow(_mapWindowViewModel);
        }
        else
        {
            _windowService.HideWindow(_mapWindowViewModel);
        }

        if (settingsWindowViewModel.MonsterTokensDownloaded)
        {
            _monsterTokens.ReloadTokens();
        }
    }

    public void SelectedTabChanged()
    {
        MouseCanvas.SetSelectedTabIndex(SelectedTabIndex);
        switch (SelectedTabIndex)
        {
            case TabIndex.Campaign:
                MouseInputCanvasVisibility = Visibility.Hidden;
                InkCanvasVisibility = Visibility.Visible;
                TokenVisibility = Visibility.Visible;
                InkCanvasZIndex = 0;
                break;
            case TabIndex.Background:
                InkCanvasVisibility = Visibility.Hidden;
                MouseInputCanvasVisibility = Visibility.Visible;
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

    public Size<double> GetSize()
    {
        return _canvasSize;
    }

    private void MoveMap(string direction)
    {
        var arrowDirection = Enum.Parse<ArrowDirection>(direction);
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

    private void SaveMap()
    {
        if (_windowService.ShowSaveFileDialog(out string path, filter: "(*.dbm)|*.dbm"))
        {
            var saveFile = new SaveFile();
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

        if(e.IsConnectionLost)
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
}
