using DigitalBattleMap.DataClasses;
using DigitalBattleMap.DrawingShapes;
using DigitalBattleMap.FogShapes;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        _mapSize.OnCanvasSizeChanged += OnCanvasSizeChanged;
        FogShapeCollection.PropertyChanged += OnFogShapeCollectionChanged;
    }

    private void Initialize()
    {
        FogShapeCollection = new();
        FogShapeCollection.OnRenderShapes += (_, _) => NotifyFogShapesUpdated();
        ActiveFogShape = new DrawPolygonFogShape(ApplyActiveFogShape, _mapSize);
        MouseCanvas = new MouseCanvasViewModel();
        MouseCanvas.OnLeftButtonDown += LeftButtonDown;
        MouseCanvas.OnLeftButtonUp += LeftButtonUp;
        MouseCanvas.OnRightButtonDown += RightButtonDown;
        MouseCanvas.OnRightButtonUp += RightButtonUp;
        MouseCanvas.OnMouseMove += MouseMove;
        MouseCanvas.OnMouseWheel += MouseWheel;
        MouseCanvas.Cursor = ActiveFogShape.Cursor;
        MouseCanvas.OnFixRatioRectangleAreaSelected += FixRatioRectangleAreaSelected;
        IsDrawPolygonChecked = true;
    }

    protected override void InitializeCommands()
    {
        DrawPolygonCommand = new RelayCommand(p => DrawFogShape(FogShapeType.DrawPolygon));
        StraightPolygonCommand = new RelayCommand(p => DrawFogShape(FogShapeType.StraightPolygon));
        RectangleCommand = new RelayCommand(p => DrawFogShape(FogShapeType.Rectangle));
        CircleCommand = new RelayCommand(p => DrawFogShape(FogShapeType.Circle));
        NGonCommand = new RelayCommand(p => DrawFogShape(FogShapeType.NGon));
        ClearFogCommand = new RelayCommand(p => ClearFog());
    }

    public event EventHandler OnFogShapeUpdated;
    public event EventHandler<ZoomAndEnhanceEventArgs> OnZoomAndEnhance;

    public MouseCanvasViewModel MouseCanvas { get => Get<MouseCanvasViewModel>(); private set => Set(value); }
    public FogShapeCollection FogShapeCollection { get => Get<FogShapeCollection>(); set => Set(value); }
    public bool IsFogShapeActive { get => Get<bool>(); set => Set(value); }
    public bool IsEditFogShapeActive { get => Get<bool>(); set => Set(value); }
    public FogShape SelectedFogShape { get => Get<FogShape>(); set => Set(value, SelectedShapeChanged); }

    public bool IsDrawPolygonChecked { get => Get<bool>(); set => Set(value); }
    public bool IsStraightPolygonChecked { get => Get<bool>(); set => Set(value); }
    public bool IsRectangleChecked { get => Get<bool>(); set => Set(value); }
    public bool IsCircleChecked { get => Get<bool>(); set => Set(value); }
    public bool IsNGonChecked { get => Get<bool>(); set => Set(value); }

    public BitmapSource DrawIconBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.FogIcons.Draw.png")).ToBitmapImage(); }
    public BitmapSource ZigZagIconBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.FogIcons.Zigzag.png")).ToBitmapImage(); }
    public BitmapSource SquareIconBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.FogIcons.Square.png")).ToBitmapImage(); }
    public BitmapSource CircleIconBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.FogIcons.Circle.png")).ToBitmapImage(); }
    public BitmapSource NGonIconBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.FogIcons.NGon.png")).ToBitmapImage(); }

    public BitmapSource FillIconBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.FogIcons.Fill.png")).ToBitmapImage(); }
    public BitmapSource CutIconBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.FogIcons.Cut.png")).ToBitmapImage(); }
    public BitmapSource SeeIconBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.FogIcons.See.png")).ToBitmapImage(); }
    public BitmapSource SnapIconBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.FogIcons.Snap.png")).ToBitmapImage(); }

    public ICommand DrawPolygonCommand { get; set; }
    public ICommand StraightPolygonCommand { get; set; }
    public ICommand RectangleCommand { get; set; }
    public ICommand CircleCommand { get; set; }
    public ICommand NGonCommand { get; set; }

    public ICommand CancelDrawShapeCommand { get; set; }
    public ICommand ClearFogCommand { get; set; }

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

    public override void Zoom(double zoomFactor)
    {
        var matrix = new Matrix();
        matrix.Translate(-(_mapSize.CanvasWidth / 2), -(_mapSize.CanvasHeight / 2));
        matrix.Scale(zoomFactor, zoomFactor);
        matrix.Translate((_mapSize.CanvasWidth / 2), (_mapSize.CanvasHeight / 2));
        FogShapeCollection.Transform(matrix);
    }

    public void ClearFog()
    {
        FogShapeCollection.Clear();
        ActiveFogShape = CreatePolygonFogShape();
        IsFogShapeActive = false;
        NotifyFogShapesUpdated();
        ToggleOffFogShapeButtons();
        IsDrawPolygonChecked = true;
    }

    public override void AddToSaveFile(SaveFile saveFile)
    {
        foreach ((var shape, var index) in FogShapeCollection.GetFogShapes().WithIndex())
        {
            saveFile.FogShapes.Add(shape);
        }
        saveFile.IsFillFogEnabled = FogShapeCollection.IsFillFogEnabled;
    }

    public override void OpenSaveFile(SaveFile saveFile)
    {

        ClearFog();

        FogShapeCollection.IsFillFogEnabled = saveFile.IsFillFogEnabled;

        foreach (var shape in saveFile.FogShapes)
        {
            shape.SetProperties(ApplyActiveFogShape, _mapSize);
            FogShapeCollection.Add(shape);
        }

        if (!saveFile.CanvasSize.Equals(_mapSize.GetCanvasSize()) && saveFile.CanvasSize.Width != 0)
        {
            var zoomFactor = _mapSize.CanvasWidth / saveFile.CanvasSize.Width;
            var matrix = new Matrix();
            matrix.Scale(zoomFactor, zoomFactor);
            FogShapeCollection.Transform(matrix);
        }

        NotifyFogShapesUpdated();
    }

    /**
     * Initial bitmap is black when 'fill with fog' is enabled else empty.
     * Returns a bitmap with all fog shapes applied. 
     * The shape will be filled in black or cut out depending on the fog shape 'IsFogEnabled' setting.
     */
    public Bitmap GetFogBitmap()
    {
        var bitmap = FogShapeCollection.IsFillFogEnabled ? BitmapTools.CreateBlackBitmap() : BitmapTools.CreateEmptyBitmap();
        BitmapTools.DrawFogShapes(bitmap, FogShapeCollection.GetFogShapes().ToList(), _mapSize.GetCanvasSize());
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
        if (!FogShapeCollection.Contains(ActiveFogShape) && ActiveFogShape.Points.Any())
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
        return new DrawPolygonFogShape(ApplyActiveFogShape, _mapSize)
        {
            SnapToGrid = false,
            IsFogEnabled = true
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
            case FogShapeType.DrawPolygon:
                DrawPolygonShape();
                IsDrawPolygonChecked = true;
                break;
            case FogShapeType.StraightPolygon:
                StraightPolygonShape();
                IsStraightPolygonChecked = true;
                break;
            case FogShapeType.Rectangle:
                RectangleShape();
                IsRectangleChecked = true;
                break;
            case FogShapeType.Circle:
                CircleShape();
                IsCircleChecked = true;
                break;
            case FogShapeType.NGon:
                NGonShape();
                IsNGonChecked = true;
                break;
            default:
                throw new NotImplementedException($"Shape {fogShapeType} is not implemented");
        }
    }

    private void DrawPolygonShape()
    {
        ActiveFogShape = new DrawPolygonFogShape(ApplyActiveFogShape, _mapSize);
    }

    private void StraightPolygonShape()
    {
        ActiveFogShape = new StraightPolygonFogShape(ApplyActiveFogShape, _mapSize);
    }

    private void RectangleShape()
    {
        ActiveFogShape = new RectangleFogShape(ApplyActiveFogShape, _mapSize);
    }

    private void CircleShape()
    {
        ActiveFogShape = new CircleFogShape(ApplyActiveFogShape, _mapSize);
    }

    // todo
    private void NGonShape()
    {
        //ActiveFogShape = new RectangleFogShape(ApplyActiveFogShape, _mapSize);
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

    private void MouseWheel(object? sender, MouseWheelDataEventArgs e)
    {
        ActiveFogShape.MouseWheel(e);
    }

    private void FixRatioRectangleAreaSelected(object? sender, RectangleF rectangle)
    {
        OnZoomAndEnhance?.Invoke(this, new ZoomAndEnhanceEventArgs() { rectangle = rectangle });
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

    private void OnFogShapeCollectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        NotifyFogShapesUpdated();
    }

    private void ToggleOffFogShapeButtons()
    {
        IsDrawPolygonChecked = false;
        IsStraightPolygonChecked = false;
        IsRectangleChecked = false;
        IsCircleChecked = false;
        IsNGonChecked = false;
    }

    public bool GetOverviewBitmap(double zoomFactor, out OverviewBitmap overviewBitmap)
    {
        overviewBitmap = new OverviewBitmap();
        var shapes = FogShapeCollection.GetFogShapes().ToList();
        if (!shapes.Any())
        {
            return false;
        }

        var shapeOverviewBitmaps = new List<OverviewBitmap>();

        foreach (var shape in shapes)
        {
            var shapeOverviewBitmap = new OverviewBitmap();
            var penSize = shape.PenSize.Map(0, _mapSize.CanvasWidth, 0, Constants.MapSize.Width);
            var points = new List<Point<double>>();

            foreach (var point in shape.Points)
            {
                var resizedX = point.X.Map(0, _mapSize.CanvasWidth, 0, Constants.MapSize.Width);
                var resizedY = point.Y.Map(0, _mapSize.CanvasHeight, 0, Constants.MapSize.Height);
                points.Add(new Point<double>(resizedX * zoomFactor, resizedY * zoomFactor));
            }

            shapeOverviewBitmap.Bitmap = BitmapTools.CreateFogShapeOverviewBitmap(points, shape.Color, penSize, shape.IsFogEnabled);

            var shapeMinX = points.Min(t => t.X);
            var shapeMinY = points.Min(t => t.Y);
            shapeMinX -= (penSize / 2);
            shapeMinY -= (penSize / 2);

            shapeOverviewBitmap.OffsetFromOrigin = new Point<int>((int)Math.Round(shapeMinX), (int)Math.Round(shapeMinY));

            shapeOverviewBitmaps.Add(shapeOverviewBitmap);
        }
        overviewBitmap.Bitmap = BitmapTools.CreateShapesOverviewBitmap(shapeOverviewBitmaps);
        overviewBitmap.Bitmap.MakeTransparent(System.Drawing.Color.White);

        // OffsetFromOrigin = top left of player view to top left of shapes bounding box
        // Shape positions are always relative to top left of the player view (=origin)
        var minX = Mathematics.Min(shapeOverviewBitmaps.Select(l => l.OffsetFromOrigin.X));
        var minY = Mathematics.Min(shapeOverviewBitmaps.Select(l => l.OffsetFromOrigin.Y));
        overviewBitmap.OffsetFromOrigin = new Point<int>(minX, minY);

        return true;
    }

    internal void ToggleFog(ToggleFogEventArgs e)
    {
        FogShapeCollection.ToggleFog(e.position);
    }
}
