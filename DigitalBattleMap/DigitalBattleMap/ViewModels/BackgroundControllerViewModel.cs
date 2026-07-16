using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Imaging;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels;

public class BackgroundControllerViewModel : ControllerViewModelBase
{
    private IImage _backgroundBitmap;
    private IImage _gmOverlayBitmap;
    private IImage _fullBackgroundBitmap;
    private IImage _gridBitmap;
    private System.Drawing.Rectangle _area; 
    private IWindowService _windowService;
    private Point<double> _mouseDownPosition;
    private bool _mouseDown;
    private Settings _settings;

    public BackgroundControllerViewModel()
    {
        GridSize = 65;
        Initialize();
    }

    public BackgroundControllerViewModel(IWindowService windowService, IMapSize mapSize, Settings settings) : base(mapSize)
    {
        _windowService = windowService;
        _settings = settings;
        GridSize = _settings.DefaultGridSize;
        SelectedBackgroundColor = _settings.DefaultBackgroundColor;
        Initialize();
    }

    private void Initialize()
    {
        _area = new System.Drawing.Rectangle(0, 0, Constants.MapSize.Width, Constants.MapSize.Height);
        RegisterPropertyChangedWatcher(nameof(IsBackgroundEditingAllowed), new List<string>() { nameof(HasOpenedBackground) });

        IsGridShown = true;
        GridBitmap = BitmapTools.CreateGrid(GridSize);
        BackgroundBitmap = BitmapTools.CreateEmptyBitmap();
        GMOverlayBitmap = BitmapTools.CreateEmptyBitmap();

        BackgroundZoomPercentage = 10;
        GridCellsWidth = 10;
        GridCellsHeight = 10;
        FeetPerGridCell = Constants.FeetPerGridCell;
        ZoomSize = Constants.DefaultZoomSize;

        MouseCanvas = new MouseCanvasViewModel();
        MouseCanvas.OnLeftButtonDown += MouseDown;
        MouseCanvas.OnLeftButtonUp += MouseUp;
        MouseCanvas.OnFixRatioRectangleAreaSelected += FixRatioRectangleAreaSelected;
    }

    protected override void InitializeCommands()
    {
        OpenBackgroundCommand = new RelayCommand(p => OpenBackground());
        OpenBackgroundFromClipboardCommand = new RelayCommand(p => OpenBackgroundFromClipboard());
        OpenGMOverlayCommand = new RelayCommand(p => OpenGMOverlay());
        ClearBackgroundCommand = new RelayCommand(p => ClearBackground());
        ClearGMOverlayCommand = new RelayCommand(p => ClearGMOverlay());
        BackgroundZoomInCommand = new RelayCommand(p => ZoomIn(BackgroundZoomPercentage));
        BackgroundZoomOutCommand = new RelayCommand(p => ZoomOut(BackgroundZoomPercentage));
        FitBackgroundToGridCommand = new RelayCommand(p => FitToGrid());
        GridSizeEnterCommand = new RelayCommand(p => GridSizeChanged());
        BackgroundColorChangedCommand = new RelayCommand(p => BackgroundColorChanged());
        RotateLeftCommand = new RelayCommand(p => RotateLeft());
        RotateRightCommand = new RelayCommand(p => RotateRight());
    }

    public event EventHandler OnBackgroundUpdated;
    public event EventHandler<GridSizeChangedEventArgs> OnGridSizeChanged;
    public event EventHandler<ZoomAndEnhanceEventArgs> OnZoomAndEnhance;

    public bool IsBackgroundEditingAllowed { get => HasOpenedBackground; }
    public bool HasOpenedBackground { get => Get<bool>(); set => Set(value); }
    public bool HasOpenGMOverlay { get => Get<bool>(); set => Set(value); }
    public bool IsGridShown { get => Get<bool>(); set => Set(value, GridShownChanged); }
    public int GridCellsWidth { get => Get<int>(); set => Set(value); }
    public int GridCellsHeight { get => Get<int>(); set => Set(value); }
    public int FeetPerGridCell { get => Get<int>(); set => Set(value); }
    public int GridSize { get => Get<int>(); set => Set(value); }
    public int ZoomSize { get => Get<int>(); set => Set(value); }
    public double BackgroundZoomPercentage { get => Get<double>(); set => Set(value, () => NotifyPropertyChange(nameof(BackgroundZoomPercentageLabel))); }
    public BackgroundColor SelectedBackgroundColor { get => Get<BackgroundColor>(); set => Set(value); }
    public BitmapSource BackgroundBitmapSource { get => Get<BitmapSource>(); private set => Set(value); }
    public BitmapSource GMOverlayBitmapSource { get => Get<BitmapSource>(); private set => Set(value); }
    public BitmapSource GridBitmapSource { get => Get<BitmapSource>(); private set => Set(value); }
    public BitmapSource RotateLeftBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.RotateLeft.png")).ToBitmapImage(); }
    public BitmapSource RotateRightBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.RotateRight.png")).ToBitmapImage(); }
    public string BackgroundZoomPercentageLabel { get => $"{BackgroundZoomPercentage}%"; }
    public MouseCanvasViewModel MouseCanvas { get => Get<MouseCanvasViewModel>(); private set => Set(value); }
    public ObservableCollection<BackgroundColor> BackgroundColors { get; set; } = new() { BackgroundColor.Black, BackgroundColor.White };
    public int? FullBackgroundWidth => _fullBackgroundBitmap?.Width;
    public int? FullBackgroundHeight => _fullBackgroundBitmap?.Height;
    public Point<int>? BackgroundOffset => _fullBackgroundBitmap != null ? new Point<int>(-_area.X, -_area.Y) : null;

    public ICommand OpenBackgroundCommand { get; set; }
    public ICommand OpenBackgroundFromClipboardCommand { get; set; }
    public ICommand OpenGMOverlayCommand { get; set; }
    public ICommand ClearBackgroundCommand { get; set; }
    public ICommand ClearGMOverlayCommand { get; set; }
    public ICommand BackgroundZoomInCommand { get; set; }
    public ICommand BackgroundZoomOutCommand { get; set; }
    public ICommand FitBackgroundToGridCommand { get; set; }
    public ICommand GridSizeEnterCommand { get; set; }
    public ICommand BackgroundColorChangedCommand { get; set; }
    public ICommand RotateLeftCommand { get; set; }
    public ICommand RotateRightCommand { get; set; }

    private IImage BackgroundBitmap
    {
        get => _backgroundBitmap;
        set
        {
            if (value != _backgroundBitmap)
            {
                _backgroundBitmap = value;
                BackgroundBitmapSource = value.ToBitmapImage();
            }
        }
    }

    private IImage GMOverlayBitmap
    {
        get => _gmOverlayBitmap;
        set
        {
            if (value != _gmOverlayBitmap)
            {
                GMOverlayBitmapSource = value.ToBitmapImage();
            }
        }
    }

    private IImage GridBitmap
    {
        get => _gridBitmap;
        set
        {
            if (value != _gridBitmap)
            {
                _gridBitmap = value;
                GridBitmapSource = value.ToBitmapImage();
            }
        }
    }

    public IImage GetBackgroundBitmap()
    {
        return SelectedBackgroundColor == BackgroundColor.Black 
            ? BitmapTools.MergeBitmaps(BitmapTools.CreateBlackBitmap(), BackgroundBitmap) : BackgroundBitmap;
    }

    public IImage GetGridBitmap()
    {
        return GridBitmap;
    }

    public bool GetOverviewBitmap(out OverviewBitmap overviewBitmap)
    {
        overviewBitmap = new OverviewBitmap();

        if(_fullBackgroundBitmap != null)
        {
            overviewBitmap.Bitmap = _fullBackgroundBitmap.Clone();
            overviewBitmap.OffsetFromOrigin = new Point<int>(-_area.X, -_area.Y);
            return true;
        }

        return false;
    }

    public void ClearBackground()
    {
        IsGridShown = true;
        GridSize = _settings.DefaultGridSize;
        GridSizeChanged();

        _fullBackgroundBitmap = null;
        _gmOverlayBitmap = null;
        _area = new System.Drawing.Rectangle(0, 0, _mapSize.Width, _mapSize.Height);
        BackgroundBitmap = BitmapTools.CreateEmptyBitmap();
        GMOverlayBitmap = BitmapTools.CreateEmptyBitmap();
        GridCellsWidth = 10;
        GridCellsHeight = 10;
        FeetPerGridCell = Constants.FeetPerGridCell;
        HasOpenedBackground = false;
        HasOpenGMOverlay = false;
        ZoomSize = Constants.DefaultZoomSize;
        SelectedBackgroundColor = _settings.DefaultBackgroundColor;
        NotifyBackgroundUpdated();
    }

    public void ClearGMOverlay()
    {
        _gmOverlayBitmap = null;
        GMOverlayBitmap = BitmapTools.CreateEmptyBitmap();
        HasOpenGMOverlay = false;
    }

    public void UpdateGridSize(int gridSizeChange)
    {
        GridSize = Math.Max(GridSize + gridSizeChange, Constants.MinGridSize);
        GridSize = Math.Min(GridSize, Constants.MaxGridSize);
        GridSizeChanged();
    }

    public override void Zoom(double zoomFactor)
    {
        if (_fullBackgroundBitmap != null)
        {
            ZoomWithZoomFactor(zoomFactor);
            CreateBackground();
        }
    }

    public override void Move(ArrowDirection direction, int movementCount)
    {
        if (_fullBackgroundBitmap != null)
        {
            double preciseGridSize = GridSize * movementCount;
            var distanceX = (int)Math.Round(preciseGridSize.Map(0, _mapSize.Width, 0, _area.Width));
            var distanceY = (int)Math.Round(preciseGridSize.Map(0, _mapSize.Height, 0, _area.Height));

            switch (direction)
            {
                case ArrowDirection.Up:
                    _area.Y -= distanceY;
                    break;
                case ArrowDirection.Down:
                    _area.Y += distanceY;
                    break;
                case ArrowDirection.Left:
                    _area.X -= distanceX;
                    break;
                case ArrowDirection.Right:
                    _area.X += distanceX;
                    break;
            }

            CreateBackground();
        }
    }

    public override void AddToSaveFile(SaveFile saveFile)
    {
        saveFile.IsGridShown = IsGridShown;
        saveFile.GridSize = GridSize;
        saveFile.FullBackground = _fullBackgroundBitmap != null ? _fullBackgroundBitmap.ToDrawingBitmap() : null;
        saveFile.GMOverlay = _gmOverlayBitmap != null ? _gmOverlayBitmap.ToDrawingBitmap() : null;
        saveFile.BackgroundArea = _area;
        saveFile.GridCellsWidth = GridCellsWidth;
        saveFile.GridCellsHeight = GridCellsHeight;
        saveFile.BackgroundFeetPerGridCell = FeetPerGridCell;
    }

    public override void OpenSaveFile(SaveFile saveFile)
    {
        ClearBackground();

        IsGridShown = saveFile.IsGridShown;
        GridSize = saveFile.GridSize;
        GridSizeChanged();

        GridCellsWidth = saveFile.GridCellsWidth;
        GridCellsHeight = saveFile.GridCellsHeight;
        FeetPerGridCell = saveFile.BackgroundFeetPerGridCell;

        if (saveFile.FullBackground != null)
        {
            _fullBackgroundBitmap = ImageFactory.FromDrawingBitmap(saveFile.FullBackground);
            _area = saveFile.BackgroundArea;
            HasOpenedBackground = true;
        }

        if (saveFile.GMOverlay != null)
        {
            _gmOverlayBitmap = ImageFactory.FromDrawingBitmap(saveFile.GMOverlay);
            HasOpenGMOverlay = true;
        }

        CreateBackground();
    }

    public double GetZoomFactor()
    {
        return _area.Width / (double)_mapSize.Width;
    }   

    protected override void CreateBitmap()
    {
        CreateBackground();
    }

    private void OpenBackground()
    {
        if (_windowService.ShowOpenFileDialog(out string path))
        {
            ExtractGridCells(Path.GetFileNameWithoutExtension(path));
            OpenBackground(ImageFactory.FromDrawingBitmap(IO.File.LoadBitmap(path)));
            FitToGrid();
        }
    }

    private void OpenBackgroundFromClipboard()
    {
        var bitmap = IO.File.LoadBitmapFromClipboard();
        if (bitmap != null)
        {
            OpenBackground(ImageFactory.FromDrawingBitmap(bitmap));
        }
    }

    private void OpenBackground(IImage bitmap)
    {
        _fullBackgroundBitmap = bitmap;
        _area = new System.Drawing.Rectangle(
            (_fullBackgroundBitmap.Width / 2) - (_mapSize.Width / 2),
            (_fullBackgroundBitmap.Height / 2) - (_mapSize.Height / 2),
            _mapSize.Width,
            _mapSize.Height);

        FeetPerGridCell = Constants.FeetPerGridCell;
        HasOpenedBackground = true;
        CreateBackground();
    }

    private void OpenGMOverlay()
    {
        if (_windowService.ShowOpenFileDialog(out string path))
        {
            _gmOverlayBitmap = ImageFactory.FromDrawingBitmap(IO.File.LoadBitmap(path));
            HasOpenGMOverlay = true;
            CreateBackground();
        }
    }

    private void MouseDown(object? sender, MouseButtonDataEventArgs e)
    {
        _mouseDownPosition = e.Position;
        _mouseDown = true;
    }

    private void MouseUp(object? sender, MouseButtonDataEventArgs e)
    {
        if (_fullBackgroundBitmap != null && _mouseDown)
        {
            var distanceX = _mouseDownPosition.X - e.Position.X;
            distanceX = distanceX.Map(0, _mapSize.CanvasWidth, 0, _area.Width);
            _area.X += (int)distanceX;

            var distanceY = _mouseDownPosition.Y - e.Position.Y;
            distanceY = distanceY.Map(0, _mapSize.CanvasWidth, 0, _area.Width);
            _area.Y += (int)distanceY;

            CreateBackground();
        }
    }

    private void ZoomIn(double zoomPercentage)
    {
        if (_fullBackgroundBitmap != null)
        {
            var zoomFactor = (100 + zoomPercentage) / 100;
            ZoomWithZoomFactor(zoomFactor);
            CreateBackground();
        }
    }

    private void ZoomOut(double zoomPercentage)
    {
        if (_fullBackgroundBitmap != null)
        {
            var zoomFactor = (100 + zoomPercentage) / 100;
            zoomFactor = 1 / zoomFactor;
            ZoomWithZoomFactor(zoomFactor);
            CreateBackground();
        }
    }

    private void ZoomWithZoomFactor(double zoomFactor)
    {
        var newWidth = _area.Width / zoomFactor;
        var newHeight = _area.Height / zoomFactor;
        _area.X += (int)Math.Round((_area.Width - newWidth) / 2);
        _area.Y += (int)Math.Round((_area.Height - newHeight) / 2);
        _area.Width = (int)Math.Round(newWidth);
        _area.Height = (int)Math.Round(newHeight);
    }

    private void FitToGrid()
    {
        // A background grid cell can have a gridsize of can be multiple gridcells
        // E.g. background grid cell might equal 10 ft
        var gridCellsPerBackgroundGridCell = Math.Round((double)FeetPerGridCell / Constants.FeetPerGridCell);
        gridCellsPerBackgroundGridCell = Math.Max(gridCellsPerBackgroundGridCell, 1);

        // Resize background to match grid size
        var newSize = new Size<double>(GridSize * gridCellsPerBackgroundGridCell * GridCellsWidth, GridSize * gridCellsPerBackgroundGridCell * GridCellsHeight);
        double factor = _fullBackgroundBitmap.Width / newSize.Width;
        var newAreaSize = new Size<double>(Math.Round(_mapSize.Width * factor), Math.Round(_mapSize.Height * factor));
        _area.X += (int)Math.Round((_area.Width - newAreaSize.Width) / 2);
        _area.Y += (int)Math.Round((_area.Height - newAreaSize.Height) / 2);
        _area.Width = (int)Math.Round(newAreaSize.Width);
        _area.Height = (int)Math.Round(newAreaSize.Height);

        // Move background grid to 0,0
        double backgroundGridSize = _fullBackgroundBitmap.Width / GridCellsWidth;
        _area.X += (int)Math.Round(backgroundGridSize - (_area.X % backgroundGridSize));
        _area.Y += (int)Math.Round(backgroundGridSize - (_area.Y % backgroundGridSize));

        // Move background grid to overlap with normal grid
        var gridOffset = Point<double>.Create(Mathematics.CalculateGridOffset(GridSize));
        _area.X -= (int)Math.Round(gridOffset.X.Map(0, _mapSize.Width, 0, _area.Width));
        _area.Y -= (int)Math.Round(gridOffset.Y.Map(0, _mapSize.Height, 0, _area.Height));

        CreateBackground();
    }

    private void NotifyGridSizeChanged(int newGridSize)
    {
        OnGridSizeChanged?.Invoke(this, new GridSizeChangedEventArgs() { NewGridSize = newGridSize });
    }

    private void NotifyBackgroundUpdated()
    {
        OnBackgroundUpdated?.Invoke(this, new EventArgs());
    }

    private void CreateBackground()
    {
        if (_pauseBitmapCreation)
            return;

        if (_fullBackgroundBitmap != null)
        {
            var croppedBitmap = BitmapTools.CropBitmap(_fullBackgroundBitmap, _area);
            BackgroundBitmap = BitmapTools.ResizeBitmap(croppedBitmap);

            if (HasOpenGMOverlay)
            {
                // resize to fullbackground
                var gmOverlayResizedBitmap = BitmapTools.ResizeToBitmap(_gmOverlayBitmap, _fullBackgroundBitmap);
                var gmOverlayBitmap = BitmapTools.CropBitmap(gmOverlayResizedBitmap, _area);
                GMOverlayBitmap = BitmapTools.ResizeBitmap(gmOverlayBitmap);
            }
        }

        NotifyBackgroundUpdated();
    }

    private void ExtractGridCells(string fileName)
    {
        GridCellsWidth = 10;
        GridCellsHeight = 10;

        var startIndex = fileName.IndexOf("(");
        if (startIndex != -1)
        {
            var endIndex = fileName[startIndex..].IndexOf(")");
            if (endIndex != -1)
            {
                var size = fileName.Substring(startIndex + 1, endIndex - 1);
                var widthAndHeight = size.ToLower().Split("x");
                if (widthAndHeight.Length == 2)
                {
                    try
                    {
                        GridCellsWidth = int.Parse(widthAndHeight[0]);
                        GridCellsHeight = int.Parse(widthAndHeight[1]);
                    }
                    catch
                    {
                        GridCellsWidth = 10;
                        GridCellsHeight = 10;
                    }
                }
            }
        }
    }

    private void FixRatioRectangleAreaSelected(object? sender, RectangleF rectangle)
    {
        OnZoomAndEnhance?.Invoke(this, new ZoomAndEnhanceEventArgs() { rectangle = rectangle });
    }

    private void GridSizeChanged()
    {
        GridSize = Math.Max(GridSize, Constants.MinGridSize);
        GridSize = Math.Min(GridSize, Constants.MaxGridSize);
        GridBitmap = IsGridShown ? BitmapTools.CreateGrid(GridSize) : BitmapTools.CreateEmptyBitmap();
        NotifyGridSizeChanged(GridSize);
    }

    private void GridShownChanged()
    {
        GridBitmap = IsGridShown ? BitmapTools.CreateGrid(GridSize) : BitmapTools.CreateEmptyBitmap();
        NotifyGridSizeChanged(GridSize);
    }

    private void BackgroundColorChanged()
    {
        CreateBackground();
    }

    private void RotateLeft()
    {
        BitmapTools.RotateBitmap(_fullBackgroundBitmap, BitmapRotation.Rotate270);
        CreateBackground();
    }

    private void RotateRight()
    {
        BitmapTools.RotateBitmap(_fullBackgroundBitmap, BitmapRotation.Rotate90);
        CreateBackground();
    }
}
