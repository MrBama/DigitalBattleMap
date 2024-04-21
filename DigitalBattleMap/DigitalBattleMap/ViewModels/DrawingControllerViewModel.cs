using DigitalBattleMap.DataClasses;
using DigitalBattleMap.DrawingShapes;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DigitalBattleMap.ViewModels;

public class DrawingControllerViewModel : ControllerViewModelBase
{
    private DrawingButton _selectedDrawingButton = DrawingButton.Black;
    private ITokenLinker _tokenLinker;
    private int _gridSize;

    public DrawingControllerViewModel()
    {
        Initialize();
    }

    public DrawingControllerViewModel(ICanvasSize canvasSize, ITokenLinker tokenLinker, int gridSize) : base(canvasSize)
    {
        Initialize();

        _tokenLinker = tokenLinker;
        _gridSize = gridSize;

        _canvasSize.OnCanvasSizeChanged += OnCanvasSizeChanged;
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
        ActiveShape = new StrokeDrawingShape(ApplyActiveShape, _tokenLinker, _canvasSize, _gridSize);
        MouseCanvas = new MouseCanvasViewModel();
        MouseCanvas.OnLeftButtonDown += LeftButtonDown;
        MouseCanvas.OnLeftButtonUp += LeftButtonUp;
        MouseCanvas.OnRightButtonDown += RightButtonDown;
        MouseCanvas.OnRightButtonUp += RightButtonUp;
        MouseCanvas.OnMouseMove += MouseMove;
        MouseCanvas.Cursor = ActiveShape.Cursor;
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

    public override void Move(ArrowDirection direction, int movementCount)
    {
        var matrix = new Matrix();
        double gridSize = _gridSize * movementCount;
        var distanceX = gridSize.Map(0, Constants.BitmapSize.Width, 0, _canvasSize.Width);
        var distanceY = gridSize.Map(0, Constants.BitmapSize.Height, 0, _canvasSize.Height);

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
                    TokenIndentifier = shape.LinkableObject.GetLinkIdentifier()
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
            //shape.OnPositionChanged += OnShapePositionChanged;
            shape.SetProperties(ApplyActiveShape, _tokenLinker, _canvasSize, _gridSize);
            ShapeCollection.Add(shape);
        }

        if (!saveFile.CanvasSize.Equals(_canvasSize.GetSize()) && saveFile.CanvasSize.Width != 0)
        {
            var zoomFactor = _canvasSize.Width / saveFile.CanvasSize.Width;
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
                _tokenLinker.LinkToToken(ShapeCollection.ElementAt(objectLink.Index), objectLink.TokenIndentifier);
            }
        }
    }

    public override void Zoom(double zoomFactor)
    {
        var matrix = new Matrix();
        matrix.Translate(-(_canvasSize.Width / 2), -(_canvasSize.Height / 2));
        matrix.Scale(zoomFactor, zoomFactor);
        matrix.Translate((_canvasSize.Width / 2), (_canvasSize.Height / 2));
        ShapeCollection.Transform(matrix);
    }

    public void UpdateGridSize(int gridSize)
    {
        _gridSize = gridSize;
        foreach (var shape in ShapeCollection.GetShapes())
        {
            shape.UpdateGridSize(gridSize);
        }
        ActiveShape.UpdateGridSize(gridSize);
    }

    public System.Drawing.Bitmap GetDrawingBitmap()
    {
        var bitmap = BitmapTools.CreateEmptyBitmap();
        BitmapTools.DrawShapes(bitmap, ShapeCollection.GetShapes().ToList(), _canvasSize.GetSize());
        return bitmap;
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
        if(IsEditShapeActive)
        {
            ActiveShape.CancelEditShape();
        }

        IsDrawShapeActive = false;
        IsEditShapeActive = false;

        ActiveShape = new EraserDrawingShape(ShapeCollection, _canvasSize)
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
        if(color == Color.FromArgb(255, 0, 0, 0))
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
        return new StrokeDrawingShape(ApplyActiveShape, _tokenLinker, _canvasSize, _gridSize)
        {
            Color = ActiveShape.Color,
            PenSize = ActiveShape.PenSize,
            SnapToGrid = ActiveShape.SnapToGrid
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
        ActiveShape = new RectangleDrawingShape(ApplyActiveShape, _tokenLinker, _canvasSize, _gridSize)
        {
            Color = ActiveShape.Color,
            PenSize = ActiveShape.PenSize
        };
    }

    private void DrawCircleShape()
    {
        ActiveShape = new CircleDrawingShape(ApplyActiveShape, _tokenLinker, _canvasSize, _gridSize)
        {
            Color = ActiveShape.Color,
            PenSize = ActiveShape.PenSize
        };
    }

    private void DrawConeShape()
    {
        ActiveShape = new ConeDrawingShape(ApplyActiveShape, _tokenLinker, _canvasSize, _gridSize)
        {
            Color = ActiveShape.Color,
            PenSize = ActiveShape.PenSize
        };
    }

    private void LineConeShape()
    {
        ActiveShape = new LineDrawingShape(ApplyActiveShape, _tokenLinker, _canvasSize, _gridSize)
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

    //private void CreateShapeStroke(Stroke addedStroke)
    //{
    //    var inkCanvasGridSize = CalculateInkCanvasGridSize();
    //    var halfGridSize = inkCanvasGridSize / 2;
    //    var gridOffset = CalculateInkCanvasGridOffset();
    //    if (gridOffset.X - halfGridSize >= 0)
    //    {
    //        gridOffset.X -= halfGridSize;
    //    }
    //    if (gridOffset.Y - halfGridSize >= 0)
    //    {
    //        gridOffset.Y -= halfGridSize;
    //    }

    //    var startPoint = SnapPoint(new Point<double>(addedStroke.StylusPoints.First().X, addedStroke.StylusPoints.First().Y), gridOffset, halfGridSize);
    //    double distanceToEdgeX, distanceToEdgeY;
    //    if (RectangleShapeSelected || CircleShapeSelected)
    //    {
    //        distanceToEdgeX = Math.Round((double)ShapeSizeX / Constants.FeetPerGridCell); // Get amount of grid cells
    //        distanceToEdgeX *= inkCanvasGridSize;
    //        distanceToEdgeY = Math.Round((double)ShapeSizeY / Constants.FeetPerGridCell); // Get amount of grid cells
    //        distanceToEdgeY *= inkCanvasGridSize;
    //        distanceToEdgeX /= 2; // divide by 2 to get radius
    //        distanceToEdgeY /= 2; // divide by 2 to get radius
    //    }
    //    else
    //    {
    //        distanceToEdgeX = Math.Round((double)ShapeSizeX / Constants.FeetPerGridCell); // Get amount of grid cells
    //        distanceToEdgeX *= inkCanvasGridSize;
    //        if (LineShapeSelected)
    //        {
    //            distanceToEdgeY = Math.Round((double)ShapeSizeY / Constants.FeetPerGridCell); // Get amount of grid cells
    //            distanceToEdgeY *= inkCanvasGridSize;
    //        }
    //        else
    //        {
    //            distanceToEdgeY = startPoint.Y;
    //            distanceToEdgeY *= inkCanvasGridSize;
    //        }
    //    }

    //    //direction
    //    var directionPoint = SnapPoint(new Point<double>(addedStroke.StylusPoints.Last().X, addedStroke.StylusPoints.Last().Y), gridOffset, halfGridSize);

    //    Strokes.Remove(ShapeStroke);
    //    addedStroke.StylusPoints = CreateShape(startPoint, new Point<double>(distanceToEdgeX, distanceToEdgeY), directionPoint);
    //    if (LineShapeSelected)
    //    {
    //        distanceToEdgeY = Math.Round((double)ShapeSizeY / Constants.FeetPerGridCell); // Get amount of grid cells
    //        distanceToEdgeY *= inkCanvasGridSize;
    //        addedStroke.DrawingAttributes.Width = distanceToEdgeY;
    //        addedStroke.DrawingAttributes.Height = distanceToEdgeY;
    //        var color = addedStroke.DrawingAttributes.Color;
    //        color.A = 50;
    //        addedStroke.DrawingAttributes.Color = color;
    //    }
    //    ShapeStroke = addedStroke;
    //}

    //private StylusPointCollection CreateShape(Point<double> startPoint, Point<double> distanceToEdge, Point<double> directionPoint)
    //{
    //    var points = new StylusPointCollection();

    //    if (RectangleShapeSelected)
    //    {
    //        points.Add(new StylusPoint(startPoint.X - distanceToEdge.X, startPoint.Y - distanceToEdge.Y));
    //        points.Add(new StylusPoint(startPoint.X + distanceToEdge.X, startPoint.Y - distanceToEdge.Y));
    //        points.Add(new StylusPoint(startPoint.X + distanceToEdge.X, startPoint.Y + distanceToEdge.Y));
    //        points.Add(new StylusPoint(startPoint.X - distanceToEdge.X, startPoint.Y + distanceToEdge.Y));
    //        points.Add(new StylusPoint(startPoint.X - distanceToEdge.X, startPoint.Y - distanceToEdge.Y));
    //    }
    //    else if (ConeShapeSelected)
    //    {
    //        // Find angle of cone
    //        double stepsize = 0.05;
    //        Point<double> point1 = TranslatePoint(distanceToEdge, startPoint);
    //        Point<double> point2 = TranslatePoint(directionPoint, startPoint);
    //        double dot = point1.X * point2.X + point1.Y * point2.Y;    // dot product
    //        double det = point1.X * point2.Y - point1.Y * point2.X;    // determinant
    //        double angle = Math.Atan2(det, dot);             // -pi < 0 < pi
    //        double cicleStart = angle + Math.PI / 4;

    //        for (double i = cicleStart; i <= cicleStart + Math.PI / 2; i += stepsize) //always 1/4 of a cicle
    //        {
    //            var x = startPoint.X + distanceToEdge.X * Math.Cos(i);
    //            var y = startPoint.Y + distanceToEdge.X * Math.Sin(i);
    //            points.Add(new StylusPoint(x, y));
    //        }
    //        points.Add(new StylusPoint(startPoint.X, startPoint.Y));
    //        points.Add(points.First());
    //    }
    //    else if (LineShapeSelected)
    //    {
    //        var thickness = distanceToEdge.Y;
    //        distanceToEdge.Y = startPoint.Y;
    //        distanceToEdge.Y *= CalculateInkCanvasGridSize();

    //        Point<double> point1 = TranslatePoint(distanceToEdge, startPoint);
    //        Point<double> point2 = TranslatePoint(directionPoint, startPoint);
    //        // Find angle of line
    //        double dot = point1.X * point2.X + point1.Y * point2.Y;    // dot product
    //        double det = point1.X * point2.Y - point1.Y * point2.X;    // determinant
    //        double angle = Math.Atan2(det, dot);             // -pi < 0 < pi
    //        double cicleStart = angle + Math.PI / 2;

    //        var x = startPoint.X + distanceToEdge.X * Math.Cos(cicleStart);
    //        var y = startPoint.Y + distanceToEdge.X * Math.Sin(cicleStart);

    //        // Shorten line for thinkness for later
    //        var percentage = (thickness / distanceToEdge.X) / 2;
    //        point1 = ShortenPoint(startPoint.X, startPoint.Y, x, y, percentage);
    //        point2 = ShortenPoint(startPoint.X, startPoint.Y, x, y, 1 - percentage);

    //        points.Add(new StylusPoint(point1.X, point1.Y));
    //        points.Add(new StylusPoint(point2.X, point2.Y));
    //        points.Add(points.First());
    //    }
    //    else
    //    {
    //        // Circle only uses x size
    //        double stepsize = 0.05;
    //        for (double i = 0; i <= 2 * Math.PI; i += stepsize)
    //        {
    //            var x = startPoint.X + distanceToEdge.X * Math.Cos(i);
    //            var y = startPoint.Y + distanceToEdge.X * Math.Sin(i);
    //            points.Add(new StylusPoint(x, y));
    //        }
    //        points.Add(new StylusPoint(startPoint.X + distanceToEdge.X, startPoint.Y));
    //    }

    //    return points;
    //}

    //private Point<double> ShortenPoint(double X1, double Y1, double X2, double Y2, double percentage)
    //{
    //    //(Ax+ t(Bx−Ax),Ay + t(By−Ay))
    //    return new Point<double>((X1 + percentage * (X2 - X1)), (Y1 + percentage * (Y2 - Y1)));
    //}

    //private Point<double> TranslatePoint(Point<double> point, Point<double> center)
    //{
    //    return new Point<double>(point.X - center.X, point.Y - center.Y);
    //}

    //private DrawingShapeType GetDrawingShapeType()
    //{
    //    return RectangleShapeSelected ? DrawingShapeType.Rectangle : CircleShapeSelected ? DrawingShapeType.Circle : ConeShapeSelected ? DrawingShapeType.Cone : DrawingShapeType.Line;
    //}

    //private void PreventErasingShape(StrokeCollection removed, StrokeCollection added)
    //{
    //    foreach (var removedStroke in removed)
    //    {
    //        var shape = Shapes.SingleOrDefault(s => s.Stroke == removedStroke);
    //        if (shape != null)
    //        {
    //            Strokes.Add(removed);
    //            Strokes.Remove(added);
    //        }
    //    }
    //}

}


/* TODO:
 * - Circle
 * - Cone
 * - Combine canvasSize and GridSize
 */
