using DigitalBattleMap.Common;
using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private Bitmap _gridBitmap;
    private IWindowService _windowService;
    private MapWindowViewModel _mapWindowViewModel;
    private Settings _settings;
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
    public bool IsShowMapLocked { get => Get<bool>(); set => Set(value, IsShowMapLockedChanged); }
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
    public TokenControllerViewModel TokenController { get; set; }
    public BitmapSource GridBitmapSource { get => _gridBitmap.ToBitmapImage(); }
    public BitmapSource MapArrowUpBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Up).ToBitmapImage(); }
    public BitmapSource MapArrowDownBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Down).ToBitmapImage(); }
    public BitmapSource MapArrowLeftBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Left).ToBitmapImage(); }
    public BitmapSource MapArrowRightBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Right).ToBitmapImage(); }
    public BitmapSource MapZoomInBitmapSource { get => BitmapTools.CreateZoomButton(true).ToBitmapImage(); }
    public BitmapSource MapZoomOutBitmapSource { get => BitmapTools.CreateZoomButton(false).ToBitmapImage(); }
    public double MouseInputX { get; set; }
    public double MouseInputY { get; set; }

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
    public ICommand ServerConnectionCommand { get; set; }
    public ICommand MapZoomInCommand { get; set; }
    public ICommand MapZoomOutCommand { get; set; }
    public ICommand HideConfigurationCommand { get; set; }

    public void Initialize()
    {
        InitializeProperties();
        _settings = Settings.Load();
        GridSize = _settings.DefaultGridSize;
        _gridBitmap = BitmapTools.CreateGrid(GridSize);
        BackgroundController = new BackgroundControllerViewModel(_windowService, GridSize);
        BackgroundController.OnBackgroundUpdated += OnBackgroundUpdated;
        TokenController = new TokenControllerViewModel(_windowService, _settings, GridSize);
        TokenController.OnTokenBitmapUpdated += TokenBitmapUpdated;
        DrawingController = new DrawingControllerViewModel(TokenController, GridSize);
        DrawingController.OnDrawingStrokesUpdated += DrawingStrokesUpdated;

        _connectionManager = new ConnectionManager(TokenController);
        _connectionManager.OnConnected += ConnectionManagerConnected;
        _connectionManager.OnDisconnect += ConnectionManagerDisconnected;
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
        ServerConnectionCommand = new RelayCommand(p => ServerConnectionButton());
        MapZoomInCommand = new RelayCommand(p => Zoom(GridSize + 10));
        MapZoomOutCommand = new RelayCommand(p => Zoom(GridSize - 10));
        HideConfigurationCommand = new RelayCommand(p => { IsConfigurationMenuExpanded = false; });
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
    }

    private void GridSizeChanged()
    {
        GridSize = Math.Max(GridSize, 1);
        _gridBitmap = BitmapTools.CreateGrid(GridSize);
        BackgroundController.UpdateGridSize(GridSize);
        DrawingController.UpdateGridSize(GridSize);
        TokenController.UpdateGridSize(GridSize);
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
                _mapWindowViewModel.TokenBitmapSource = TokenController.TokenBitmapSource;
                _connectionManager.SendMapUpdate(new MapUpdate{ Layer = DrawLayer.Background, Bitmap = new Bitmap(BackgroundController.GetBackgroundBitmap()) });
                _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.GridAndStrokes, Bitmap = new Bitmap(gridAndTokenBitmapAll) });
                _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Tokens, Bitmap = new Bitmap(TokenController.GetTokenBitmap()) });
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
                _mapWindowViewModel.TokenBitmapSource = TokenController.TokenBitmapSource;
                _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Tokens, Bitmap = new Bitmap(TokenController.GetTokenBitmap()) });
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
        _canvasSize = new Size<double>
        {
            Width = width,
            Height = width / 16 * 9
        };

        BackgroundController.SetCanvasSize(_canvasSize);
        DrawingController.SetCanvasSize(_canvasSize);
        TokenController.SetCanvasSize(_canvasSize);
    }

    private void ClearMap()
    {
        var confirmationWindowViewModel = new ConfirmationWindowViewModel
        {
            Content = "Are you sure you want to clear everything?"
        };
        _windowService.ShowWindowDialog<ConfirmationWindow>(confirmationWindowViewModel);

        if (confirmationWindowViewModel.Confirmed)
        {
            BackgroundController.ClearBackground();
            DrawingController.ClearDrawings();
            TokenController.ClearTokens();
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
            TokenController.ReloadMonsterTokens();
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
                TokenController.MouseDown(new Point<double>(MouseInputX, MouseInputY));
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
        TokenController.Move(arrowDirection);
    }

    private void SaveMap()
    {
        if (_windowService.ShowSaveFileDialog(out string path, "(*.dbm)|*.dbm"))
        {
            var saveFile = new SaveFile
            {
                GridSize = GridSize,
                IsGridShown = IsGridShown
            };
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
            GridSize = saveFile.GridSize;
            GridSizeChanged();
            IsGridShown = saveFile.IsGridShown;
            DrawingController.OpenSaveFile(saveFile);
            TokenController.OpenSaveFile(saveFile);
            SelectedTabIndex = TabIndex.Tokens;

            DrawingController.OpenObjectLinks(saveFile.ObjectLinks);
            TokenController.OpenObjectLinks(saveFile.ObjectLinks);

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

    private void IsShowMapLockedChanged()
    {
        _connectionManager?.UpdatePlayerControlAllowed(IsShowMapLocked);
        UpdateMap(DrawLayer.All);
    }
}
