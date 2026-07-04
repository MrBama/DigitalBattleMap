using DigitalBattleMap.Common;
using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;
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
    private ApplicationUpdater _applicationUpdater;
    private Size<double> _canvasSize;
    private Action _multiMoveAction;
    private MonsterTokens _monsterTokens = new();
    private Timer _autoSaveTimer;

    public MainWindowViewModel(IWindowService windowService)
    {
        _windowService = windowService;
        Initialize();
        OpenMapWindow();
        _applicationUpdater!.CheckForUpdatesInBackground();
    }

    public MainWindowViewModel()
    {
        // This is required to render MainWindow in editor
        IO.Initialize(new Directory(), new File(), new ZipFile());
        CampaignController = new();
        BackgroundController = new();
        FogController = new();
        DrawingController = new();
        TokenController = new();
        InitializeProperties();
    }

    public event EventHandler OnGridSizeChanged;
    public event EventHandler<CanvasSizeChangedEventArgs> OnCanvasSizeChanged;

    public int SelectedTabIndex { get => Get<int>(); set => Set(value, SelectedTabChanged); }
    public int SelectedMapTabIndex { get => Get<int>(); set => SetWhenChanged(value, SelectedTabMapChanged); }
    public int DrawingCanvasZIndex { get => Get<int>(); set => Set(value); }
    public int MultiMoveCount { get => Get<int>(); set => Set(value); }
    public bool IsShowMapLocked { get => Get<bool>(); set => Set(value, IsShowMapLockedChanged); }
    public bool ServerConnectionButtonEnabled { get => Get<bool>(); set => Set(value); }
    public bool IsConfigurationMenuExpanded { get => Get<bool>(); set => Set(value); }
    public bool IsMultiMove { get => Get<bool>(); set => Set(value); }
    public bool HideDungeonMasterFeatures { get => Get<bool>(); set => Set(value); }
    public bool IsInfoEnabled { get => Get<bool>(); set => Set(value); }
    public string ServerConnectionButtonText { get => Get<string>(); set => Set(value); }
    public string ServerConnectionStatus { get => Get<string>(); set => Set(value); }
    public string ControlInfoText { get => Get<string>(); set => Set(value); }
    public Visibility DrawingCanvasVisibility { get => Get<Visibility>(); set => Set(value); }
    public Visibility FogCanvasVisibility { get => Get<Visibility>(); set => Set(value); }
    public Visibility MouseInputCanvasVisibility { get => Get<Visibility>(); set => Set(value); }
    public Visibility TokenVisibility { get => Get<Visibility>(); set => Set(value); }
    public System.Windows.Media.Brush ServerConnectionStatusColor { get => Get<System.Windows.Media.Brush>(); set => Set(value); }
    public System.Windows.Media.Brush CropColor { get => Get<System.Windows.Media.Brush>(); set => Set(value); }
    public CommandHistory<ZoomAndMoveCommand> ZoomAndMoveHistory { get; set; } = new(30);
    public MouseCanvasViewModel MouseCanvas { get => Get<MouseCanvasViewModel>(); set => Set(value); }

    public CampaignControllerViewModel CampaignController { get; set; }
    public BackgroundControllerViewModel BackgroundController { get; set; }
    public FogControllerViewModel FogController { get; set; }
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
    public BitmapSource FogEmblemBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.FogEmblem.png")).ToBitmapImage(); }
    public BitmapSource DrawingEmblemBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.DrawingEmblem.png")).ToBitmapImage(); }
    public BitmapSource TokenEmblemBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.TokenEmblem.png")).ToBitmapImage(); }
    public BitmapSource InfoIconBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.InfoIcon.png")).ToBitmapImage(); }

    int IMapSize.Width => Constants.MapSize.Width;
    int IMapSize.Height => Constants.MapSize.Height;
    int IMapSize.GridSize => BackgroundController.GridSize;
    double IMapSize.CanvasWidth => _canvasSize.Width;
    double IMapSize.CanvasHeight => _canvasSize.Height;
    double IMapSize.CanvasGridSize => CalculateCanvasGridSize();
    int? IMapSize.BackgroundWidth => BackgroundController.FullBackgroundWidth;
    int? IMapSize.BackgroundHeight => BackgroundController.FullBackgroundHeight;
    Point<int>? IMapSize.BackgroundOffset => BackgroundController.BackgroundOffset;
    string CampaingInfoText => "Active Mode: None \n \n Campaign tab contains no mouse controls";
    // Oneliner because webpage elements exist on top of all other ui elements including any info text after a new line.
    string StatBlockInfoText => "\t \t Select the header of a statblock to keep it focused. Switching between tabs will return you to the focused statblock."; 
    string OverviewInfoText => "Active Mode: Map Overview \n \n Press reset to reset the overview";

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
    public ICommand MapRevertZoomAndMoveCommand { get; set; }
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
        _applicationUpdater = new ApplicationUpdater(_windowService, _settings);
        _autoSaveTimer = new Timer(Constants.AutoSaveIntervalInMs);
        _autoSaveTimer.Elapsed += AutoSaveMap;

        MapOverview = new MapOverviewViewModel(this);
        MapOverview.OnZoomAndEnhance += OnZoomAndEnhance;

        CampaignController = new CampaignControllerViewModel(_windowService, _connectionManager, _monsterTokens, _settings);

        BackgroundController = new BackgroundControllerViewModel(_windowService, this, _settings);
        BackgroundController.OnGridSizeChanged += OnBackgroundGridSizeChanged;
        BackgroundController.OnZoomAndEnhance += OnZoomAndEnhance;
        BackgroundController.OnBackgroundUpdated += OnBackgroundUpdated;

        FogController = new FogControllerViewModel(_windowService, this, _settings);
        FogController.OnZoomAndEnhance += OnZoomAndEnhance;
        FogController.OnFogShapeUpdated += FogShapesUpdated;
        FogController.OnControlUpdated += ControlUpdated;

        TokenController = new TokenControllerViewModel(_windowService, _connectionManager, this, _monsterTokens, CampaignController, _settings);
        TokenController.OnZoomAndEnhance += OnZoomAndEnhance;
        TokenController.OnTokenBitmapUpdated += TokenBitmapUpdated;
        TokenController.OnToggleFog += ToggleFog;

        DrawingController = new DrawingControllerViewModel(this, TokenController);
        DrawingController.OnZoomAndEnhance += OnZoomAndEnhance;
        DrawingController.OnDrawingShapesUpdated += DrawingShapesUpdated;

        // settings
        HideDungeonMasterFeatures = _settings.HideDungeonMasterFeatures;
        CropColor = System.Windows.Media.Brushes.LightGray;
        MouseCanvas = BackgroundController.MouseCanvas;
        ControlInfoText = CampaingInfoText;

        if (_settings.IsAutoSaveEnabled)
            _autoSaveTimer.Start();
    }

    private void InitializeProperties()
    {
        DrawingCanvasVisibility = Visibility.Hidden;
        MouseInputCanvasVisibility = Visibility.Visible;
        TokenVisibility = Visibility.Hidden;
        ServerConnectionButtonText = "Connect";
        ServerConnectionStatus = "Disconnected";
        ServerConnectionStatusColor = System.Windows.Media.Brushes.Red;
        ServerConnectionButtonEnabled = true;
    }

    protected override void InitializeCommands()
    {
        ShowMapCommand = new RelayCommand(p => ShowMapToPlayers());
        WindowClosingCommand = new RelayCommand(p => WindowClosing());
        CanvasSizeOnStartupCommand = new RelayCommand(p => SetCanvasSize((double)p));
        CanvasSizeChangedCommand = new RelayCommand(p => CanvasSizeChanged((SizeChangedEventArgs)p));
        ClearAllCommand = new RelayCommand(p => ClearMap());
        SettingsCommand = new RelayCommand(p => OpenSettings());
        MoveMapArrowCommand = new RelayCommand(p => MoveMap((ArrowDirection)p));
        SaveMapCommand = new RelayCommand(p => SaveMap());
        OpenMapCommand = new RelayCommand(p => OpenMap());
        ServerConnectionCommand = new RelayCommand(p => ServerConnectionButton());
        MapZoomInCommand = new RelayCommand(p => ZoomIn());
        MapZoomOutCommand = new RelayCommand(p => ZoomOut());
        MapCropCommand = new RelayCommand(p => SetCropMode());
        MapRevertZoomAndMoveCommand = new RelayCommand(p => RevertZoomAndMove());
        HideConfigurationCommand = new RelayCommand(p => { IsConfigurationMenuExpanded = false; });
        KeyDownCommand = new RelayCommand(p => KeyDown((KeyEventArgs)p));
        KeyUpCommand = new RelayCommand(p => KeyUp((KeyEventArgs)p));
    }

    public void UpdateSuccessful()
    {
        var confirmationWindowViewModel = new ConfirmationWindowViewModel
        {
            Content = "Update successful!",
            IsLeftButtonVisible = false,
            IsRightButtonVisible = false,
            IsMiddleButtonVisible = true
        };
        _windowService.ShowWindowDialog<ConfirmationWindow>(confirmationWindowViewModel);

        if (IO.Directory.Exists(Constants.UpdateDirectoryPath))
        {
            IO.Directory.Delete(Constants.UpdateDirectoryPath, true);
        }
    }

    private void OnBackgroundGridSizeChanged(object? sender, GridSizeChangedEventArgs e)
    {
        OnGridSizeChanged?.Invoke(this, new EventArgs());
        UpdateMap(DrawLayer.GridAndStrokes);
    }

    private void OnZoomAndEnhance(object? sender, ZoomAndEnhanceEventArgs e)
    {
        var selectedArea = e.rectangle;

        // Move to the Middle of the selected area (in steps of Gridsize)
        // find center of screen and selection
        var middleOfScreenX = _canvasSize.Width / 2.0;
        var middleOfScreenY = _canvasSize.Height / 2.0;
        var middleOfSelectionX = selectedArea.X + (selectedArea.Width / 2.0);
        var middleOfSelectionY = selectedArea.Y + (selectedArea.Height / 2.0);

        // find number of positive or negative steps need to reach the center of selection
        var stepsX = (int)Math.Round((middleOfSelectionX - middleOfScreenX) / CalculateCanvasGridSize());
        var stepsY = (int)Math.Round((middleOfSelectionY - middleOfScreenY) / CalculateCanvasGridSize());

        PauseBitmapCreation(true);
        ZoomAndMoveHistory.PauseEnqueueing = true;
        MoveMap(stepsX, stepsY);

        // Zoom to new grid size
        var ratio = _canvasSize.Width / selectedArea.Width;
        var oldGridSize = BackgroundController.GridSize;
        var newGridSize = (int)Math.Round((double)BackgroundController.GridSize * ratio, 0);
        newGridSize = Math.Min(newGridSize, Constants.MaxGridSize);

        PauseBitmapCreation(false);
        ZoomToGridSize(newGridSize);

        // Save the move and zoom for history
        ZoomAndMoveHistory.PauseEnqueueing = false;
        ZoomAndMoveHistory.Enqueue(new ZoomAndMoveCommand(new Point<int>(stepsX, stepsY), oldGridSize));

        // Reset mouse canvas
        MouseCanvas.ResetSelection();
        MapOverview.MouseCanvas.ResetSelection();
        CropColor = System.Windows.Media.Brushes.LightGray;
        SelectedMapTabIndex = TabMapIndex.Map;
    }

    private void ControlUpdated(object? sender, ControlInfoEventArgs e)
    {
        ControlInfoText = GetControlInfoText(e);
    }

    private void ToggleFog(object? sender, ToggleFogEventArgs e)
    {
        FogController.ToggleFog(e);
        UpdateMap(DrawLayer.Fog);
    }

    private void OnBackgroundUpdated(object? sender, EventArgs e)
    {
        UpdateMap(DrawLayer.Background);
    }

    private void FogShapesUpdated(object? sender, EventArgs e)
    {
        UpdateMap(DrawLayer.Fog);
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

    private void ShowMapToPlayers()
    {
        UpdatePauseStatus();
        ShowMap();
    }

    private void ShowMap(DrawLayer drawing = 0)
    {
        switch (drawing)
        {
            case DrawLayer.All:
                {
                    using var backgroundBitmap = BackgroundController.GetBackgroundBitmap();
                    using var gridAndTokenBitmapAll = CreateGridAndDrawingBitmap();
                    _mapWindowViewModel.BackgroundBitmapSource = backgroundBitmap.ToBitmapImage();
                    _mapWindowViewModel.FogBitmapSource = FogController.GetFogBitmap().ToBitmapImage();
                    _mapWindowViewModel.GridBitmapSource = gridAndTokenBitmapAll.ToBitmapImage();
                    _mapWindowViewModel.TokenBitmapSource = TokenController.TokenBitmapSource;

                    using var backgroundAndFogBitmap = GetMergedBackgroundAndFogBitmap(backgroundBitmap);
                    _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Background, Bitmap = new Bitmap(backgroundAndFogBitmap) });
                    _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.GridAndStrokes, Bitmap = new Bitmap(gridAndTokenBitmapAll) });
                    _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Tokens, Bitmap = new Bitmap(TokenController.GetTokenBitmap()) });
                    break;
                }
            case DrawLayer.Background: case DrawLayer.Fog:
                {
                    using var backgroundBitmap = BackgroundController.GetBackgroundBitmap();
                    using var backgroundAndFogBitmap = GetMergedBackgroundAndFogBitmap(backgroundBitmap);
                    _mapWindowViewModel.BackgroundBitmapSource = backgroundBitmap.ToBitmapImage();
                    _mapWindowViewModel.FogBitmapSource = FogController.GetFogBitmap().ToBitmapImage();
                    _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Background, Bitmap = new Bitmap(backgroundAndFogBitmap) });
                    break;
                }
            case DrawLayer.GridAndStrokes:
                {
                    using var gridAndTokenBitmap = CreateGridAndDrawingBitmap();
                    _mapWindowViewModel.GridBitmapSource = gridAndTokenBitmap.ToBitmapImage();
                    _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.GridAndStrokes, Bitmap = new Bitmap(gridAndTokenBitmap) });
                    break;
                }
            case DrawLayer.Tokens:
                {
                    _mapWindowViewModel.TokenBitmapSource = TokenController.TokenBitmapSource;
                    _connectionManager.SendMapUpdate(new MapUpdate { Layer = DrawLayer.Tokens, Bitmap = new Bitmap(TokenController.GetTokenBitmap()) });
                    break;
                }
        }
    }

    private Bitmap GetMergedBackgroundAndFogBitmap(Bitmap backgroundBitmap)
    {
        return BitmapTools.MergeBitmaps(backgroundBitmap, FogController.GetFogBitmap());
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
            FogController.ClearFog();

            SelectedTabMapChanged();
            _connectionManager.ClearMap();
            ZoomAndMoveHistory.Clear();
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
                MouseInputCanvasVisibility = Visibility.Visible;
                FogCanvasVisibility = Visibility.Visible;
                DrawingCanvasVisibility = Visibility.Visible;
                TokenVisibility = Visibility.Visible;

                MouseCanvas = CampaignController.MouseCanvas;
                ControlInfoText = CampaingInfoText;
                DrawingCanvasZIndex = 0;
                break;
            case TabIndex.Background:
                MouseInputCanvasVisibility = Visibility.Visible;
                FogCanvasVisibility = Visibility.Visible;
                DrawingCanvasVisibility = Visibility.Hidden;
                TokenVisibility = Visibility.Hidden;

                MouseCanvas = BackgroundController.MouseCanvas;
                ControlInfoText = string.Empty;
                DrawingCanvasZIndex = 1;
                break;
            case TabIndex.Fog:
                MouseInputCanvasVisibility = Visibility.Visible;
                FogCanvasVisibility = Visibility.Visible;
                DrawingCanvasVisibility = Visibility.Visible;
                TokenVisibility = Visibility.Hidden;

                MouseCanvas = FogController.MouseCanvas;
                FogController.ActiveFogShape.UpdateControls();
                DrawingCanvasZIndex = 1;
                break;
            case TabIndex.Drawing:
                MouseInputCanvasVisibility = Visibility.Visible;
                FogCanvasVisibility = Visibility.Hidden;
                DrawingCanvasVisibility = Visibility.Visible;
                TokenVisibility = Visibility.Visible;

                MouseCanvas = DrawingController.MouseCanvas;
                ControlInfoText = GetDrawingInfoText();
                DrawingCanvasZIndex = 1;
                break;
            case TabIndex.Tokens:
                DrawingCanvasVisibility = Visibility.Visible;
                FogCanvasVisibility = Visibility.Visible;
                MouseInputCanvasVisibility = Visibility.Visible;
                TokenVisibility = Visibility.Visible;

                MouseCanvas = TokenController.MouseCanvas;
                ControlInfoText = GetTokenInfoText();
                DrawingCanvasZIndex = 0;
                break;
        }

        SetMapTabControlText();
    }

    public Size<int> GetSize()
    {
        return Constants.MapSize;
    }

    public Size<double> GetCanvasSize()
    {
        return _canvasSize;
    }

    private void MoveMap(ArrowDirection arrowDirection)
    {
        if (!IsMultiMove)
        {
            MoveMap(arrowDirection, 1);
        }
        else
        {
            MultiMoveCount++;
            _multiMoveAction = () =>
            {
                MoveMap(arrowDirection, MultiMoveCount);
            };
        }
    }

    private void MoveMap(ArrowDirection arrowDirection, int steps)
    {
        PauseBitmapCreation(true);
        ZoomAndMoveHistory.Enqueue(new ZoomAndMoveCommand(arrowDirection, steps));
        BackgroundController.Move(arrowDirection, steps);
        FogController.Move(arrowDirection, steps);
        DrawingController.Move(arrowDirection, steps);
        TokenController.Move(arrowDirection, steps);
        PauseBitmapCreation(false);
    }

    private void MoveMap(int stepsX, int stepsY)
    {
        if(stepsX != 0)
        {
            var directionX = stepsX > 0 ? ArrowDirection.Right : ArrowDirection.Left;
            MoveMap(directionX, Math.Abs(stepsX));
        }

        if(stepsY != 0)
        {
            var directionY = stepsY > 0 ? ArrowDirection.Down : ArrowDirection.Up;
            MoveMap(directionY, Math.Abs(stepsY));
        }
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
            FogController.AddToSaveFile(saveFile);
            saveFile.Save(path);
        }
    }

    private void AutoSaveMap(object? sender, ElapsedEventArgs e)
    {
        _autoSaveTimer.Stop();
        var saveFile = new SaveFile();
        saveFile.CanvasSize = GetCanvasSize();
        BackgroundController.AddToSaveFile(saveFile);
        DrawingController.AddToSaveFile(saveFile);
        TokenController.AddToSaveFile(saveFile);
        FogController.AddToSaveFile(saveFile);
        saveFile.AutoSave();
        _autoSaveTimer.Start();
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
        FogController.OpenSaveFile(saveFile);

        DrawingController.OpenObjectLinks(saveFile.ObjectLinks);
        TokenController.OpenObjectLinks(saveFile.ObjectLinks);

        SelectedTabIndex = TabIndex.Tokens;
        SelectedMapTabIndex = 0;
        ControlInfoText = GetTokenInfoText();
        ZoomAndMoveHistory.Clear();

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
        if (SelectedMapTabIndex == TabMapIndex.Map)
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

    private void RevertZoomAndMove()
    {
        if (ZoomAndMoveHistory.TryDequeuePreviousCommand(out var previousCommand))
        {
            ZoomAndMoveHistory.PauseEnqueueing = true;
            PauseBitmapCreation(true);

            if (previousCommand.GridSize != null)
            {
                ZoomToGridSize((int)previousCommand.GridSize);
            }

            if (previousCommand.Steps != null)
            {
                MoveMap(-previousCommand.Steps.X, -previousCommand.Steps.Y);
            }

            ZoomAndMoveHistory.PauseEnqueueing = false;
            PauseBitmapCreation(false);
        }
    }

    private void ZoomToGridSize(int newGridSize)
    {
        var gridSizeChange = newGridSize - BackgroundController.GridSize;
        Zoom(gridSizeChange);
    }

    private void Zoom(int gridSizeChange)
    {
        var oldGridSize = BackgroundController.GridSize;
        ZoomAndMoveHistory.Enqueue(new ZoomAndMoveCommand(oldGridSize));

        var currentIsShowMapLocked = IsShowMapLocked;
        IsShowMapLocked = false;
        BackgroundController.UpdateGridSize(gridSizeChange);

        var zoomFactor = (double)BackgroundController.GridSize / (double)oldGridSize;
        BackgroundController.Zoom(zoomFactor);
        FogController.Zoom(zoomFactor);
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
        _connectionManager.SendMessage(new PauseMessage() { IsPaused = _mapWindowViewModel.IsPaused });
    }

    private void UpdatePauseStatus()
    {
        var paused = !IsShowMapLocked && TokenController.IsAnyTokenControlledByPlayer();
        if (paused != _mapWindowViewModel.IsPaused)
        {
            _mapWindowViewModel.IsPaused = paused;
            _connectionManager.SendMessage(new PauseMessage() { IsPaused = paused });
        }
    }

    private void IsShowMapLockedChanged()
    {
        _connectionManager?.UpdatePlayerControlAllowed(IsShowMapLocked);
        UpdatePauseStatus();
        UpdateMap(DrawLayer.All);
    }

    private void KeyDown(KeyEventArgs keyEventArgs)
    {
        if (keyEventArgs.Key == Key.LeftShift && !IsMultiMove)
        {
            IsMultiMove = true;
            MultiMoveCount = 0;
        }

        BackgroundController.KeyDown(keyEventArgs);
        FogController.KeyDown(keyEventArgs);
        DrawingController.KeyDown(keyEventArgs);
        TokenController.KeyDown(keyEventArgs);
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

        BackgroundController.KeyUp(keyEventArgs);
        FogController.KeyUp(keyEventArgs);
        DrawingController.KeyUp(keyEventArgs);
        TokenController.KeyUp(keyEventArgs);
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
        else if (e.SettingName == nameof(Settings.IsAutoSaveEnabled))
        {
            if (_settings.IsAutoSaveEnabled)
            {
                if(!_autoSaveTimer.Enabled)
                    _autoSaveTimer.Start();
            }
            else
            {
                _autoSaveTimer.Stop();
            }
        }
    }

    private double CalculateCanvasGridSize()
    {
        var gridSize = (double)BackgroundController.GridSize;
        return gridSize.Map(0.0, Constants.MapSize.Width, 0.0, _canvasSize.Width);
    }

    private void SelectedTabMapChanged()
    {
        switch (SelectedMapTabIndex)
        {
            case TabMapIndex.Map:
                // Not sure if this is smart
                // Rediscorvers the selected tab and sets the Control text depending on the tab.
                SelectedTabChanged(); 
                MapOverview.ClearMap();
                break;
            case TabMapIndex.Overview:
                GenerateMapOverview();
                break;
            case TabMapIndex.Statblocks:
                MapOverview.ClearMap();
                break;
        }

        SetMapTabControlText();
    }
     
    private void GenerateMapOverview() 
    { 
        var zoomFactor = BackgroundController.GetZoomFactor();
        var containsBackgroundOverview = false;

        var overviewBitmaps = new List<OverviewBitmap>();
        if (BackgroundController.GetOverviewBitmap(out var backgroundOverview))
        {
            containsBackgroundOverview = true;
            overviewBitmaps.Add(backgroundOverview);
        }
        if (FogController.GetOverviewBitmap(zoomFactor, out var fogOverview))
        {
            overviewBitmaps.Add(fogOverview);
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

    /**
     * Set the control text depending on TabMapIndex
     * TabMapIndex.Map control text depends on an other tab and is done in SelectedTabChanged
     */
    private void SetMapTabControlText()
    {
        if(SelectedMapTabIndex == TabMapIndex.Overview)
        {
            ControlInfoText = OverviewInfoText;
        } 
        else if (SelectedMapTabIndex == TabMapIndex.Statblocks)
        {
            ControlInfoText = StatBlockInfoText;
        }
    }

    private void PauseBitmapCreation(bool paused)
    {
        if(paused)
        {
            BackgroundController.PauseBitmapCreation();
            FogController.PauseBitmapCreation();
            DrawingController.PauseBitmapCreation();
            TokenController.PauseBitmapCreation();
        }
        else
        {
            BackgroundController.ResumeBitmapCreation();
            DrawingController.ResumeBitmapCreation();
            TokenController.ResumeBitmapCreation();
            FogController.ResumeBitmapCreation(); // For a move action causes and addition render to the server so placed as last so this is not noticed.
        }
    }

    private string GetTokenInfoText()
    {
        var info = new ControlInfoEventArgs()
        {
            controlName = "Token Controls",
            infoBlocks = new List<InfoBlock>()
            {
                new InfoBlock(ControlType.LMB, "Move selected token to grid location"),
                new InfoBlock(ControlType.RMB, "Select token in grid location"),
                new InfoBlock(ControlType.Alt, ControlType.LMB, "Toggle on/off the fog of war shape at the selected location")
            }
        };
        return GetControlInfoText(info);
    }

    private string GetDrawingInfoText()
    {
        var info = new ControlInfoEventArgs()
        {
            controlName = "Drawing Controls",
            infoBlocks = new List<InfoBlock>()
            {
                new InfoBlock(ControlType.LMB, "Hold to draw a stroke"),
                new InfoBlock(ControlType.RMB, "Move the selected shape")
            }
        };
        return GetControlInfoText(info);
    }

    private string GetControlInfoText(ControlInfoEventArgs e)
    {
        var controlInfoText = new StringBuilder();
        controlInfoText.Append("Active Mode: ").Append(e.controlName.ToString()); // header
        controlInfoText.Append("\n").Append("\n");
        foreach (var controlInfo in e.infoBlocks.OrderBy(i => (int?) i.Type ?? -1)) // order by type, null type first
        {
            var additional = controlInfo?.Additional == null ? string.Empty : " + " + controlInfo.Additional.ToString();
            var typeInfo = controlInfo?.Type == null ?
                string.Empty : // ignore type info in control info text
                string.Format("{0}{1}: ", controlInfo.Type.ToString(), additional); // adds type info in control info text + adds additional if present.

            var info = string.Format("{0}{1}\n", typeInfo, controlInfo!.ControlInfo);
            controlInfoText.Append(info);
        }
        return controlInfoText.ToString();
    }
}
