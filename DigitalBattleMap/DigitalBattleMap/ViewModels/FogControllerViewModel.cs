using DigitalBattleMap.DataClasses;
using DigitalBattleMap.FogShapes;
using DigitalBattleMap.Imaging;
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
        IsSnapToGridEnabled = false;
        ActiveFogShape = new DrawPolygonFogShape(ApplyActiveFogShape, _mapSize, !FogShapeCollection.IsFillFogEnabled, IsSnapToGridEnabled);
        ActiveFogShape.OnControlUpdated += NotifyControlUpdated;
        MouseCanvas = new MouseCanvasViewModel();
        MouseCanvas.OnLeftButtonDown += LeftButtonDown;
        MouseCanvas.OnLeftButtonUp += LeftButtonUp;
        MouseCanvas.OnRightButtonUp += RightButtonUp;
        MouseCanvas.OnMouseMove += MouseMove;
        MouseCanvas.OnMouseWheel += MouseWheel;
        MouseCanvas.Cursor = ActiveFogShape.Cursor;
        MouseCanvas.OnFixRatioRectangleAreaSelected += FixRatioRectangleAreaSelected;
        IsDrawPolygonChecked = true;
    }

    protected override void InitializeCommands()
    {
        EnableAllCommand = new RelayCommand(p => EnableAllFog(true));
        DisableAllCommand = new RelayCommand(p => EnableAllFog(false));
        DrawPolygonCommand = new RelayCommand(p => DrawFogShape(FogShapeType.DrawPolygon));
        AngularPolygonCommand = new RelayCommand(p => DrawFogShape(FogShapeType.AngularPolygon));
        RectangleCommand = new RelayCommand(p => DrawFogShape(FogShapeType.Rectangle));
        CircleCommand = new RelayCommand(p => DrawFogShape(FogShapeType.Circle));
        NGonCommand = new RelayCommand(p => DrawFogShape(FogShapeType.NGon));
        ClearFogCommand = new RelayCommand(p => ClearFog());
    }

    public event EventHandler OnFogShapeUpdated;
    public event EventHandler<ControlInfoEventArgs> OnControlUpdated;
    public event EventHandler<ZoomAndEnhanceEventArgs> OnZoomAndEnhance;

    public MouseCanvasViewModel MouseCanvas { get => Get<MouseCanvasViewModel>(); private set => Set(value); }
    public FogShapeCollection FogShapeCollection { get => Get<FogShapeCollection>(); set => Set(value); }
    public bool IsSnapToGridEnabled { get => Get<bool>(); set => Set(value, SnapToGridChanged); }
    public FogShape SelectedFogShape { get => Get<FogShape>(); set => Set(value, SelectedShapeChanged); }

    public bool IsDrawPolygonChecked { get => Get<bool>(); set => Set(value); }
    public bool IsAngularPolygonChecked { get => Get<bool>(); set => Set(value); }
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

    public ICommand EnableAllCommand { get; set; }
    public ICommand DisableAllCommand { get; set; }

    public ICommand DrawPolygonCommand { get; set; }
    public ICommand AngularPolygonCommand { get; set; }
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
        DrawPolygonShape();
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
    public IImage GetFogBitmap()
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
        NotifyFogShapesUpdated();
    }

    private void NotifyFogShapesUpdated()
    {
        if (_pauseBitmapCreation)
            return;

        OnFogShapeUpdated?.Invoke(this, new EventArgs());
    }

    private void NotifyControlUpdated(object? sender, ControlInfoEventArgs e)
    {
        OnControlUpdated.Invoke(sender, e);
    }

    private void SnapToGridChanged()
    { 
        if (ActiveFogShape != null)
        {
            ActiveFogShape.SnapToGrid = IsSnapToGridEnabled;
        }
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
    private void EnableAllFog(bool isEnabled)
    {
        foreach (FogShape item in FogShapeCollection)
        {
            item.IsFogEnabled = isEnabled;
        }
    }

    private void DrawFogShape(FogShapeType fogShapeType)
    {
        ToggleOffFogShapeButtons();

        switch (fogShapeType)
        {
            case FogShapeType.DrawPolygon:
                DrawPolygonShape();
                IsDrawPolygonChecked = true;
                break;
            case FogShapeType.AngularPolygon:
                AngularPolygonShape();
                IsAngularPolygonChecked = true;
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
        ActiveFogShape = new DrawPolygonFogShape(ApplyActiveFogShape, _mapSize, !FogShapeCollection.IsFillFogEnabled, IsSnapToGridEnabled);
        ActiveFogShape.OnControlUpdated += NotifyControlUpdated;
        ActiveFogShape.UpdateControls();
    }

    private void AngularPolygonShape()
    {
        ActiveFogShape = new AngularPolygonFogShape(ApplyActiveFogShape, _mapSize, !FogShapeCollection.IsFillFogEnabled, IsSnapToGridEnabled);
        ActiveFogShape.OnControlUpdated += NotifyControlUpdated;
        ActiveFogShape.UpdateControls();
    }

    private void RectangleShape()
    {
        ActiveFogShape = new RectangleFogShape(ApplyActiveFogShape, _mapSize, !FogShapeCollection.IsFillFogEnabled, IsSnapToGridEnabled);
        ActiveFogShape.OnControlUpdated += NotifyControlUpdated;
        ActiveFogShape.UpdateControls();
    }

    private void CircleShape()
    {
        ActiveFogShape = new CircleFogShape(ApplyActiveFogShape, _mapSize, !FogShapeCollection.IsFillFogEnabled, IsSnapToGridEnabled);
        ActiveFogShape.OnControlUpdated += NotifyControlUpdated;
        ActiveFogShape.UpdateControls();
    }

    private void NGonShape()
    {
        ActiveFogShape = new NGonFogShape(ApplyActiveFogShape, _mapSize, !FogShapeCollection.IsFillFogEnabled, IsSnapToGridEnabled);
        ActiveFogShape.OnControlUpdated += NotifyControlUpdated;
        ActiveFogShape.UpdateControls();
    }

    private void LeftButtonDown(object? sender, MouseButtonDataEventArgs e)
    {
        ActiveFogShape.LeftButtonDown(e);
    }

    private void LeftButtonUp(object? sender, MouseButtonDataEventArgs e)
    {
        ActiveFogShape.LeftButtonUp(e);
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
        ActiveFogShape.IsFogEnabled = !FogShapeCollection.IsFillFogEnabled;
        NotifyFogShapesUpdated();
    }

    private void ToggleOffFogShapeButtons()
    {
        IsDrawPolygonChecked = false;
        IsAngularPolygonChecked = false;
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

        var shapeOverviewBitmaps = new List<FogOverviewBitmap>();
        foreach (var shape in shapes)
        {
            var shapeOverviewBitmap = new FogOverviewBitmap();
            var penSize = shape.PenSize.Map(0, _mapSize.CanvasWidth, 0, Constants.MapSize.Width);
            var points = new List<Point<double>>();
            foreach (var point in shape.Points)
            {
                var resizedX = point.X.Map(0, _mapSize.CanvasWidth, 0, Constants.MapSize.Width);
                var resizedY = point.Y.Map(0, _mapSize.CanvasHeight, 0, Constants.MapSize.Height);
                points.Add(new Point<double>(resizedX * zoomFactor, resizedY * zoomFactor));
            }

            var shapeMinX = points.Min(t => t.X) - (penSize / 2);
            var shapeMinY = points.Min(t => t.Y) - (penSize / 2);

            shapeOverviewBitmap.Bitmap = BitmapTools.CreateFogShapeOverviewBitmap(points, shape, penSize);
            shapeOverviewBitmap.OffsetFromOrigin = new Point<int>((int)Math.Round(shapeMinX), (int)Math.Round(shapeMinY));
            shapeOverviewBitmap.IsFogEnabled = shape.IsFogEnabled;

            // Points adjusted into the full bitmap coordinate space (origin = player view top left)
            shapeOverviewBitmap.ScaledPoints = points.Select(p => new PointF(
                (float)(p.X - shapeMinX + shapeOverviewBitmap.OffsetFromOrigin.X),
                (float)(p.Y - shapeMinY + shapeOverviewBitmap.OffsetFromOrigin.Y)))
                .ToList();

            shapeOverviewBitmaps.Add(shapeOverviewBitmap);
        }
        var playerViewWidth = (int)Math.Round(Constants.MapSize.Width * zoomFactor);
        var playerViewHeight = (int)Math.Round(Constants.MapSize.Height * zoomFactor);

        var fullWidth = _mapSize.BackgroundWidth.HasValue
            ? Math.Max(playerViewWidth, _mapSize.BackgroundWidth.Value)
            : playerViewWidth;
        var fullHeight = _mapSize.BackgroundHeight.HasValue
            ? Math.Max(playerViewHeight, _mapSize.BackgroundHeight.Value)
            : playerViewHeight;

        var backgroundOffset = _mapSize.BackgroundOffset ?? new Point<int>(0, 0);
        overviewBitmap.OffsetFromOrigin = backgroundOffset;

        overviewBitmap.Bitmap = BitmapTools.CreateFogOverviewBitmap(
            shapeOverviewBitmaps,
            FogShapeCollection.IsFillFogEnabled,
            fullWidth,
            fullHeight,
            backgroundOffset);

        overviewBitmap.OffsetFromOrigin = backgroundOffset;

        return true;
    }

    internal void ToggleFog(ToggleFogEventArgs e)
    {
        FogShapeCollection.ToggleFog(e.position);
    }
}
