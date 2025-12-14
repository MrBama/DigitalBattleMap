using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels;

public class BackgroundControllerViewModel : ControllerViewModelBase
{
    private Bitmap _backgroundBitmap;
    private Bitmap _gmOverlayBitmap;
    private Bitmap _fogOfWarBitMap;
    private Bitmap _backgroundAndFogOfWarBitmap;
    private Bitmap _fullBackgroundBitmap;
    private Bitmap _gridBitmap;
    private BitmapSource _backgroundAndFogOfWarBitmapSource;
    private Rectangle _area;
    private SelectedArea _selectedArea;
    private IWindowService _windowService;
    private Point<double> _mouseDownPosition;
    private bool _mouseDown;
    private List<SelectedArea> _fogOfWarAreas = new();
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
        Initialize();
    }

    private void Initialize()
    {
        _area = new Rectangle(0, 0, Constants.MapSize.Width, Constants.MapSize.Height);
        RegisterPropertyChangedWatcher(nameof(IsBackgroundEditingAllowed), new List<string>() { nameof(HasOpenedBackground), nameof(IsFogOfWarEnabled) });

        IsGridShown = true;
        GridBitmap = BitmapTools.CreateGrid(GridSize);
        BackgroundBitmap = BitmapTools.CreateEmptyBitmap();
        FogOfWarBitmap = BitmapTools.CreateEmptyBitmap();
        GMOverlayBitmap = BitmapTools.CreateEmptyBitmap();
        BackgroundAndFogOfWarBitmap = BitmapTools.CreateEmptyBitmap();
        BackgroundZoomPercentage = 10;
        GridCellsWidth = 10;
        GridCellsHeight = 10;
        FeetPerGridCell = Constants.FeetPerGridCell;
        FogRemovalRectangleShape = true;
        ZoomSize = Constants.DefaultZoomSize;
        MouseCanvas = new MouseCanvasViewModel();
        MouseCanvas.OnLeftButtonDown += MouseDown;
        MouseCanvas.OnLeftButtonUp += MouseUp;
        MouseCanvas.OnRectangleAreaSelected += RectangleAreaSelected;
        MouseCanvas.OnPolygonAreaSelected += FogOfWarPolygonAreaSelected;
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
        ApplyFogOfWarCommand = new RelayCommand(p => ApplyFogRemoval());
        CancelFogRemovalCommand = new RelayCommand(p => CancelFogRemoval());
        ClearFogCommand = new RelayCommand(p => ClearFog());
        GridSizeEnterCommand = new RelayCommand(p => GridSizeChanged());
    }

    public event EventHandler OnBackgroundUpdated;
    public event EventHandler<GridSizeChangedEventArgs> OnGridSizeChanged;
    public event EventHandler<ZoomAndEnhanceEventArgs> OnZoomAndEnhance;

    public bool IsBackgroundEditingAllowed { get => HasOpenedBackground && !IsFogOfWarEnabled; }
    public bool HasOpenedBackground { get => Get<bool>(); set => Set(value); }
    public bool IsFogOfWarEnabled { get => Get<bool>(); set => SetWhenChanged(value, IsFogOfWarEnabledChanged); }
    public bool HasOpenGMOverlay { get => Get<bool>(); set => Set(value); }
    public bool IsFogOfWarAreaSelected { get => Get<bool>(); set => Set(value); }
    public bool FogRemovalRectangleShape { get => Get<bool>(); set => Set(value, FogRemovalShapeChanged); }
    public bool FogRemovalPolygonShape { get => Get<bool>(); set => Set(value); }
    public bool IsGridShown { get => Get<bool>(); set => Set(value, GridShownChanged); }
    public int GridCellsWidth { get => Get<int>(); set => Set(value); }
    public int GridCellsHeight { get => Get<int>(); set => Set(value); }
    public int FeetPerGridCell { get => Get<int>(); set => Set(value); }
    public int GridSize { get => Get<int>(); set => Set(value); }
    public int ZoomSize { get => Get<int>(); set => Set(value); }
    public double BackgroundZoomPercentage { get => Get<double>(); set => Set(value, () => NotifyPropertyChange(nameof(BackgroundZoomPercentageLabel))); }
    public BitmapSource BackgroundBitmapSource { get => Get<BitmapSource>(); private set => Set(value); }
    public BitmapSource GMOverlayBitmapSource { get => Get<BitmapSource>(); private set => Set(value); }
    public BitmapSource FogOfWarBitmapSource { get => Get<BitmapSource>(); private set => Set(value); }
    public BitmapSource GridBitmapSource { get => Get<BitmapSource>(); private set => Set(value); }
    public string BackgroundZoomPercentageLabel { get => $"{BackgroundZoomPercentage}%"; }
    public MouseCanvasViewModel MouseCanvas { get => Get<MouseCanvasViewModel>(); private set => Set(value); }

    public ICommand OpenBackgroundCommand { get; set; }
    public ICommand OpenBackgroundFromClipboardCommand { get; set; }
    public ICommand OpenGMOverlayCommand { get; set; }
    public ICommand ClearBackgroundCommand { get; set; }
    public ICommand ClearGMOverlayCommand { get; set; }
    public ICommand BackgroundZoomInCommand { get; set; }
    public ICommand BackgroundZoomOutCommand { get; set; }
    public ICommand FitBackgroundToGridCommand { get; set; }
    public ICommand CancelFogRemovalCommand { get; set; }
    public ICommand ApplyFogOfWarCommand { get; set; }
    public ICommand ClearFogCommand { get; set; }
    public ICommand GridSizeEnterCommand { get; set; }

    private Bitmap BackgroundBitmap
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

    private Bitmap GMOverlayBitmap
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

    private Bitmap FogOfWarBitmap
    {
        get => _fogOfWarBitMap;
        set
        {
            if (value != _fogOfWarBitMap)
            {
                _fogOfWarBitMap = value;
                FogOfWarBitmapSource = value.ToBitmapImage();
            }
        }
    }

    private Bitmap BackgroundAndFogOfWarBitmap
    {
        get => _backgroundAndFogOfWarBitmap;
        set
        {
            if (value != _backgroundAndFogOfWarBitmap)
            {
                _backgroundAndFogOfWarBitmap = value;
                _backgroundAndFogOfWarBitmapSource = value.ToBitmapImage();
            }
        }
    }

    private Bitmap GridBitmap
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

    public Bitmap GetBackgroundBitmap()
    {
        if (IsFogOfWarEnabled)
        {
            return BackgroundAndFogOfWarBitmap;
        }
        else
        {
            return BackgroundBitmap;
        }
    }

    public BitmapSource GetBackgroundBitmapSource()
    {
        if (IsFogOfWarEnabled)
        {
            return _backgroundAndFogOfWarBitmapSource;
        }
        else
        {
            return BackgroundBitmapSource;
        }
    }

    public Bitmap GetGridBitmap()
    {
        return GridBitmap;
    }

    public bool GetOverviewBitmap(out OverviewBitmap overviewBitmap)
    {
        overviewBitmap = new OverviewBitmap();

        if(_fullBackgroundBitmap != null)
        {
            overviewBitmap.Bitmap = new Bitmap(_fullBackgroundBitmap);
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
        _area = new Rectangle(0, 0, _mapSize.Width, _mapSize.Height);
        BackgroundBitmap = BitmapTools.CreateEmptyBitmap();
        FogOfWarBitmap = BitmapTools.CreateEmptyBitmap();
        GMOverlayBitmap = BitmapTools.CreateEmptyBitmap();
        BackgroundAndFogOfWarBitmap = BitmapTools.CreateEmptyBitmap();
        _fogOfWarAreas.Clear();
        GridCellsWidth = 10;
        GridCellsHeight = 10;
        FeetPerGridCell = Constants.FeetPerGridCell;
        HasOpenedBackground = false;
        HasOpenGMOverlay = false;
        IsFogOfWarEnabled = false;
        FogRemovalRectangleShape = true;
        FogRemovalPolygonShape = false;
        ZoomSize = Constants.DefaultZoomSize;
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
        saveFile.FullBackground = _fullBackgroundBitmap;
        saveFile.GMOverlay = _gmOverlayBitmap;
        saveFile.BackgroundArea = _area;
        saveFile.GridCellsWidth = GridCellsWidth;
        saveFile.GridCellsHeight = GridCellsHeight;
        saveFile.BackgroundFeetPerGridCell = FeetPerGridCell;
        saveFile.IsFogOfWarEnabled = IsFogOfWarEnabled;
        saveFile.FogOfWarAreas = _fogOfWarAreas;
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
        IsFogOfWarEnabled = saveFile.IsFogOfWarEnabled;
        _fogOfWarAreas = saveFile.FogOfWarAreas;

        if (saveFile.FullBackground != null)
        {
            _fullBackgroundBitmap = saveFile.FullBackground;
            _area = saveFile.BackgroundArea;
            HasOpenedBackground = true;
        }

        if (saveFile.GMOverlay != null)
        {
            _gmOverlayBitmap = saveFile.GMOverlay;
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
            OpenBackground(IO.File.LoadBitmap(path));
        }
    }

    private void OpenBackgroundFromClipboard()
    {
        var bitmap = IO.File.LoadBitmapFromClipboard();
        if (bitmap != null)
        {
            OpenBackground(bitmap);
        }
    }

    private void OpenBackground(Bitmap bitmap)
    {
        _fullBackgroundBitmap = bitmap;
        _area = new Rectangle(
            (_fullBackgroundBitmap.Width / 2) - (_mapSize.Width / 2),
            (_fullBackgroundBitmap.Height / 2) - (_mapSize.Height / 2),
            _mapSize.Width,
            _mapSize.Height);

        FeetPerGridCell = Constants.FeetPerGridCell;
        HasOpenedBackground = true;
        IsFogOfWarEnabled = false;
        _fogOfWarAreas.Clear();
        CreateBackground();
    }

    private void OpenGMOverlay()
    {
        if (_windowService.ShowOpenFileDialog(out string path))
        {
            _gmOverlayBitmap = IO.File.LoadBitmap(path);
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

        if (IsFogOfWarEnabled)
        {
            var fogOfWarBitMap = BitmapTools.CreateFogOfWarBitmap(_area, _fogOfWarAreas);
            FogOfWarBitmap = BitmapTools.ResizeBitmap(fogOfWarBitMap);
            BackgroundAndFogOfWarBitmap = BitmapTools.MergeBitmaps(BackgroundBitmap, FogOfWarBitmap);
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

    private void IsFogOfWarEnabledChanged()
    {
        if (IsFogOfWarEnabled)
        {
            if (FogRemovalRectangleShape)
            {
                MouseCanvas.SetMode(MouseCanvasMode.RectangleSelection);
            }
            else
            {
                MouseCanvas.SetMode(MouseCanvasMode.PolygonSelection);
            }
        }
        else
        {
            FogOfWarBitmap = BitmapTools.CreateEmptyBitmap();
            CancelFogRemoval();
            MouseCanvas.SetMode(MouseCanvasMode.Click);
        }
        CreateBackground();
    }

    private void ApplyFogRemoval()
    {
        _fogOfWarAreas.Add(_selectedArea);
        IsFogOfWarAreaSelected = false;
        MouseCanvas.ResetSelection();
        CreateBackground();
    }

    private void CancelFogRemoval()
    {
        IsFogOfWarAreaSelected = false;
        MouseCanvas.ResetSelection();
    }

    private void ClearFog()
    {
        _fogOfWarAreas.Clear();
        CancelFogRemoval();
        CreateBackground();
    }

    private void RectangleAreaSelected(object? sender, RectangleF rectangle)
    {
        IsFogOfWarAreaSelected = IsFogOfWarEnabled;

        var x = ((double)rectangle.X).Map(0, _mapSize.CanvasWidth, _area.X, _area.X + _area.Width);
        var y = ((double)rectangle.Y).Map(0, _mapSize.CanvasHeight, _area.Y, _area.Y + _area.Height);
        var width = ((double)rectangle.Width).Map(0, _mapSize.CanvasWidth, 0, _area.Width);
        var height = ((double)rectangle.Height).Map(0, _mapSize.CanvasHeight, 0, _area.Height);

        var fogOfWarArea = new SelectedArea();
        fogOfWarArea.Points.Add(new Point<double>(x, y));
        fogOfWarArea.Points.Add(new Point<double>(x + width, y));
        fogOfWarArea.Points.Add(new Point<double>(x + width, y + height));
        fogOfWarArea.Points.Add(new Point<double>(x, y + height));

        _selectedArea = fogOfWarArea;
    }

    private void FogOfWarPolygonAreaSelected(object? sender, Polygon polygon)
    {
        IsFogOfWarAreaSelected = true;

        var fogOfWarArea = new SelectedArea();
        foreach (var point in polygon.Points)
        {
            var mappedPoint = new Point<double>(
                point.X.Map(0, _mapSize.CanvasWidth, _area.X, _area.X + _area.Width),
                point.Y.Map(0, _mapSize.CanvasHeight, _area.Y, _area.Y + _area.Height));
            fogOfWarArea.Points.Add(mappedPoint);
        }
        _selectedArea = fogOfWarArea;
    }

    private void FixRatioRectangleAreaSelected(object? sender, RectangleF rectangle)
    {
        OnZoomAndEnhance?.Invoke(this, new ZoomAndEnhanceEventArgs() { rectangle = rectangle });
    }

    private void FogRemovalShapeChanged()
    {
        if (MouseCanvas != null && IsFogOfWarEnabled)
        {
            IsFogOfWarAreaSelected = false;

            if (FogRemovalRectangleShape)
            {
                MouseCanvas.SetMode(MouseCanvasMode.RectangleSelection);
            }
            else
            {
                MouseCanvas.SetMode(MouseCanvasMode.PolygonSelection);
            }
        }
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
}
