using DigitalBattleMap.DataClasses;
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
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DigitalBattleMap.ViewModels;

public class DrawingControllerViewModel : ControllerViewModelBase
{
    private DrawingButton _selectedDrawingButton = DrawingButton.Black;
    private bool _isShapeEditorActive;
    private Stroke _shapeStroke;
    private ITokenLinker _tokenLinker;
    private bool _disableSelectionAnimation;
    private DrawingShape _editShape;

    public DrawingControllerViewModel() : base(50)
    {
        Initialize();
    }

    public DrawingControllerViewModel(ITokenLinker tokenLinker, int gridSize) : base(gridSize)
    {
        _tokenLinker = tokenLinker;
        Initialize();
    }

    private void Initialize()
    {
        InkCanvasDrawingAttributes = new DrawingAttributes();
        PenSize = 5;
        InkCanvasDrawingAttributes.Width = PenSize;
        InkCanvasDrawingAttributes.Height = PenSize;
        InkCanvasDrawingAttributes.IgnorePressure = true;
        BlackButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 0), true).ToBitmapImage();
        RedButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 255, 0, 0), false).ToBitmapImage();
        GreenButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 255, 0), false).ToBitmapImage();
        BlueButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 255), false).ToBitmapImage();
        EraserButtonBitmapSource = BitmapTools.CreateEraserButton(false).ToBitmapImage();
        EditingMode = InkCanvasEditingMode.Ink;
        EraserShape = new EllipseStylusShape(PenSize, PenSize);
        ShapeSize = 10;
        SquareShapeSelected = true;
        CancelShapeButtonVisibility = Visibility.Hidden;
        ApplyShapeButtonVisibility = Visibility.Hidden;
        Strokes = new StrokeCollection();
        Strokes.StrokesChanged += OnStrokesChanged;
    }

    protected override void InitializeCommands()
    {
        SelectedDrawingButtonChangedCommand = new RelayCommand(p => SelectedDrawingButtonChanged((string)p));
        ClearDrawingCommand = new RelayCommand(p => ClearDrawings());
        DrawShapeCommand = new RelayCommand(p => DrawShape());
        CancelShapeCommand = new RelayCommand(p => CancelShape());
        ApplyShapeCommand = new RelayCommand(p => ApplyShape());
        EditShapeCommand = new RelayCommand(p => EditShape());
        RemoveShapeCommand = new RelayCommand(p => RemoveShape());
    }

    public event EventHandler OnDrawingStrokesUpdated;

    public BitmapSource BlackButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource RedButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource GreenButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource BlueButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource EraserButtonBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public DrawingAttributes InkCanvasDrawingAttributes { get => Get<DrawingAttributes>(); set => Set(value); }
    public DrawingShape SelectedShape { get => Get<DrawingShape>(); set => Set(value, SelectedShapeChanged); }
    public InkCanvasEditingMode EditingMode { get => Get<InkCanvasEditingMode>(); set => Set(value); }
    public StylusShape EraserShape { get => Get<StylusShape>(); set => Set(value); }
    public Visibility DrawShapeButtonVisibility { get => Get<Visibility>(); set => Set(value); }
    public Visibility CancelShapeButtonVisibility { get => Get<Visibility>(); set => Set(value); }
    public Visibility ApplyShapeButtonVisibility { get => Get<Visibility>(); set => Set(value); }
    public double PenSize { get => Get<double>(); set => Set(Math.Clamp(value, 1, 100), PenSizeChanged); }
    public bool IsSnapToGridEnabled { get => Get<bool>(); set => Set(value); }
    public int ShapeSize { get => Get<int>(); set => Set(value); }
    public bool SquareShapeSelected { get => Get<bool>(); set => Set(value); }
    public bool CircleShapeSelected { get => Get<bool>(); set => Set(value); }
    public StrokeCollection Strokes { get => Get<StrokeCollection>(); set => Set(value); }
    public bool IsShapeSelected { get => SelectedShape != null; }
    public bool IsShapeEditAndRemoveEnabled { get => IsShapeSelected && !_isShapeEditorActive; }
    public bool IsShapeSelectionEnabled { get => !_isShapeEditorActive; }
    public bool IsShapeDrawn { get => ShapeStroke != null; }
    public System.Windows.Media.Brush InkCanvasBackgroundBitmapSource { get => System.Windows.Media.Brushes.Transparent; }
    public ObservableCollection<DrawingShape> Shapes { get; set; } = new ObservableCollection<DrawingShape>();
    public ICommand SelectedDrawingButtonChangedCommand { get; set; }
    public ICommand ClearDrawingCommand { get; set; }
    public ICommand DrawShapeCommand { get; set; }
    public ICommand CancelShapeCommand { get; set; }
    public ICommand ApplyShapeCommand { get; set; }
    public ICommand EditShapeCommand { get; set; }
    public ICommand RemoveShapeCommand { get; set; }

    private Stroke ShapeStroke
    {
        get => _shapeStroke;
        set
        {
            if (value != _shapeStroke)
            {
                _shapeStroke = value;
                NotifyPropertyChange(nameof(IsShapeDrawn));
            }
        }
    }

    public void SelectedDrawingButtonChanged(string button)
    {
        var drawingButton = Enum.Parse<DrawingButton>(button);
        ChangeDrawingButton(drawingButton);
    }

    public override void Move(ArrowDirection direction)
    {
        var matrix = new System.Windows.Media.Matrix();
        double gridSize = _gridSize;
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

        Strokes.Transform(matrix, false);
        NotifyDrawingStrokesUpdated();
    }

    public override void AddToSaveFile(SaveFile saveFile)
    {
        saveFile.Strokes = Strokes;
        saveFile.CanvasSize = _canvasSize;

        foreach ((var shape, var index) in Shapes.WithIndex())
        {
            var strokeIndex = Strokes.IndexOf(shape.Stroke);
            saveFile.DrawingShapes.Add(new DrawingShapeSave(shape, strokeIndex));

            if(shape.IsLinked())
            {
                var objectLink = new ObjectLink
                {
                    LinkableObjectType = typeof(DrawingShape),
                    Index = index,
                    TokenIndentifier = shape.GetLinkIdentifier()
                };
                saveFile.ObjectLinks.Add(objectLink);
            }
        }
    }

    public override void OpenSaveFile(SaveFile saveFile)
    {
        ClearDrawings();

        Strokes = saveFile.Strokes;

        if (!saveFile.CanvasSize.Equals(_canvasSize) && saveFile.CanvasSize.Width != 0)
        {
            var zoomFactor = _canvasSize.Width / saveFile.CanvasSize.Width;
            var matrix = new System.Windows.Media.Matrix();
            matrix.Scale(zoomFactor, zoomFactor);
            Strokes.Transform(matrix, false);
        }

        foreach (var saveShape in saveFile.DrawingShapes)
        {
            var shape = new DrawingShape
            {
                DrawingShapeType = saveShape.DrawingShapeType,
                Size = saveShape.Size,
                DrawingButton = saveShape.DrawingButton,
                Stroke = Strokes[saveShape.StrokeIndex],
                CanvasSize = _canvasSize
            };
            shape.OnPositionChanged += OnShapePositionChanged;
            shape.SetTokenLinker(_tokenLinker);

            Shapes.Add(shape);
        }

        Strokes.StrokesChanged += OnStrokesChanged;
        NotifyDrawingStrokesUpdated();
    }

    public void OpenObjectLinks(List<ObjectLink> objectLinks)
    {
        foreach (var objectLink in objectLinks)
        {
            if (objectLink.LinkableObjectType == typeof(DrawingShape))
            {
                _tokenLinker.LinkToToken(Shapes[objectLink.Index], objectLink.TokenIndentifier);
            }
        }
    }

    public void ClearDrawings()
    {
        ActivateShapeEditor(false);

        foreach (var shape in Shapes)
        {
            shape.Dispose();
        }
        Shapes.Clear();
        Strokes.Clear();
        NotifyDrawingStrokesUpdated();
    }

    public void DrawShape()
    {
        ActivateShapeEditor(true);
    }

    public void CancelShape()
    {
        ActivateShapeEditor(false);

        if (_editShape != null)
        {
            PenSize = _editShape.Stroke.DrawingAttributes.Width;
            SquareShapeSelected = _editShape.DrawingShapeType == DrawingShapeType.Square;
            CircleShapeSelected = _editShape.DrawingShapeType == DrawingShapeType.Circle;
            ShapeSize = _editShape.Size;
            ChangeDrawingButton(_editShape.DrawingButton);

            SelectedShape.DrawingShapeType = _editShape.DrawingShapeType;
            SelectedShape.Size = _editShape.Size;
            SelectedShape.Stroke = _editShape.Stroke;
            SelectedShape.DrawingButton = _editShape.DrawingButton;
            SelectedShape.CanvasSize = _editShape.CanvasSize;
            Strokes.Add(_editShape.Stroke);

            _editShape.Dispose();
            _editShape = null;
        }

        Strokes.Remove(ShapeStroke);
        ShapeStroke = null;
        NotifyDrawingStrokesUpdated();
    }

    public void ApplyShape()
    {
        ActivateShapeEditor(false);

        if (_editShape != null)
        {
            SelectedShape.DrawingShapeType = GetDrawingShapeType();
            SelectedShape.Size = ShapeSize;
            SelectedShape.Stroke = ShapeStroke;
            SelectedShape.DrawingButton = _selectedDrawingButton;

            _editShape.Dispose();
            _editShape = null;
        }
        else
        {
            var shape = new DrawingShape
            {
                DrawingShapeType = GetDrawingShapeType(),
                Size = ShapeSize,
                Stroke = ShapeStroke,
                DrawingButton = _selectedDrawingButton,
                CanvasSize = _canvasSize
            };
            shape.OnPositionChanged += OnShapePositionChanged;
            shape.SetTokenLinker(_tokenLinker);

            _disableSelectionAnimation = true;
            SelectedShape = shape;
            _disableSelectionAnimation = false;
            Shapes.Add(shape);
        }        
        
        ShapeStroke = null;
    }

    public void EditShape()
    {
        _editShape = new DrawingShape(SelectedShape);
        ShapeStroke = SelectedShape.Stroke;

        PenSize = SelectedShape.Stroke.DrawingAttributes.Width;
        SquareShapeSelected = SelectedShape.DrawingShapeType == DrawingShapeType.Square;
        CircleShapeSelected = SelectedShape.DrawingShapeType == DrawingShapeType.Circle;
        ShapeSize = SelectedShape.Size;
        ChangeDrawingButton(SelectedShape.DrawingButton);

        ActivateShapeEditor(true);
    }

    public void RemoveShape()
    {
        var stroke = SelectedShape.Stroke;
        Shapes.Remove(SelectedShape);
        Strokes.Remove(stroke);
        ActivateShapeEditor(false);
    }

    public override void Zoom(double zoomFactor)
    {
        var matrix = new System.Windows.Media.Matrix();
        matrix.Translate(-(_canvasSize.Width / 2), -(_canvasSize.Height / 2));
        matrix.Scale(zoomFactor, zoomFactor);
        matrix.Translate((_canvasSize.Width / 2), (_canvasSize.Height / 2));
        Strokes.Transform(matrix, false);
    }

    private void ActivateShapeEditor(bool activate)
    {
        _isShapeEditorActive = activate;

        if (_isShapeEditorActive)
        {
            DrawShapeButtonVisibility = Visibility.Hidden;
            CancelShapeButtonVisibility = Visibility.Visible;
            ApplyShapeButtonVisibility = Visibility.Visible;
        }
        else
        {
            DrawShapeButtonVisibility = Visibility.Visible;
            CancelShapeButtonVisibility = Visibility.Hidden;
            ApplyShapeButtonVisibility = Visibility.Hidden;
        }

        NotifyPropertyChange(nameof(IsShapeEditAndRemoveEnabled));
        NotifyPropertyChange(nameof(IsShapeSelectionEnabled));
    }

    private void ChangeDrawingButton(DrawingButton drawingButton)
    {
        if (_selectedDrawingButton == drawingButton)
        {
            return;
        }

        switch (_selectedDrawingButton)
        {
            case DrawingButton.Black:
                BlackButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 0), false).ToBitmapImage();
                break;
            case DrawingButton.Red:
                RedButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 255, 0, 0), false).ToBitmapImage();
                break;
            case DrawingButton.Green:
                GreenButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 255, 0), false).ToBitmapImage();
                break;
            case DrawingButton.Blue:
                BlueButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 255), false).ToBitmapImage();
                break;
            case DrawingButton.Eraser:
                EditingMode = InkCanvasEditingMode.Ink;
                EraserButtonBitmapSource = BitmapTools.CreateEraserButton(false).ToBitmapImage();
                break;
        }

        _selectedDrawingButton = drawingButton;

        switch (_selectedDrawingButton)
        {
            case DrawingButton.Black:
                InkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 0, 0);
                BlackButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 0), true).ToBitmapImage();
                break;
            case DrawingButton.Red:
                InkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(255, 0, 0);
                RedButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 255, 0, 0), true).ToBitmapImage();
                break;
            case DrawingButton.Green:
                InkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 255, 0);
                GreenButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 255, 0), true).ToBitmapImage();
                break;
            case DrawingButton.Blue:
                InkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 0, 255);
                BlueButtonBitmapSource = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 255), true).ToBitmapImage();
                break;
            case DrawingButton.Eraser:
                EditingMode = InkCanvasEditingMode.EraseByPoint;
                EraserShape = new EllipseStylusShape(PenSize, PenSize);
                EraserButtonBitmapSource = BitmapTools.CreateEraserButton(true).ToBitmapImage();
                break;
        }
    }

    private void NotifyDrawingStrokesUpdated()
    {
        OnDrawingStrokesUpdated?.Invoke(this, new EventArgs());
    }

    private void PenSizeChanged()
    {
        InkCanvasDrawingAttributes.Width = PenSize;
        InkCanvasDrawingAttributes.Height = PenSize;

        if (EditingMode == InkCanvasEditingMode.EraseByPoint)
        {
            EraserShape = new EllipseStylusShape(PenSize, PenSize);
        }
    }

    private void OnStrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
    {
        if (_isShapeEditorActive)
        {
            if (e.Added.Count > 0)
            {
                CreateShapeStroke(e.Added.First());
            }
        }
        else if (IsSnapToGridEnabled)
        {
            SnapToGrid(e.Added);
        }

        if (!_isShapeEditorActive)
        {
            PreventErasingShape(e.Removed, e.Added);
        }

        NotifyDrawingStrokesUpdated();
    }

    private void SnapToGrid(StrokeCollection strokes)
    {
        var inkCanvasGridOffset = CalculateInkCanvasGridOffset();
        double inkCanvasGridSize = CalculateInkCanvasGridSize();

        foreach (var stroke in strokes)
        {
            var tasks = new List<Task>();
            var points = new StylusPointCollection();
            object lockObject = "";

            foreach (var stylusPoint in stroke.StylusPoints)
            {
                var point = new Point<double>(stylusPoint.X, stylusPoint.Y);
                var task = Task.Run(() =>
                {
                    var snappedPoint = SnapPoint(point, inkCanvasGridOffset, inkCanvasGridSize);

                    lock (lockObject)
                    {
                        points.Add(new StylusPoint(snappedPoint.X, snappedPoint.Y));
                    }
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
            stroke.StylusPoints = points;
        }
    }

    private Point<double> CalculateInkCanvasGridOffset()
    {
        var gridOffset = Point<double>.Create(BitmapTools.CalculateGridOffset(_gridSize));
        return new(gridOffset.X.Map(0, Constants.BitmapSize.Width, 0, _canvasSize.Width), gridOffset.Y.Map(0, Constants.BitmapSize.Height, 0, _canvasSize.Height));
    }

    private double CalculateInkCanvasGridSize()
    {
        double inkCanvasGridSize = _gridSize;
        return inkCanvasGridSize.Map(0, Constants.BitmapSize.Width, 0, _canvasSize.Width);
    }

    private Point<double> SnapPoint(Point<double> point, Point<double> inkCanvasGridOffset, double inkCanvasGridSize)
    {
        var result = new Point<double>(point);
        var x = result.X - inkCanvasGridOffset.X;
        var y = result.Y - inkCanvasGridOffset.Y;
        var leftOverX = x % inkCanvasGridSize;
        var leftOverY = y % inkCanvasGridSize;

        if (leftOverX < inkCanvasGridSize / 2)
        {
            result.X -= leftOverX;
        }
        else
        {
            result.X += (inkCanvasGridSize - leftOverX);
        }

        if (leftOverY < inkCanvasGridSize / 2)
        {
            result.Y -= leftOverY;
        }
        else
        {
            result.Y += (inkCanvasGridSize - leftOverY);
        }

        return result;
    }

    private void CreateShapeStroke(Stroke addedStroke)
    {
        var inkCanvasGridSize = CalculateInkCanvasGridSize();
        var halfGridSize = inkCanvasGridSize / 2;
        var gridOffset = CalculateInkCanvasGridOffset();
        if (gridOffset.X - halfGridSize >= 0)
        {
            gridOffset.X -= halfGridSize;
        }
        if (gridOffset.Y - halfGridSize >= 0)
        {
            gridOffset.Y -= halfGridSize;
        }

        var startPoint = SnapPoint(new Point<double>(addedStroke.StylusPoints.First().X, addedStroke.StylusPoints.First().Y), gridOffset, halfGridSize);
        var distanceToEdge = Math.Round((double)ShapeSize / Constants.FeetPerGridCell); // Get amount of grid cells
        distanceToEdge *= inkCanvasGridSize;
        distanceToEdge /= 2; // divide by 2 to get radius

        Strokes.Remove(ShapeStroke);
        addedStroke.StylusPoints = CreateShape(startPoint, distanceToEdge);
        ShapeStroke = addedStroke;
    }

    private StylusPointCollection CreateShape(Point<double> startPoint, double distanceToEdge)
    {
        var points = new StylusPointCollection();

        if (SquareShapeSelected)
        {
            points.Add(new StylusPoint(startPoint.X - distanceToEdge, startPoint.Y - distanceToEdge));
            points.Add(new StylusPoint(startPoint.X + distanceToEdge, startPoint.Y - distanceToEdge));
            points.Add(new StylusPoint(startPoint.X + distanceToEdge, startPoint.Y + distanceToEdge));
            points.Add(new StylusPoint(startPoint.X - distanceToEdge, startPoint.Y + distanceToEdge));
            points.Add(new StylusPoint(startPoint.X - distanceToEdge, startPoint.Y - distanceToEdge));
        }
        else
        {
            double stepsize = 0.05;
            for (double i = 0; i <= 2 * Math.PI; i += stepsize)
            {
                var x = startPoint.X + distanceToEdge * Math.Cos(i);
                var y = startPoint.Y + distanceToEdge * Math.Sin(i);
                points.Add(new StylusPoint(x, y));
            }
            points.Add(new StylusPoint(startPoint.X + distanceToEdge, startPoint.Y));
        }

        return points;
    }

    private DrawingShapeType GetDrawingShapeType()
    {
        return SquareShapeSelected ? DrawingShapeType.Square : DrawingShapeType.Circle;
    }

    private void PreventErasingShape(StrokeCollection removed, StrokeCollection added)
    {
        foreach (var removedStroke in removed)
        {
            var shape = Shapes.SingleOrDefault(s => s.Stroke == removedStroke);
            if (shape != null)
            {
                Strokes.Add(removed);
                Strokes.Remove(added);
            }
        }
    }

    private void SelectedShapeChanged()
    {
        if (SelectedShape != null)
        {
            ShowShapeSelection();
        }

        NotifyPropertyChange(nameof(IsShapeSelected));
        NotifyPropertyChange(nameof(IsShapeEditAndRemoveEnabled));
    }

    private void ShowShapeSelection()
    {
        if(!_disableSelectionAnimation)
        {
            var color = SelectedShape.Stroke.DrawingAttributes.Color;
            SelectedShape.Stroke.DrawingAttributes.Color = System.Windows.Media.Colors.Transparent;

            Task.Run(() =>
            {
                Thread.Sleep(150);
                Application.Current.Dispatcher.Invoke(() => { SelectedShape.Stroke.DrawingAttributes.Color = color; }, DispatcherPriority.Normal);
            });
        }
    }

    private void OnShapePositionChanged(object? sender, EventArgs e)
    {
        NotifyDrawingStrokesUpdated();
    }
}
