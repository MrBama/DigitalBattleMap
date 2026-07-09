using DigitalBattleMap.DataClasses;
using DigitalBattleMap.DrawingShapes;
using DigitalBattleMap.Imaging;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Color = System.Windows.Media.Color;

namespace DigitalBattleMap.ViewModels;

public class DrawingControllerViewModel : ControllerViewModelBase
{
    private DrawingButton _selectedDrawingButton = DrawingButton.Black;
    private ITokenLinker _tokenLinker;
    private bool _showSelectedShapeIndicator = true;
    private List<DrawingShape> _rotationMarkers = new();
    private List<DrawingShape> _shapeAreaDrawings = new();
    private bool _isShapeAreaShown = false;

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
        BlackButtonBitmapSource = BitmapTools.CreateColorButton(DrawingButton.Black.ToColor(), true).ToDrawingBitmap().ToBitmapImage();
        RedButtonBitmapSource = BitmapTools.CreateColorButton(DrawingButton.Red.ToColor(), false).ToDrawingBitmap().ToBitmapImage();
        GreenButtonBitmapSource = BitmapTools.CreateColorButton(DrawingButton.Green.ToColor(), false).ToDrawingBitmap().ToBitmapImage();
        BlueButtonBitmapSource = BitmapTools.CreateColorButton(DrawingButton.Blue.ToColor(), false).ToDrawingBitmap().ToBitmapImage();
        EraserButtonBitmapSource = BitmapTools.CreateEraserButton(false).ToDrawingBitmap().ToBitmapImage();
        ShapeCollection = new();
        ShapeCollection.OnRenderShapes += OnShapeRenderChanged;
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
        SelectedDrawingButtonChangedCommand = new RelayCommand(p => SelectedDrawingButtonChanged((DrawingButton)p));
        ClearDrawingCommand = new RelayCommand(p => ClearDrawings());
        CancelDrawShapeCommand = new RelayCommand(p => CancelDrawShape());
        RemoveShapeCommand = new RelayCommand(p => RemoveShape());
        DrawRectangleCommand = new RelayCommand(p => DrawShape(DrawingShapeType.Rectangle));
        DrawCircleCommand = new RelayCommand(p => DrawShape(DrawingShapeType.Circle));
        DrawConeCommand = new RelayCommand(p => DrawShape(DrawingShapeType.Cone));
        DrawLineCommand = new RelayCommand(p => DrawShape(DrawingShapeType.Line));
        UndoCommand = new RelayCommand(p => Undo());
        RedoCommand = new RelayCommand(p => Redo());
    }

    public event EventHandler OnDrawingShapesUpdated;
    public event EventHandler<ZoomAndEnhanceEventArgs> OnZoomAndEnhance;

    public BitmapSource BlackButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource RedButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource GreenButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource BlueButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource EraserButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource UndoBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.UndoIcon.png")).ToBitmapImage(); }
    public BitmapSource RedoBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.RedoIcon.png")).ToBitmapImage(); }
    public DrawingShape SelectedShape { get => Get<DrawingShape>(); set => Set(value, SelectedShapeChanged); }
    public DrawingShapeCollection ShapeCollection { get => Get<DrawingShapeCollection>(); set => Set(value); }
    public bool IsDrawShapeActive { get => Get<bool>(); set => Set(value); }
    public MouseCanvasViewModel MouseCanvas { get => Get<MouseCanvasViewModel>(); private set => Set(value); }
    public CommandHistory<DrawingShapeCommand> DrawingHistory { get; set; } = new(30);
    public ICommand SelectedDrawingButtonChangedCommand { get; set; }
    public ICommand ClearDrawingCommand { get; set; }
    public ICommand CancelDrawShapeCommand { get; set; }
    public ICommand RemoveShapeCommand { get; set; }
    public ICommand DrawRectangleCommand { get; set; }
    public ICommand DrawCircleCommand { get; set; }
    public ICommand DrawConeCommand { get; set; }
    public ICommand DrawLineCommand { get; set; }
    public ICommand UndoCommand { get; set; }
    public ICommand RedoCommand { get; set; }

    public DrawingShape ActiveShape
    {
        get => Get<DrawingShape>();
        set
        {
            var oldValue = Get<DrawingShape>();
            if (oldValue != null)
            {
                oldValue.PropertyChanged -= ActiveShapePropertyChanged;
                oldValue.OnRenderChanged -= OnShapeRenderChanged;
            }

            Set(value);

            if (value != null)
            {
                value.PropertyChanged += ActiveShapePropertyChanged;
                value.OnRenderChanged += OnShapeRenderChanged;
            }
        }
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

        ShapeCollection.Transform(matrix);
        NotifyDrawingShapesUpdated();
    }

    public override void AddToSaveFile(SaveFile saveFile)
    {
        foreach ((var shape, var index) in ShapeCollection.GetDrawingShapes().WithIndex())
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

    public override void KeyDown(KeyEventArgs keyEventArgs)
    {
        base.KeyDown(keyEventArgs);

        if (keyEventArgs.Key == Key.LeftShift)
        {
            EnableShapeRotation();
        }

        if(keyEventArgs.Key == Key.LeftCtrl)
        {
            ShowShapeArea();
        }
    }   

    public override void KeyUp(KeyEventArgs keyEventArgs)
    {
        base.KeyUp(keyEventArgs);
        if (keyEventArgs.Key == Key.LeftShift)
        {
            DisableShapeRotation();
        }

        if(keyEventArgs.Key == Key.LeftCtrl)
        {
            HideShapeArea();
        }
    }

    private void OnCanvasSizeChanged(object? sender, CanvasSizeChangedEventArgs eventArgs)
    {
        if (eventArgs.OldSize != null && !eventArgs.OldSize.Equals(eventArgs.NewSize))
        {
            var zoomFactor = eventArgs.NewSize.Width / eventArgs.OldSize.Width;

            foreach (var shape in ShapeCollection.GetDrawingShapes())
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

    public IImage GetDrawingBitmap()
    {
        var bitmap = BitmapTools.CreateEmptyBitmap();
        BitmapTools.DrawShapes(bitmap, ShapeCollection.GetDrawingShapes().ToList(), _mapSize.GetCanvasSize());
        return bitmap;
    }

    public bool GetOverviewBitmap(double zoomFactor, out OverviewBitmap overviewBitmap)
    {
        overviewBitmap = new OverviewBitmap();
        var shapes = ShapeCollection.GetDrawingShapes().ToList();
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

    public void ClearDrawings()
    {
        foreach (var shape in ShapeCollection.GetDrawingShapes())
        {
            shape.LinkableObject.Dispose();
        }

        ShapeCollection.Clear();
        DrawingHistory.Clear();
        ActiveShape = CreateStrokeDrawingShape();
        ResetDrawingButton();
        IsDrawShapeActive = false;
        NotifyDrawingShapesUpdated();
    }

    protected override void CreateBitmap()
    {
        NotifyDrawingShapesUpdated();
    }

    private void SelectedDrawingButtonChanged(DrawingButton drawingButton)
    {
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

        ActiveShape.Color = newDrawingButton.ToColor();
    }

    private void ResetDrawingButton()
    {
        SetDrawingButtonSelection(_selectedDrawingButton, false);
        SetDrawingButtonSelection(DrawingButton.Black, true);
        ActiveShape.Color = DrawingButton.Black.ToColor();
        _selectedDrawingButton = DrawingButton.Black;
    }

    private void SelectEraser()
    {
        IsDrawShapeActive = false;

        var eraserDrawingShape = new EraserDrawingShape(ShapeCollection, _mapSize)
        {
            PenSize = ActiveShape.PenSize
        };

        eraserDrawingShape.OnErased += OnErased;
        ActiveShape = eraserDrawingShape;
    }

    private void OnErased(object? sender, DrawingShapeErasedEventArgs e)
    {
        DrawingHistory.Enqueue(new DrawingShapeCommand(null, DrawingShapeCommandAction.Erase) { EraseData = e });
        NotifyDrawingShapesUpdated();
    }

    private void SetDrawingButtonSelection(DrawingButton drawingButton, bool isSelected)
    {
        var color = drawingButton.ToColor();

        switch (drawingButton)
        {
            case DrawingButton.Black:
                BlackButtonBitmapSource = BitmapTools.CreateColorButton(color, isSelected).ToDrawingBitmap().ToBitmapImage();
                break;
            case DrawingButton.Red:
                RedButtonBitmapSource = BitmapTools.CreateColorButton(color, isSelected).ToDrawingBitmap().ToBitmapImage();
                break;
            case DrawingButton.Green:
                GreenButtonBitmapSource = BitmapTools.CreateColorButton(color, isSelected).ToDrawingBitmap().ToBitmapImage();
                break;
            case DrawingButton.Blue:
                BlueButtonBitmapSource = BitmapTools.CreateColorButton(color, isSelected).ToDrawingBitmap().ToBitmapImage();
                break;
            case DrawingButton.Eraser:
                EraserButtonBitmapSource = BitmapTools.CreateEraserButton(isSelected).ToDrawingBitmap().ToBitmapImage();
                break;
        }
    }

    private void ApplyActiveShape()
    {
        ShapeCollection.Add(ActiveShape);
        if(ActiveShape.ShowInShapesOverview)
        {
            _showSelectedShapeIndicator = false;
            SelectedShape = ActiveShape;
            _showSelectedShapeIndicator = true;
        }
        DrawingHistory.Enqueue(new DrawingShapeCommand(ActiveShape, DrawingShapeCommandAction.Add));

        ActiveShape = CreateStrokeDrawingShape();
        IsDrawShapeActive = false;
        NotifyDrawingShapesUpdated();
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
        SelectedShape?.RightButtonDown(e);
    }

    private void RightButtonUp(object? sender, MouseButtonDataEventArgs e)
    {
        SelectedShape?.RightButtonUp(e);
    }

    private void MouseMove(object? sender, MouseMoveDataEventArgs e)
    {
        if (e.RightButtonDown)
        {
            SelectedShape?.MouseMove(e);
        }
        else
        {
            ActiveShape.MouseMove(e);
        }      
    }

    private DrawingShape CreateStrokeDrawingShape()
    {
        var strokeDrawingShapes = ShapeCollection.GetDrawingShapes().OfType<StrokeDrawingShape>();

        return new StrokeDrawingShape(ApplyActiveShape, _tokenLinker, _mapSize)
        {
            Color = ActiveShape.Color,
            PenSize = ActiveShape.PenSize,
            SnapToGrid = strokeDrawingShapes.Any() && strokeDrawingShapes.Last().SnapToGrid
        };
    }

    private void NotifyDrawingShapesUpdated()
    {
        if (_pauseBitmapCreation)
            return;

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
        if (SelectedShape != null && _showSelectedShapeIndicator)
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

    private void RemoveShape()
    {
        if (ActiveShape == SelectedShape)
        {
            ActiveShape = CreateStrokeDrawingShape();
        }

        DrawingHistory.Enqueue(new DrawingShapeCommand(SelectedShape, DrawingShapeCommandAction.Remove) { RemovedAtIndex = ShapeCollection.IndexOf(SelectedShape) });
        ShapeCollection.Remove(SelectedShape);

        NotifyDrawingShapesUpdated();
    }

    private void Undo()
    {
        if(DrawingHistory.TryDequeuePreviousCommand(out var command))
        {
            UndoDrawingCommand(command);
        }
    }

    private void UndoDrawingCommand(DrawingShapeCommand command)
    {
        switch (command.Action)
        {
            case DrawingShapeCommandAction.Add:
                ShapeCollection.Remove(command.DrawingShape);
                break;
            case DrawingShapeCommandAction.Remove:
                ShapeCollection.Insert(command.RemovedAtIndex, command.DrawingShape);
                break;
            case DrawingShapeCommandAction.Edit:
                command.DrawingShape.Color = command.OldInfo.Color;
                command.DrawingShape.PenSize = command.OldInfo.Size;
                command.DrawingShape.Points = new ObservableCollection<Point<double>>(command.OldInfo.Points);
                command.DrawingShape.CentersOfRotation = new List<Point<double>>(command.OldInfo.CentersOfRotation);
                break;
            case DrawingShapeCommandAction.Erase:
                foreach (var eraseCommand in command.EraseData.EraseCommands)
                {
                    UndoDrawingCommand(eraseCommand);
                }
                break;
            default:
                throw new NotImplementedException();
        }

        NotifyDrawingShapesUpdated();
    }

    private void Redo()
    {
        if (DrawingHistory.TryDequeueNextCommand(out var command))
        {
            RedoDrawingCommand(command);
        }
    }

    private void RedoDrawingCommand(DrawingShapeCommand command)
    {
        switch (command.Action)
        {
            case DrawingShapeCommandAction.Add:
                ShapeCollection.Add(command.DrawingShape);
                break;
            case DrawingShapeCommandAction.Remove:
                ShapeCollection.Remove(command.DrawingShape);
                break;
            case DrawingShapeCommandAction.Edit:
                command.DrawingShape.Color = command.NewInfo.Color;
                command.DrawingShape.PenSize = command.NewInfo.Size;
                command.DrawingShape.Points = new ObservableCollection<Point<double>>(command.NewInfo.Points);
                command.DrawingShape.CentersOfRotation = new List<Point<double>>(command.NewInfo.CentersOfRotation);
                break;
            case DrawingShapeCommandAction.Erase:
                foreach (var eraseCommand in command.EraseData.EraseCommands)
                {
                    RedoDrawingCommand(eraseCommand);
                }
                break;
            default:
                throw new NotImplementedException();
        }

        NotifyDrawingShapesUpdated();
    }

    private void ActiveShapePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DrawingShape.Cursor))
        {
            MouseCanvas.Cursor = ActiveShape.Cursor;
        }
    }

    private void OnShapeRenderChanged(object? sender, DrawingShapeEditedEventArgs e)
    {
        var editCommand = new DrawingShapeCommand(e.DrawingShape, DrawingShapeCommandAction.Edit);
        editCommand.OldInfo = e.OldInfo;
        editCommand.NewInfo = e.NewInfo;
        DrawingHistory.Enqueue(editCommand);
        NotifyDrawingShapesUpdated();
    }

    private void FixRatioRectangleAreaSelected(object? sender, RectangleF rectangle)
    {
        OnZoomAndEnhance?.Invoke(this, new ZoomAndEnhanceEventArgs() { rectangle = rectangle });
    }

    private void EnableShapeRotation()
    {
        if (SelectedShape != null && SelectedShape.Mode != DrawingShapeMode.Rotate)
        {
            SelectedShape.Mode = DrawingShapeMode.Rotate;
            foreach (var centerOfRotation in SelectedShape.CentersOfRotation)
            {
                var shape = CreateStrokeDrawingShape();
                shape.Points.Add(centerOfRotation);
                shape.Color = Colors.Orange;
                shape.PenSize = Math.Max(SelectedShape.PenSize, 20);
                ShapeCollection.Add(shape);
                _rotationMarkers.Add(shape);
            }
        }
    }

    private void DisableShapeRotation()
    {
        if (_rotationMarkers.Count > 0)
        {
            foreach (var shape in _rotationMarkers)
            {
                ShapeCollection.Remove(shape);
            }
            _rotationMarkers.Clear();
        }

        foreach (var shape in ShapeCollection.GetDrawingShapes())
        {
            shape.Mode = DrawingShapeMode.Move;
        }
    }

    private void ShowShapeArea()
    {
        if (SelectedShape != null && !_isShapeAreaShown)
        {
            _isShapeAreaShown = true;

            var points = new List<Point<double>>();
            var gridOffset = Mathematics.CalculateCanvasGridOffset(_mapSize);

            // Normalize grid coordinates by removing the grid offset
            foreach (var point in SelectedShape.Points)
            {
                points.Add(new Point<double>(point.X - gridOffset.X, point.Y - gridOffset.Y));
            }

            // Since shapes are closed the first and last point will be the same
            points.RemoveAt(0);

            // Create a drawing shape for each cell that is atleast 50% covered by the selected shape
            var cells = Mathematics.CalculateCoveredGridCells(points, _mapSize.CanvasGridSize, 0.49);
            foreach (var cell in cells)
            {
                // The coordinates are the top left corner of a grid cell. 
                // Add a point for each corner of the cell and add grid offset
                var shape = CreateStrokeDrawingShape();
                shape.Points.Add(new Point<double>(cell.X * _mapSize.CanvasGridSize + gridOffset.X, cell.Y * _mapSize.CanvasGridSize + gridOffset.Y));
                shape.Points.Add(new Point<double>(cell.X * _mapSize.CanvasGridSize + gridOffset.X + (_mapSize.CanvasGridSize), cell.Y * _mapSize.CanvasGridSize + gridOffset.Y));
                shape.Points.Add(new Point<double>(cell.X * _mapSize.CanvasGridSize + gridOffset.X + (_mapSize.CanvasGridSize), cell.Y * _mapSize.CanvasGridSize + gridOffset.Y + (_mapSize.CanvasGridSize)));
                shape.Points.Add(new Point<double>(cell.X * _mapSize.CanvasGridSize + gridOffset.X, cell.Y * _mapSize.CanvasGridSize + gridOffset.Y + (_mapSize.CanvasGridSize)));
                shape.Points.Add(new Point<double>(cell.X * _mapSize.CanvasGridSize + gridOffset.X, cell.Y * _mapSize.CanvasGridSize + gridOffset.Y));
                shape.Color = Colors.LimeGreen;
                shape.PenSize = 5;
                ShapeCollection.Add(shape);
                _shapeAreaDrawings.Add(shape);
            }

            NotifyDrawingShapesUpdated();
        }
    }

    private void HideShapeArea()
    {
        if(_isShapeAreaShown)
        {
            foreach (var shape in _shapeAreaDrawings)
            {
                ShapeCollection.Remove(shape);
            }
            _shapeAreaDrawings.Clear();
            _isShapeAreaShown = false;
            NotifyDrawingShapesUpdated();
        }
    }
}
