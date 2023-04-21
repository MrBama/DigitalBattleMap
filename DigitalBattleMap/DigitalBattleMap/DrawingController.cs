using DigitalBattleMap.DataClasses;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DigitalBattleMap;

public class DrawingController
{
    private DrawingButton _selectedDrawingButton = DrawingButton.Black;
    private double _penSize = 5;
    private Size<double> _canvasSize;
    private Size<int> _bitmapSize;
    private InkCanvasEditingMode _editingMode = InkCanvasEditingMode.Ink;
    private StylusShape _eraserShape;
    private DrawingAttributes _inkCanvasDrawingAttributes = new DrawingAttributes();
    private Bitmap _blackButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 0), true);
    private Bitmap _redButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 255, 0, 0), false);
    private Bitmap _greenButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 255, 0), false);
    private Bitmap _blueButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 255), false);
    private Bitmap _eraserButtonBitmap = BitmapTools.CreateEraserButton(false);
    private Bitmap _inkCanvasBitmap = BitmapTools.CreateEmptyBitmap();
    private int _gridSize;
    private bool _isShapeEditorActive;
    private Stroke _shapeStroke;
    private DrawingShape _selectedShape;

    public DrawingController(int gridSize)
    {
        _gridSize = gridSize;
        _bitmapSize = BitmapTools.GetBitmapSize();

        _inkCanvasDrawingAttributes.Width = PenSize;
        _inkCanvasDrawingAttributes.Height = PenSize;
        _inkCanvasDrawingAttributes.IgnorePressure = true;
        _eraserShape = new EllipseStylusShape(PenSize, PenSize);
        Strokes = new StrokeCollection();
        Strokes.StrokesChanged += OnStrokesChanged;
    }

    public event EventHandler OnDrawingButtonsUpdated;
    public event EventHandler OnDrawingShapeButtonsUpdated;
    public event EventHandler OnDrawingStrokesUpdated;

    public double PenSize
    {
        get => _penSize;
        set
        {
            _penSize = Math.Clamp(value, 1, 100);
            PenSizeChanged();
        }
    }

    public DrawingShape SelectedShape
    {
        get => _selectedShape;
        set
        {
            if (value != _selectedShape)
            {
                _selectedShape = value;
                ShowShapeSelection();
                NotifyDrawingShapeButtonsUpdated();
            }
        }
    }

    public StrokeCollection Strokes { get; set; }
    public bool IsSnapToGridEnabled { get; set; }
    public int ShapeRadius { get; set; } = 10;
    public bool SquareShapeSelected { get; set; } = true;
    public bool CircleShapeSelected { get; set; }
    public ObservableCollection<DrawingShape> Shapes { get; set; } = new ObservableCollection<DrawingShape>();

    public void SetCanvasSize(Size<double> canvasSize)
    {
        _canvasSize = canvasSize;
    }

    public void SelectedDrawingButtonChanged(string button)
    {
        var drawingButton = Enum.Parse<DrawingButton>(button);
        ChangeDrawingButton(drawingButton);
        NotifyDrawingButtonsUpdated();
    }

    public void MoveDrawings(ArrowDirection direction)
    {
        var matrix = new System.Windows.Media.Matrix();
        double gridSize = _gridSize;
        var distanceX = gridSize.Map(0, _bitmapSize.Width, 0, _canvasSize.Width);
        var distanceY = gridSize.Map(0, _bitmapSize.Height, 0, _canvasSize.Height);

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

    public InkCanvasEditingMode GetEditingMode()
    {
        return _editingMode;
    }

    public StylusShape GetEraserShape()
    {
        return _eraserShape;
    }

    public DrawingAttributes GetInkCanvasDrawingAttributes()
    {
        return _inkCanvasDrawingAttributes;
    }

    public Bitmap GetBlackButtonBitmap()
    {
        return _blackButtonBitmap;
    }

    public Bitmap GetRedButtonBitmap()
    {
        return _redButtonBitmap;
    }

    public Bitmap GetGreenButtonBitmap()
    {
        return _greenButtonBitmap;
    }

    public Bitmap GetBlueButtonBitmap()
    {
        return _blueButtonBitmap;
    }

    public Bitmap GetEraserButtonBitmap()
    {
        return _eraserButtonBitmap;
    }

    public Bitmap GetInkCanvasBitmap()
    {
        return _inkCanvasBitmap;
    }

    public Visibility GetDrawShapeButtonVisibility()
    {
        return _isShapeEditorActive ? Visibility.Hidden : Visibility.Visible;
    }

    public Visibility GetCancelShapeButtonVisibility()
    {
        return _isShapeEditorActive ? Visibility.Visible : Visibility.Hidden;
    }

    public Visibility GetApplyShapeButtonVisibility()
    {
        return _isShapeEditorActive ? Visibility.Visible : Visibility.Hidden;
    }

    public bool IsShapeSelected()
    {
        return SelectedShape != null;
    }

    public bool IsShapeDrawn()
    {
        return _shapeStroke != null;
    }

    public void AddToSaveFile(SaveFile saveFile)
    {
        saveFile.Strokes = Strokes;

        foreach (var shape in Shapes)
        {
            var index = Strokes.IndexOf(shape.Stroke);
            saveFile.DrawingShapes.Add(new DrawingShapeSave(shape, index));
        }
    }

    public void OpenSaveFile(SaveFile saveFile)
    {
        ClearDrawings();

        Strokes = saveFile.Strokes;
        foreach (var saveShape in saveFile.DrawingShapes)
        {
            var shape = new DrawingShape
            {
                DrawingShapeType = saveShape.DrawingShapeType,
                Radius = saveShape.Radius,
                DrawingButton = saveShape.DrawingButton,
                Stroke = Strokes[saveShape.StrokeIndex]
            };
            Shapes.Add(shape);
        }

        Strokes.StrokesChanged += OnStrokesChanged;
        NotifyDrawingStrokesUpdated();
    }

    public void UpdateGridSize(int gridSize)
    {
        _gridSize = gridSize;
    }

    public void ClearDrawings()
    {
        _isShapeEditorActive = false;
        Shapes.Clear();
        Strokes.Clear();
        NotifyDrawingShapeButtonsUpdated();
        NotifyDrawingStrokesUpdated();
    }

    public void DrawShape()
    {
        _isShapeEditorActive = true;
        NotifyDrawingShapeButtonsUpdated();
    }

    public void CancelShape()
    {
        _isShapeEditorActive = false;
        Strokes.Remove(_shapeStroke);
        _shapeStroke = null;
        NotifyDrawingShapeButtonsUpdated();
        NotifyDrawingStrokesUpdated();
    }

    public void ApplyShape()
    {
        var shape = new DrawingShape 
        { 
            DrawingShapeType = GetDrawingShapeType(), 
            Radius = ShapeRadius, 
            Stroke = _shapeStroke, 
            DrawingButton = _selectedDrawingButton 
        };

        Shapes.Add(shape);
        _isShapeEditorActive = false;
        _shapeStroke = null;
        NotifyDrawingShapeButtonsUpdated();
    }

    public void EditShape()
    {
        _shapeStroke = SelectedShape.Stroke;
        PenSize = SelectedShape.Stroke.DrawingAttributes.Width;
        SquareShapeSelected = SelectedShape.DrawingShapeType == DrawingShapeType.Square;
        CircleShapeSelected = SelectedShape.DrawingShapeType == DrawingShapeType.Circle;
        ShapeRadius = SelectedShape.Radius;
        ChangeDrawingButton(SelectedShape.DrawingButton);

        Shapes.Remove(SelectedShape);
        _isShapeEditorActive = true;
        NotifyDrawingButtonsUpdated();
        NotifyDrawingShapeButtonsUpdated();
    }

    public void RemoveShape()
    {
        var stroke = SelectedShape.Stroke;
        Shapes.Remove(SelectedShape);
        Strokes.Remove(stroke);
        _isShapeEditorActive = false;
        NotifyDrawingShapeButtonsUpdated();
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
                _blackButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 0), false);
                break;
            case DrawingButton.Red:
                _redButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 255, 0, 0), false);
                break;
            case DrawingButton.Green:
                _greenButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 255, 0), false);
                break;
            case DrawingButton.Blue:
                _blueButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 255), false);
                break;
            case DrawingButton.Eraser:
                _editingMode = InkCanvasEditingMode.Ink;
                _eraserButtonBitmap = BitmapTools.CreateEraserButton(false);
                break;
        }

        _selectedDrawingButton = drawingButton;

        switch (_selectedDrawingButton)
        {
            case DrawingButton.Black:
                _inkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 0, 0);
                _blackButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 0), true);
                break;
            case DrawingButton.Red:
                _inkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(255, 0, 0);
                _redButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 255, 0, 0), true);
                break;
            case DrawingButton.Green:
                _inkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 255, 0);
                _greenButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 255, 0), true);
                break;
            case DrawingButton.Blue:
                _inkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 0, 255);
                _blueButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 255), true);
                break;
            case DrawingButton.Eraser:
                _editingMode = InkCanvasEditingMode.EraseByPoint;
                _eraserShape = new EllipseStylusShape(PenSize, PenSize);
                _eraserButtonBitmap = BitmapTools.CreateEraserButton(true);
                break;
        }
    }

    private void NotifyDrawingButtonsUpdated()
    {
        OnDrawingButtonsUpdated?.Invoke(this, new EventArgs());
    }

    private void NotifyDrawingShapeButtonsUpdated()
    {
        OnDrawingShapeButtonsUpdated?.Invoke(this, new EventArgs());
    }

    private void NotifyDrawingStrokesUpdated()
    {
        OnDrawingStrokesUpdated?.Invoke(this, new EventArgs());
    }

    private void PenSizeChanged()
    {
        _inkCanvasDrawingAttributes.Width = PenSize;
        _inkCanvasDrawingAttributes.Height = PenSize;

        if (_editingMode == InkCanvasEditingMode.EraseByPoint)
        {
            _eraserShape = new EllipseStylusShape(PenSize, PenSize);
        }

        NotifyDrawingButtonsUpdated();
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

        if(!_isShapeEditorActive)
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
        return new Point<double>(gridOffset.X.Map(0, _bitmapSize.Width, 0, _canvasSize.Width), gridOffset.Y.Map(0, _bitmapSize.Height, 0, _canvasSize.Height));
    }

    private double CalculateInkCanvasGridSize()
    {
        double inkCanvasGridSize = _gridSize;
        return inkCanvasGridSize.Map(0, _bitmapSize.Width, 0, _canvasSize.Width);
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
        var distanceToEdge = Math.Round((double)ShapeRadius / Constants.FeetPerGridCell);
        distanceToEdge *= inkCanvasGridSize;

        Strokes.Remove(_shapeStroke);
        addedStroke.StylusPoints = CreateShape(startPoint, distanceToEdge);
        _shapeStroke = addedStroke;

        NotifyDrawingShapeButtonsUpdated();
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

    private void ShowShapeSelection()
    {
        if(SelectedShape != null)
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
}
