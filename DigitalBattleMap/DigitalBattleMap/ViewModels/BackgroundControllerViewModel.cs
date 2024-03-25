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
    private Bitmap _fogOfWarBitMap;
    private Bitmap _backgroundAndFogOfWarBitmap;
    private Bitmap _fullBackgroundBitmap;
    private Bitmap _gridBitmap;
    private BitmapSource _backgroundAndFogOfWarBitmapSource;
    private Rectangle _area;
    private FogOfWarArea _selectedFogOfWarArea;
    private IWindowService _windowService;
    private IMouseCanvas _mouseCanvas;
    private Point<double> _mouseDownPosition;
    private bool _mouseDown;
    private List<FogOfWarArea> _fogOfWarAreas = new();
    private Settings _settings;

    public BackgroundControllerViewModel()
    {
        GridSize = 65;
        Initialize();
    }

    public BackgroundControllerViewModel(IWindowService windowService, ICanvasSize canvasSize, IMouseCanvas mouseCanvas, Settings settings) : base(canvasSize)
    {
        _windowService = windowService;
        _mouseCanvas = mouseCanvas;
        _settings = settings;
        GridSize = _settings.DefaultGridSize;
        _mouseCanvas.SubscribeLeftButtonDown(TabIndex.Background, MouseDown);
        _mouseCanvas.SubscribeLeftButtonUp(TabIndex.Background, MouseUp);
        _mouseCanvas.SubscribeRectangleAreaSelected(TabIndex.Background, FogOfWarRectangleAreaSelected);
        _mouseCanvas.SubscribePolygonAreaSelected(TabIndex.Background, FogOfWarPolygonAreaSelected);
        Initialize();
    }

    private void Initialize()
    {
        _area = new Rectangle(0, 0, Constants.BitmapSize.Width, Constants.BitmapSize.Height);
        RegisterPropertyChangedWatcher(nameof(IsBackgroundEditingAllowed), new List<string>() { nameof(HasOpenedBackground), nameof(IsFogOfWarEnabled) });

        IsGridShown = true;
        GridBitmap = BitmapTools.CreateGrid(GridSize);
        BackgroundBitmap = BitmapTools.CreateEmptyBitmap();
        FogOfWarBitmap = BitmapTools.CreateEmptyBitmap();
        BackgroundAndFogOfWarBitmap = BitmapTools.CreateEmptyBitmap();
        BackgroundZoomPercentage = 10;
        GridCellsWidth = 10;
        GridCellsHeight = 10;
        FeetPerGridCell = Constants.FeetPerGridCell;
        FogRemovalRectangleShape = true;
        ZoomSize = Constants.DefaultZoomSize;
    }

    protected override void InitializeCommands()
    {
        OpenBackgroundCommand = new RelayCommand(p => OpenBackground());
        ClearBackgroundCommand = new RelayCommand(p => ClearBackground());
        BackgroundZoomInCommand = new RelayCommand(p => ZoomIn(BackgroundZoomPercentage));
        BackgroundZoomOutCommand = new RelayCommand(p => ZoomOut(BackgroundZoomPercentage));
        FitBackgroundToGridCommand = new RelayCommand(p => FitToGrid());
        ApplyFogRemovalCommand = new RelayCommand(p => ApplyFogRemoval());
        CancelFogRemovalCommand = new RelayCommand(p => CancelFogRemoval());
        ClearFogCommand = new RelayCommand(p => ClearFog());
        GridSizeEnterCommand = new RelayCommand(p => GridSizeChanged());
    }

    public event EventHandler OnBackgroundUpdated;
    public event EventHandler<GridSizeChangedEventArgs> OnGridSizeChanged;

    public bool IsBackgroundEditingAllowed { get => HasOpenedBackground && !IsFogOfWarEnabled; }
    public bool HasOpenedBackground { get => Get<bool>(); set => Set(value); }
    public bool IsFogOfWarEnabled { get => Get<bool>(); set => Set(value, IsFogOfWarEnabledChanged); }
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
    public BitmapSource FogOfWarBitmapSource { get => Get<BitmapSource>(); private set => Set(value); }
    public BitmapSource GridBitmapSource { get => Get<BitmapSource>(); private set => Set(value); }
    public string BackgroundZoomPercentageLabel { get => $"{BackgroundZoomPercentage}%"; }

    public ICommand OpenBackgroundCommand { get; set; }
    public ICommand ClearBackgroundCommand { get; set; }
    public ICommand BackgroundZoomInCommand { get; set; }
    public ICommand BackgroundZoomOutCommand { get; set; }
    public ICommand FitBackgroundToGridCommand { get; set; }
    public ICommand CancelFogRemovalCommand { get; set; }
    public ICommand ApplyFogRemovalCommand { get; set; }
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

    public BitmapSource GetBackGroundBitmapSource()
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

    public void SetSelectedTabIndex(int tabIndex)
    {
        if (tabIndex != TabIndex.Background && IsFogOfWarEnabled)// IsRemovingFog)
        {
            CancelFogRemoval();
        }
    }

    public void OpenBackground()
    {
        if (_windowService.ShowOpenFileDialog(out string path))
        {
            _fullBackgroundBitmap = IO.File.LoadBitmap(path);
            _area = new Rectangle(
                (_fullBackgroundBitmap.Width / 2) - (Constants.BitmapSize.Width / 2),
                (_fullBackgroundBitmap.Height / 2) - (Constants.BitmapSize.Height / 2),
                Constants.BitmapSize.Width,
                Constants.BitmapSize.Height);

            ExtractGridCells(Path.GetFileNameWithoutExtension(path));
            FeetPerGridCell = Constants.FeetPerGridCell;
            HasOpenedBackground = true;
            IsFogOfWarEnabled = false;
            _fogOfWarAreas.Clear();
            CreateBackground();
        }
    }

    public void ClearBackground()
    {
        IsGridShown = true;
        GridSize = _settings.DefaultGridSize;
        GridSizeChanged();

        _fullBackgroundBitmap = null;
        _area = new Rectangle(0, 0, Constants.BitmapSize.Width, Constants.BitmapSize.Height);
        BackgroundBitmap = BitmapTools.CreateEmptyBitmap();
        FogOfWarBitmap = BitmapTools.CreateEmptyBitmap();
        BackgroundAndFogOfWarBitmap = BitmapTools.CreateEmptyBitmap();
        _fogOfWarAreas.Clear();
        GridCellsWidth = 10;
        GridCellsHeight = 10;
        FeetPerGridCell = Constants.FeetPerGridCell;
        HasOpenedBackground = false;
        IsFogOfWarEnabled = false;
        FogRemovalRectangleShape = true;
        FogRemovalPolygonShape = false;
        ZoomSize = Constants.DefaultZoomSize;
        NotifyBackgroundUpdated();
    }

    public void UpdateGridSize(int gridSizeChange)
    {
        GridSize = Math.Max(GridSize + gridSizeChange, Constants.MinimalZoomGridSize);
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
            var distanceX = (int)Math.Round(preciseGridSize.Map(0, Constants.BitmapSize.Width, 0, _area.Width));
            var distanceY = (int)Math.Round(preciseGridSize.Map(0, Constants.BitmapSize.Height, 0, _area.Height));

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

        CreateBackground();
    }

    private void MouseDown(Point<double> point)
    {
        _mouseDownPosition = point;
        _mouseDown = true;
    }

    private void MouseUp(Point<double> point)
    {
        if (_fullBackgroundBitmap != null && _mouseDown)
        {
            var distanceX = _mouseDownPosition.X - point.X;
            distanceX = distanceX.Map(0, _canvasSize.Width, 0, _area.Width);
            _area.X += (int)distanceX;

            var distanceY = _mouseDownPosition.Y - point.Y;
            distanceY = distanceY.Map(0, _canvasSize.Width, 0, _area.Width);
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
        var newAreaSize = new Size<double>(Math.Round(Constants.BitmapSize.Width * factor), Math.Round(Constants.BitmapSize.Height * factor));
        _area.X += (int)Math.Round((_area.Width - newAreaSize.Width) / 2);
        _area.Y += (int)Math.Round((_area.Height - newAreaSize.Height) / 2);
        _area.Width = (int)Math.Round(newAreaSize.Width);
        _area.Height = (int)Math.Round(newAreaSize.Height);

        // Move background grid to 0,0
        double backgroundGridSize = _fullBackgroundBitmap.Width / GridCellsWidth;
        _area.X += (int)Math.Round(backgroundGridSize - (_area.X % backgroundGridSize));
        _area.Y += (int)Math.Round(backgroundGridSize - (_area.Y % backgroundGridSize));

        // Move background grid to overlap with normal grid
        var gridOffset = Point<double>.Create(BitmapTools.CalculateGridOffset(GridSize));
        _area.X -= (int)Math.Round(gridOffset.X.Map(0, Constants.BitmapSize.Width, 0, _area.Width));
        _area.Y -= (int)Math.Round(gridOffset.Y.Map(0, Constants.BitmapSize.Height, 0, _area.Height));

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
        if (_fullBackgroundBitmap != null)
        {
            var croppedBitmap = BitmapTools.CropBitmap(_fullBackgroundBitmap, _area);
            BackgroundBitmap = BitmapTools.ResizeBitmap(croppedBitmap);
        }

        if (IsFogOfWarEnabled)
        {
            var fogOfWarBitMap = BitmapTools.CreateFogOfWarBitmap(_area, _fogOfWarAreas);
            FogOfWarBitmap = BitmapTools.ResizeBitmap(fogOfWarBitMap);
            BackgroundAndFogOfWarBitmap = BitmapTools.MergeBitmaps(new List<Bitmap> { BackgroundBitmap, FogOfWarBitmap });
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
                _mouseCanvas.SetMode(MouseCanvasMode.RectangleSelection);
            }
            else
            {
                _mouseCanvas.SetMode(MouseCanvasMode.PolygonSelection);
            }
        }
        else
        {
            FogOfWarBitmap = BitmapTools.CreateEmptyBitmap();
            CancelFogRemoval();
            _mouseCanvas.SetMode(MouseCanvasMode.Click);
        }
        CreateBackground();
    }

    private void ApplyFogRemoval()
    {
        _fogOfWarAreas.Add(_selectedFogOfWarArea);
        IsFogOfWarAreaSelected = false;
        _mouseCanvas.ResetSelection();
        CreateBackground();
    }

    private void CancelFogRemoval()
    {
        IsFogOfWarAreaSelected = false;
        _mouseCanvas.SetMode(MouseCanvasMode.Click);
    }

    private void ClearFog()
    {
        _fogOfWarAreas.Clear();
        CancelFogRemoval();
        CreateBackground();
    }

    private void FogOfWarRectangleAreaSelected(RectangleF rectangle)
    {
        IsFogOfWarAreaSelected = true;

        var x = ((double)rectangle.X).Map(0, _canvasSize.Width, _area.X, _area.X + _area.Width);
        var y = ((double)rectangle.Y).Map(0, _canvasSize.Height, _area.Y, _area.Y + _area.Height);
        var width = ((double)rectangle.Width).Map(0, _canvasSize.Width, 0, _area.Width);
        var height = ((double)rectangle.Height).Map(0, _canvasSize.Height, 0, _area.Height);

        var fogOfWarArea = new FogOfWarArea();
        fogOfWarArea.Points.Add(new Point<double>(x, y));
        fogOfWarArea.Points.Add(new Point<double>(x + width, y));
        fogOfWarArea.Points.Add(new Point<double>(x + width, y + height));
        fogOfWarArea.Points.Add(new Point<double>(x, y + height));

        _selectedFogOfWarArea = fogOfWarArea;
    }

    private void FogOfWarPolygonAreaSelected(Polygon polygon)
    {
        IsFogOfWarAreaSelected = true;

        var fogOfWarArea = new FogOfWarArea();
        foreach (var point in polygon.Points)
        {
            var mappedPoint = new Point<double>(
                point.X.Map(0, _canvasSize.Width, _area.X, _area.X + _area.Width),
                point.Y.Map(0, _canvasSize.Height, _area.Y, _area.Y + _area.Height));
            fogOfWarArea.Points.Add(mappedPoint);
        }
        _selectedFogOfWarArea = fogOfWarArea;
    }

    private void FogRemovalShapeChanged()
    {
        if (_mouseCanvas != null && IsFogOfWarEnabled)
        {
            if (FogRemovalRectangleShape)
            {
                _mouseCanvas.SetMode(MouseCanvasMode.RectangleSelection);
            }
            else
            {
                _mouseCanvas.SetMode(MouseCanvasMode.PolygonSelection);
            }
        }
    }

    private void GridSizeChanged()
    {
        GridSize = Math.Max(GridSize, Constants.MinimalZoomGridSize);
        GridBitmap = IsGridShown ? BitmapTools.CreateGrid(GridSize) : BitmapTools.CreateEmptyBitmap();
        NotifyGridSizeChanged(GridSize);
    }

    private void GridShownChanged()
    {
        GridBitmap = IsGridShown ? BitmapTools.CreateGrid(GridSize) : BitmapTools.CreateEmptyBitmap();
        NotifyGridSizeChanged(GridSize);
    }
}
