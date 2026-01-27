using DigitalBattleMap.DataClasses;
using DigitalBattleMap.FogShapes;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DigitalBattleMap.ViewModels;

public class FogControllerViewModel : ControllerViewModelBase
{

    public FogControllerViewModel()
    {
        Initialize();
    }

    public FogControllerViewModel(IWindowService windowService, IMapSize mapSize, Settings settings) : base(mapSize)
    {
        Initialize();

        mapSize.OnCanvasSizeChanged += OnCanvasSizeChanged;
    }

    private void Initialize()
    {
        FogShapeCollection = new();
        FogShapeCollection.OnRenderShapes += (_, _) => NotifyFogShapesUpdated();
        ActiveFogShape = new PolygonFogShape(ApplyActiveFogShape, _mapSize);
        MouseCanvas = new MouseCanvasViewModel();
        MouseCanvas.OnLeftButtonDown += LeftButtonDown;
        MouseCanvas.OnLeftButtonUp += LeftButtonUp;
        MouseCanvas.OnRightButtonDown += RightButtonDown;
        MouseCanvas.OnRightButtonUp += RightButtonUp;
        MouseCanvas.OnMouseMove += MouseMove;
        MouseCanvas.Cursor = ActiveFogShape.Cursor;
        MouseCanvas.OnFixRatioRectangleAreaSelected += FixRatioRectangleAreaSelected;
        IsPolygonChecked = true;
    }

    protected override void InitializeCommands()
    {
        DrawPolygonCommand = new RelayCommand(p => DrawFogShape(FogShapeType.Polygon));
        DrawRectangleCommand = new RelayCommand(p => DrawFogShape(FogShapeType.Rectangle));
        CancelDrawShapeCommand = new RelayCommand(p => CancelDrawShape());
        ClearFogCommand = new RelayCommand(p => ClearFog());
    }

    public event EventHandler OnFogShapeUpdated;
    public event EventHandler<ZoomAndEnhanceEventArgs> OnZoomAndEnhance;

    public MouseCanvasViewModel MouseCanvas { get => Get<MouseCanvasViewModel>(); private set => Set(value); }
    public FogShapeCollection FogShapeCollection { get => Get<FogShapeCollection>(); set => Set(value); }
    public bool IsFogShapeActive { get => Get<bool>(); set => Set(value); }
    public bool IsEditFogShapeActive { get => Get<bool>(); set => Set(value); }
    public FogShape SelectedFogShape { get => Get<FogShape>(); set => Set(value, SelectedShapeChanged); }

    public bool IsPolygonChecked { get => Get<bool>(); set => Set(value); }
    public bool IsRectangleChecked { get => Get<bool>(); set => Set(value); }

    public ICommand DrawPolygonCommand { get; set; }
    public ICommand DrawRectangleCommand { get; set; }
    public ICommand CancelDrawShapeCommand { get; set; }
    public ICommand ClearFogCommand { get; set; }

    public void ClearFog()
    {
        //IsGridShown = true;
        //GridSize = _settings.DefaultGridSize;
        //GridSizeChanged();

        //_fullBackgroundBitmap = null;
        //_gmOverlayBitmap = null;
        //_area = new Rectangle(0, 0, _mapSize.Width, _mapSize.Height);
        //BackgroundBitmap = BitmapTools.CreateEmptyBitmap();
        //FogOfWarBitmap = BitmapTools.CreateEmptyBitmap();
        //GMOverlayBitmap = BitmapTools.CreateEmptyBitmap();
        //BackgroundAndFogOfWarBitmap = BitmapTools.CreateEmptyBitmap();
        //_fogOfWarAreas.Clear();
        //GridCellsWidth = 10;
        //GridCellsHeight = 10;
        //FeetPerGridCell = Constants.FeetPerGridCell;
        //HasOpenedBackground = false;
        //HasOpenGMOverlay = false;
        //IsFogOfWarEnabled = false;
        //FogRemovalRectangleShape = true; // todo change to new setup
        //FogRemovalPolygonShape = false;
        //ZoomSize = Constants.DefaultZoomSize;
        //NotifyBackgroundUpdated();
    }

    public override void AddToSaveFile(SaveFile saveFile)
    {
        //saveFile.IsGridShown = IsGridShown;
        //saveFile.GridSize = GridSize;
        //saveFile.FullBackground = _fullBackgroundBitmap;
        //saveFile.GMOverlay = _gmOverlayBitmap;
        ////saveFile.BackgroundArea = _area;
        //saveFile.GridCellsWidth = GridCellsWidth;
        //saveFile.GridCellsHeight = GridCellsHeight;
        //saveFile.BackgroundFeetPerGridCell = FeetPerGridCell;
        //saveFile.IsFogOfWarEnabled = IsFogOfWarEnabled;
        //saveFile.FogOfWarAreas = _fogOfWarAreas;
    }

    public override void OpenSaveFile(SaveFile saveFile)
    {
        //ClearBackground();

        //IsGridShown = saveFile.IsGridShown;
        //GridSize = saveFile.GridSize;
        //GridSizeChanged();

        //GridCellsWidth = saveFile.GridCellsWidth;
        //GridCellsHeight = saveFile.GridCellsHeight;
        //FeetPerGridCell = saveFile.BackgroundFeetPerGridCell;
        //IsFogOfWarEnabled = saveFile.IsFogOfWarEnabled;
        //_fogOfWarAreas = saveFile.FogOfWarAreas;

        //if (saveFile.FullBackground != null)
        //{
        //    _fullBackgroundBitmap = saveFile.FullBackground;
        //    //_area = saveFile.BackgroundArea;
        //    HasOpenedBackground = true;
        //}

        //if (saveFile.GMOverlay != null)
        //{
        //    _gmOverlayBitmap = saveFile.GMOverlay;
        //    HasOpenGMOverlay = true;
        //}

        //CreateBackground();
    }

    /**
     * Returns a bitmap with all shapes internaly filled in to black.
     */
    public Bitmap GetShowMapFogBitmap()
    {
        var bitmap = BitmapTools.CreateEmptyBitmap();
        BitmapTools.DrawHiddenFogShapes(bitmap, FogShapeCollection.GetFogShapes().ToList<IShape>(), _mapSize.GetCanvasSize());
        return bitmap;
    }

    public FogShape ActiveFogShape
    {
        get => Get<FogShape>();
        set
        {
            var oldValue = Get<FogShape>();
            if (oldValue != null)
            {
                oldValue.PropertyChanged -= ActiveShapePropertyChanged;
                oldValue.OnRenderChanged -= OnActiveShapeRenderChanged;
            }

            Set(value);

            if (value != null)
            {
                value.PropertyChanged += ActiveShapePropertyChanged;
                value.OnRenderChanged += OnActiveShapeRenderChanged;
            }
        }
    }

    private void ActiveShapePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FogShape.Cursor))
        {
            MouseCanvas.Cursor = ActiveFogShape.Cursor;
        }
    }

    private void OnActiveShapeRenderChanged(object? sender, EventArgs e)
    {
        NotifyFogShapesUpdated();
    }

    private void ApplyActiveFogShape()
    {
        if (!FogShapeCollection.Contains(ActiveFogShape))
        {
            FogShapeCollection.Add(ActiveFogShape);
        }

        ActiveFogShape = ActiveFogShape.Clone();
        IsFogShapeActive = false;
        IsEditFogShapeActive = false;
        NotifyFogShapesUpdated();
    }

    private FogShape CreatePolygonFogShape()
    {
        return new RectangleFogShape(ApplyActiveFogShape, _mapSize)
        {
            SnapToGrid = false
        };
    }

    private void NotifyFogShapesUpdated()
    {
        if (_pauseBitmapCreation)
            return;

        OnFogShapeUpdated?.Invoke(this, new EventArgs());
    }

    private void SelectedShapeChanged()
    {
        if (SelectedFogShape != null)
        {
            var points = SelectedFogShape.Points.ToList();
            SelectedFogShape.Points.Clear();
            Task.Run(() =>
            {
                Thread.Sleep(150);
                Application.Current.Dispatcher.Invoke(() => { SelectedFogShape.Points = new ObservableCollection<Point<double>>(points); }, DispatcherPriority.Normal);
            });
        }
    }

    private void DrawFogShape(FogShapeType fogShapeType)
    {
        IsFogShapeActive = true;
        IsEditFogShapeActive = false;
        ToggleOffFogShapeButtons();

        switch (fogShapeType)
        {
            case FogShapeType.Polygon:
                DrawPolygonShape();
                IsPolygonChecked = true;
                break;
            case FogShapeType.Rectangle:
                DrawRectangleShape();
                IsRectangleChecked = true;
                break;
            default:
                throw new NotImplementedException($"Shape {fogShapeType} is not implemented");
        }
    }

    private void DrawPolygonShape()
    {
        ActiveFogShape = new PolygonFogShape(ApplyActiveFogShape, _mapSize);
    }

    private void DrawRectangleShape()
    {
        ActiveFogShape = new RectangleFogShape(ApplyActiveFogShape, _mapSize);
    }

    private void CancelDrawShape()
    {
        ActiveFogShape = CreatePolygonFogShape();
        IsFogShapeActive = false;
    }

    private void LeftButtonDown(object? sender, MouseButtonDataEventArgs e)
    {
        ActiveFogShape.LeftButtonDown(e);
    }

    private void LeftButtonUp(object? sender, MouseButtonDataEventArgs e)
    {
         ActiveFogShape.LeftButtonUp(e);
    }

    private void RightButtonDown(object? sender, MouseButtonDataEventArgs e)
    {
        ActiveFogShape.RightButtonDown(e);
    }

    private void RightButtonUp(object? sender, MouseButtonDataEventArgs e)
    {
        ActiveFogShape.RightButtonUp(e);
    }

    private void MouseMove(object? sender, MouseMoveDataEventArgs e)
    {
        ActiveFogShape.MouseMove(e);
    }

    private void FixRatioRectangleAreaSelected(object? sender, RectangleF rectangle)
    {
        OnZoomAndEnhance?.Invoke(this, new ZoomAndEnhanceEventArgs() { rectangle = rectangle });
    }

    public override void Zoom(double zoomFactor)
    {
        var matrix = new Matrix();
        matrix.Translate(-(_mapSize.CanvasWidth / 2), -(_mapSize.CanvasHeight / 2));
        matrix.Scale(zoomFactor, zoomFactor);
        matrix.Translate((_mapSize.CanvasWidth / 2), (_mapSize.CanvasHeight / 2));
        FogShapeCollection.Transform(matrix);
    }

    public override void Move(ArrowDirection direction, int movementCount)
    {
        var matrix = new Matrix();
        double gridSize = _mapSize.GridSize * movementCount;
        var distanceX = gridSize.Map(0, _mapSize.Width, 0, _mapSize.CanvasWidth);
        var distanceY = gridSize.Map(0, _mapSize.Height, 0, _mapSize.CanvasHeight);

        switch (direction)
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

        FogShapeCollection.Transform(matrix);
        NotifyFogShapesUpdated();
    }

    protected override void CreateBitmap()
    {
        NotifyFogShapesUpdated();
    }

    private void OnCanvasSizeChanged(object? sender, CanvasSizeChangedEventArgs eventArgs)
    {
        if (eventArgs.OldSize != null && !eventArgs.OldSize.Equals(eventArgs.NewSize))
        {
            var zoomFactor = eventArgs.NewSize.Width / eventArgs.OldSize.Width;

            foreach (var shape in FogShapeCollection.GetFogShapes())
            {
                shape.PenSize *= zoomFactor;
            }

            var matrix = new Matrix();
            matrix.Scale(zoomFactor, zoomFactor);
            FogShapeCollection.Transform(matrix);

            NotifyFogShapesUpdated();
        }
    }

    private void ToggleOffFogShapeButtons()
    {
        IsPolygonChecked = false;
        IsRectangleChecked = false;
    }
}
