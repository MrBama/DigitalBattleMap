using DigitalBattleMap.DataClasses;
using DigitalBattleMap.DrawingShapes;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;

namespace DigitalBattleMap.ViewModels;

public class DrawingControllerViewModel : ControllerViewModelBase
{
    private DrawingButton _selectedDrawingButton = DrawingButton.Black;
    private ITokenLinker _tokenLinker;

    public DrawingControllerViewModel()
    {
        Initialize();
    }

    public DrawingControllerViewModel(IMapSize mapSize, ITokenLinker tokenLinker) : base(mapSize)
    {
        Initialize();

        _tokenLinker = tokenLinker;

        _mapSize.OnCanvasSizeChanged += OnCanvasSizeChanged;
    }

    private void Initialize()
    {
        BlackButtonBitmapSource = BitmapTools.CreateColorButton(GetDrawingButtonColor(DrawingButton.Black).ToDrawingBrush(), true).ToBitmapImage();
        RedButtonBitmapSource = BitmapTools.CreateColorButton(GetDrawingButtonColor(DrawingButton.Red).ToDrawingBrush(), false).ToBitmapImage();
        GreenButtonBitmapSource = BitmapTools.CreateColorButton(GetDrawingButtonColor(DrawingButton.Green).ToDrawingBrush(), false).ToBitmapImage();
        BlueButtonBitmapSource = BitmapTools.CreateColorButton(GetDrawingButtonColor(DrawingButton.Blue).ToDrawingBrush(), false).ToBitmapImage();
        EraserButtonBitmapSource = BitmapTools.CreateEraserButton(false).ToBitmapImage();
        ShapeCollection = new();
        ShapeCollection.OnRenderShapes += (_, _) => NotifyDrawingShapesUpdated();
        ActiveShape = new StrokeDrawingShape(ApplyActiveShape, _tokenLinker, _mapSize);
        MouseCanvas = new MouseCanvasViewModel();
        MouseCanvas.OnLeftButtonDown += LeftButtonDown;
        MouseCanvas.OnLeftButtonUp += LeftButtonUp;
        MouseCanvas.OnRightButtonDown += RightButtonDown;
        MouseCanvas.OnRightButtonUp += RightButtonUp;
        MouseCanvas.OnMouseMove += MouseMove;
        MouseCanvas.Cursor = ActiveShape.Cursor;
        MouseCanvas.OnFixRatioRectangleAreaSelected += FixRatioRectangleAreaSelected;
    }

    protected override void InitializeCommands()
    {
        SelectedDrawingButtonChangedCommand = new RelayCommand(p => SelectedDrawingButtonChanged((string)p));
        ClearDrawingCommand = new RelayCommand(p => ClearDrawings());
        CancelDrawShapeCommand = new RelayCommand(p => CancelDrawShape());
        EditShapeCommand = new RelayCommand(p => EditShape());
        CancelEditShapeCommand = new RelayCommand(p => CancelEditShape());
        ApplyEditShapeCommand = new RelayCommand(p => ApplyEditShape());
        RemoveShapeCommand = new RelayCommand(p => RemoveShape());
        DrawRectangleCommand = new RelayCommand(p => DrawShape(DrawingShapeType.Rectangle));
        DrawCircleCommand = new RelayCommand(p => DrawShape(DrawingShapeType.Circle));
        DrawConeCommand = new RelayCommand(p => DrawShape(DrawingShapeType.Cone));
        DrawLineCommand = new RelayCommand(p => DrawShape(DrawingShapeType.Line));
    }

    public event EventHandler OnDrawingShapesUpdated;
    public event EventHandler<GridSizeZoomAndEnhanceEventArgs> OnGridSizeZoomAndEnhance;

    public BitmapSource BlackButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource RedButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource GreenButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource BlueButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource EraserButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public DrawingShape SelectedShape { get => Get<DrawingShape>(); set => Set(value, SelectedShapeChanged); }
    public DrawingShapeCollection ShapeCollection { get => Get<DrawingShapeCollection>(); set => Set(value); }
    public bool IsDrawShapeActive { get => Get<bool>(); set => Set(value); }
    public bool IsEditShapeActive { get => Get<bool>(); set => Set(value); }
    public MouseCanvasViewModel MouseCanvas { get => Get<MouseCanvasViewModel>(); private set => Set(value); }
    public ICommand SelectedDrawingButtonChangedCommand { get; set; }
    public ICommand ClearDrawingCommand { get; set; }
    public ICommand CancelDrawShapeCommand { get; set; }
    public ICommand EditShapeCommand { get; set; }
    public ICommand CancelEditShapeCommand { get; set; }
    public ICommand ApplyEditShapeCommand { get; set; }
    public ICommand RemoveShapeCommand { get; set; }
    public ICommand DrawRectangleCommand { get; set; }
    public ICommand DrawCircleCommand { get; set; }
    public ICommand DrawConeCommand { get; set; }
    public ICommand DrawLineCommand { get; set; }

    public DrawingShape ActiveShape
    {
        get => Get<DrawingShape>();
        set
        {
            var oldValue = Get<DrawingShape>();
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

    public override void Move(ArrowDirection direction, int movementCount, bool update = true)
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

        ShapeCollection.Transform(matrix);
        NotifyDrawingShapesUpdated();
    }

    public override void AddToSaveFile(SaveFile saveFile)
    {
        foreach ((var shape, var index) in ShapeCollection.GetShapes().WithIndex())
        {
            saveFile.DrawingShapes.Add(shape);

            if (shape.LinkableObject.IsLinked())
            {
                var objectLink = new ObjectLink
                {
                    LinkableObjectType = typeof(DrawingShape),
                    Index = index,
                    TokenIdentifier = shape.LinkableObject.GetLinkIdentifier()
                };
                saveFile.ObjectLinks.Add(objectLink);
            }
        }
    }

    public override void OpenSaveFile(SaveFile saveFile)
    {
        ClearDrawings();

        foreach (var shape in saveFile.DrawingShapes)
        {
            shape.SetProperties(ApplyActiveShape, _tokenLinker, _mapSize);
            ShapeCollection.Add(shape);
        }

        if (!saveFile.CanvasSize.Equals(_mapSize.GetCanvasSize()) && saveFile.CanvasSize.Width != 0)
        {
            var zoomFactor = _mapSize.CanvasWidth / saveFile.CanvasSize.Width;
            var matrix = new Matrix();
            matrix.Scale(zoomFactor, zoomFactor);
            ShapeCollection.Transform(matrix);
        }

        NotifyDrawingShapesUpdated();
    }

    private void OnCanvasSizeChanged(object? sender, CanvasSizeChangedEventArgs eventArgs)
    {
        if (eventArgs.OldSize != null && !eventArgs.OldSize.Equals(eventArgs.NewSize))
        {
            var zoomFactor = eventArgs.NewSize.Width / eventArgs.OldSize.Width;

            foreach (var shape in ShapeCollection.GetShapes())
            {
                shape.PenSize *= zoomFactor;
            }

            var matrix = new Matrix();
            matrix.Scale(zoomFactor, zoomFactor);
            ShapeCollection.Transform(matrix);

            NotifyDrawingShapesUpdated();
        }
    }

    public void OpenObjectLinks(List<ObjectLink> objectLinks)
    {
        foreach (var objectLink in objectLinks)
        {
            if (objectLink.LinkableObjectType == typeof(DrawingShape))
            {
                _tokenLinker.LinkToToken(ShapeCollection.ElementAt(objectLink.Index), objectLink.TokenIdentifier);
            }
        }
    }

    public override void Zoom(double zoomFactor)
    {
        var matrix = new Matrix();
        matrix.Translate(-(_mapSize.CanvasWidth / 2), -(_mapSize.CanvasHeight / 2));
        matrix.Scale(zoomFactor, zoomFactor);
        matrix.Translate((_mapSize.CanvasWidth / 2), (_mapSize.CanvasHeight / 2));
        ShapeCollection.Transform(matrix);
    }

    public Bitmap GetDrawingBitmap()
    {
        var bitmap = BitmapTools.CreateEmptyBitmap();
        BitmapTools.DrawShapes(bitmap, ShapeCollection.GetShapes().ToList(), _mapSize.GetCanvasSize());
        return bitmap;
    }

    public bool GetOverviewBitmap(double zoomFactor, out OverviewBitmap overviewBitmap)
    {
        overviewBitmap = new OverviewBitmap();
        var shapes = ShapeCollection.GetShapes().ToList();
        if (shapes.Count != 0)
        {
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

                shapeOverviewBitmap.Bitmap = BitmapTools.CreateShapeOverviewBitmap(points, shape.Color, penSize);

                var shapeMinX = points.Min(t => t.X);
                var shapeMinY = points.Min(t => t.Y);
                shapeMinX -= (penSize / 2);
                shapeMinY -= (penSize / 2);

                shapeOverviewBitmap.OffsetFromOrigin = new Point<int>((int)Math.Round(shapeMinX), (int)Math.Round(shapeMinY));

                shapeOverviewBitmaps.Add(shapeOverviewBitmap);
            }

            overviewBitmap.Bitmap = BitmapTools.CreateShapesOverviewBitmap(shapeOverviewBitmaps);

            // OffsetFromOrigin = top left of player view to top left of shapes bounding box
            // Shape positions are always relative to top left of the player view (=origin)
            var minX = Mathematics.Min(shapeOverviewBitmaps.Select(l => l.OffsetFromOrigin.X));
            var minY = Mathematics.Min(shapeOverviewBitmaps.Select(l => l.OffsetFromOrigin.Y));
            overviewBitmap.OffsetFromOrigin = new Point<int>(minX, minY);

            return true;
        }

        return false;
    }

    public void ClearDrawings()
    {
        foreach (var shape in ShapeCollection.GetShapes())
        {
            shape.LinkableObject.Dispose();
        }

        ShapeCollection.Clear();
        ActiveShape = CreateStrokeDrawingShape();
        IsDrawShapeActive = false;
        NotifyDrawingShapesUpdated();
    }

    private void SelectedDrawingButtonChanged(string button)
    {
        var drawingButton = Enum.Parse<DrawingButton>(button);

        if (_selectedDrawingButton != drawingButton)
        {
            ChangeDrawingButton(_selectedDrawingButton, drawingButton);
            _selectedDrawingButton = drawingButton;
        }
    }

    private void ChangeDrawingButton(DrawingButton previousDrawingButton, DrawingButton newDrawingButton)
    {
        SetDrawingButtonSelection(previousDrawingButton, false);
        SetDrawingButtonSelection(newDrawingButton, true);

        if (previousDrawingButton == DrawingButton.Eraser)
        {
            ActiveShape = CreateStrokeDrawingShape();
        }
        else if (newDrawingButton == DrawingButton.Eraser)
        {
            SelectEraser();
        }

        ActiveShape.Color = GetDrawingButtonColor(newDrawingButton);
    }

    private void SelectEraser()
    {
        if (IsEditShapeActive)
        {
            ActiveShape.CancelEditShape();
            NotifyDrawingShapesUpdated();
        }

        IsDrawShapeActive = false;
        IsEditShapeActive = false;

        ActiveShape = new EraserDrawingShape(ShapeCollection, _mapSize)
        {
            PenSize = ActiveShape.PenSize
        };
    }

    private void SetDrawingButtonSelection(DrawingButton drawingButton, bool isSelected)
    {
        var color = GetDrawingButtonColor(drawingButton);

        switch (drawingButton)
        {
            case DrawingButton.Black:
                BlackButtonBitmapSource = BitmapTools.CreateColorButton(color.ToDrawingBrush(), isSelected).ToBitmapImage();
                break;
            case DrawingButton.Red:
                RedButtonBitmapSource = BitmapTools.CreateColorButton(color.ToDrawingBrush(), isSelected).ToBitmapImage();
                break;
            case DrawingButton.Green:
                GreenButtonBitmapSource = BitmapTools.CreateColorButton(color.ToDrawingBrush(), isSelected).ToBitmapImage();
                break;
            case DrawingButton.Blue:
                BlueButtonBitmapSource = BitmapTools.CreateColorButton(color.ToDrawingBrush(), isSelected).ToBitmapImage();
                break;
            case DrawingButton.Eraser:
                EraserButtonBitmapSource = BitmapTools.CreateEraserButton(isSelected).ToBitmapImage();
                break;
        }
    }

    private void ApplyActiveShape()
    {
        if (!ShapeCollection.Contains(ActiveShape))
        {
            ShapeCollection.Add(ActiveShape);
        }

        ActiveShape = CreateStrokeDrawingShape();
        IsDrawShapeActive = false;
        IsEditShapeActive = false;
        NotifyDrawingShapesUpdated();
    }

    private Color GetDrawingButtonColor(DrawingButton drawingButton)
    {
        switch (drawingButton)
        {
            case DrawingButton.Black:
                return Color.FromArgb(255, 0, 0, 0);
            case DrawingButton.Red:
                return Color.FromArgb(255, 255, 0, 0);
            case DrawingButton.Green:
                return Color.FromArgb(255, 0, 255, 0);
            case DrawingButton.Blue:
                return Color.FromArgb(255, 0, 0, 255);
            default:
                return Color.FromArgb(255, 255, 0, 0);
        }
    }

    private DrawingButton GetDrawingButton(Color color)
    {
        if (color == Color.FromArgb(255, 0, 0, 0))
        {
            return DrawingButton.Black;
        }
        if (color == Color.FromArgb(255, 255, 0, 0))
        {
            return DrawingButton.Red;
        }
        if (color == Color.FromArgb(255, 0, 255, 0))
        {
            return DrawingButton.Green;
        }
        if (color == Color.FromArgb(255, 0, 0, 255))
        {
            return DrawingButton.Blue;
        }
        else
        {
            return DrawingButton.Black;
        }
    }

    private void LeftButtonDown(object? sender, MouseButtonDataEventArgs e)
    {
        ActiveShape.LeftButtonDown(e);
    }

    private void LeftButtonUp(object? sender, MouseButtonDataEventArgs e)
    {
        ActiveShape.LeftButtonUp(e);
    }

    private void RightButtonDown(object? sender, MouseButtonDataEventArgs e)
    {
        ActiveShape.RightButtonDown(e);
    }

    private void RightButtonUp(object? sender, MouseButtonDataEventArgs e)
    {
        ActiveShape.RightButtonUp(e);
    }

    private void MouseMove(object? sender, MouseMoveDataEventArgs e)
    {
        ActiveShape.MouseMove(e);
    }

    private DrawingShape CreateStrokeDrawingShape()
    {
        var strokeDrawingShapes = ShapeCollection.GetShapes().OfType<StrokeDrawingShape>();

        return new StrokeDrawingShape(ApplyActiveShape, _tokenLinker, _mapSize)
        {
            Color = ActiveShape.Color,
            PenSize = ActiveShape.PenSize,
            SnapToGrid = strokeDrawingShapes.Any() && strokeDrawingShapes.Last().SnapToGrid
        };
    }

    private void NotifyDrawingShapesUpdated()
    {
        OnDrawingShapesUpdated?.Invoke(this, new EventArgs());
    }

    private void DrawShape(DrawingShapeType drawingShapeType)
    {
        if (_selectedDrawingButton == DrawingButton.Eraser)
        {
            SetDrawingButtonSelection(_selectedDrawingButton, false);
            SetDrawingButtonSelection(DrawingButton.Black, true);
            _selectedDrawingButton = DrawingButton.Black;
        }

        IsDrawShapeActive = true;
        IsEditShapeActive = false;

        switch (drawingShapeType)
        {
            case DrawingShapeType.Rectangle:
                DrawRectangleShape();
                break;
            case DrawingShapeType.Circle:
                DrawCircleShape();
                break;
            case DrawingShapeType.Cone:
                DrawConeShape();
                break;
            case DrawingShapeType.Line:
                LineConeShape();
                break;
            default:
                throw new NotImplementedException($"Shape {drawingShapeType} is not implemented");
        }
    }

    private void DrawRectangleShape()
    {
        ActiveShape = new RectangleDrawingShape(ApplyActiveShape, _tokenLinker, _mapSize)
        {
            Color = ActiveShape.Color,
            PenSize = ActiveShape.PenSize
        };
    }

    private void DrawCircleShape()
    {
        ActiveShape = new CircleDrawingShape(ApplyActiveShape, _tokenLinker, _mapSize)
        {
            Color = ActiveShape.Color,
            PenSize = ActiveShape.PenSize
        };
    }

    private void DrawConeShape()
    {
        ActiveShape = new ConeDrawingShape(ApplyActiveShape, _tokenLinker, _mapSize)
        {
            Color = ActiveShape.Color,
            PenSize = ActiveShape.PenSize
        };
    }

    private void LineConeShape()
    {
        ActiveShape = new LineDrawingShape(ApplyActiveShape, _tokenLinker, _mapSize)
        {
            Color = ActiveShape.Color,
            PenSize = ActiveShape.PenSize
        };
    }

    private void CancelDrawShape()
    {
        ActiveShape = CreateStrokeDrawingShape();
        IsDrawShapeActive = false;
    }

    private void SelectedShapeChanged()
    {
        if (SelectedShape != null)
        {
            var points = SelectedShape.Points.ToList();
            SelectedShape.Points.Clear();
            Task.Run(() =>
            {
                Thread.Sleep(150);
                Application.Current.Dispatcher.Invoke(() => { SelectedShape.Points = new ObservableCollection<Point<double>>(points); }, DispatcherPriority.Normal);
            });
        }
    }

    private void EditShape()
    {
        ActiveShape = SelectedShape;
        ActiveShape.EditShape();
        IsDrawShapeActive = false;
        IsEditShapeActive = true;

        var drawingButton = GetDrawingButton(ActiveShape.Color);
        if (_selectedDrawingButton != drawingButton)
        {
            SetDrawingButtonSelection(_selectedDrawingButton, false);
            SetDrawingButtonSelection(drawingButton, true);
            MouseCanvas.Cursor = ActiveShape.Cursor;
            _selectedDrawingButton = drawingButton;
        }
    }

    private void CancelEditShape()
    {
        IsEditShapeActive = false;
        ActiveShape.CancelEditShape();
        ActiveShape = CreateStrokeDrawingShape();
        NotifyDrawingShapesUpdated();
    }

    private void ApplyEditShape()
    {
        IsEditShapeActive = false;
        ActiveShape.ApplyShape();
    }

    private void RemoveShape()
    {
        if (ActiveShape == SelectedShape)
        {
            ActiveShape = CreateStrokeDrawingShape();
        }

        ShapeCollection.Remove(SelectedShape);
        IsEditShapeActive = false;
        NotifyDrawingShapesUpdated();
    }

    private void ActiveShapePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DrawingShape.Cursor))
        {
            MouseCanvas.Cursor = ActiveShape.Cursor;
        }
    }

    private void OnActiveShapeRenderChanged(object? sender, EventArgs e)
    {
        NotifyDrawingShapesUpdated();
    }

    private void FixRatioRectangleAreaSelected(object? sender, RectangleF rectangle)
    {
        OnGridSizeZoomAndEnhance?.Invoke(this, new GridSizeZoomAndEnhanceEventArgs() { rectangle = rectangle });
    }
}
