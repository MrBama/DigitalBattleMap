using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace DigitalBattleMap;

public class DrawingController
{
    private DrawingButtons _selectedDrawingButton = DrawingButtons.Black;
    private double _penSize = 5;
    private Size<double> _canvasSize;
    private Size<int> _bitmapSize;
    private InkCanvasEditingMode _editingMode = InkCanvasEditingMode.Ink;
    private StylusShape _eraserShape;
    private DrawingAttributes _iInkCanvasDrawingAttributes = new DrawingAttributes();
    private Bitmap _blackButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 0), true);
    private Bitmap _redButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 255, 0, 0), false);
    private Bitmap _greenButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 255, 0), false);
    private Bitmap _blueButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 255), false);
    private Bitmap _eraserButtonBitmap = BitmapTools.CreateEraserButton(false);
    private Bitmap _inkCanvasBitmap = BitmapTools.CreateEmptyBitmap();
    private int _gridSize;
    private bool _isShapeEditorActive;
    private Stroke _shapeStroke;

    public DrawingController(int gridSize)
    {
        _gridSize = gridSize;
        _bitmapSize = BitmapTools.GetBitmapSize();

        _iInkCanvasDrawingAttributes.Width = PenSize;
        _iInkCanvasDrawingAttributes.Height = PenSize;
        _iInkCanvasDrawingAttributes.IgnorePressure = true;
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

    public StrokeCollection Strokes { get; set; }
    public bool IsSnapToGridEnabled { get; set; }
    public int ShapeSize { get; set; } = 10;
    public bool SquareShapeSelected { get; set; } = true;
    public bool CircleShapeSelected { get; set; }

    public void SetCanvasSize(Size<double> canvasSize)
    {
        _canvasSize = canvasSize;
    }

    public void SelectedDrawingButtonChanged(string button)
    {
        var newSelectedButton = Enum.Parse<DrawingButtons>(button);
        if (_selectedDrawingButton == newSelectedButton)
        {
            return;
        }

        switch (_selectedDrawingButton)
        {
            case DrawingButtons.Black:
                _blackButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 0), false);
                break;
            case DrawingButtons.Red:
                _redButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 255, 0, 0), false);
                break;
            case DrawingButtons.Green:
                _greenButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 255, 0), false);
                break;
            case DrawingButtons.Blue:
                _blueButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 255), false);
                break;
            case DrawingButtons.Eraser:
                _editingMode = InkCanvasEditingMode.Ink;
                _eraserButtonBitmap = BitmapTools.CreateEraserButton(false);
                break;
        }

        _selectedDrawingButton = newSelectedButton;

        switch (_selectedDrawingButton)
        {
            case DrawingButtons.Black:
                _iInkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 0, 0);
                _blackButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 0), true);
                break;
            case DrawingButtons.Red:
                _iInkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(255, 0, 0);
                _redButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 255, 0, 0), true);
                break;
            case DrawingButtons.Green:
                _iInkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 255, 0);
                _greenButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 255, 0), true);
                break;
            case DrawingButtons.Blue:
                _iInkCanvasDrawingAttributes.Color = System.Windows.Media.Color.FromRgb(0, 0, 255);
                _blueButtonBitmap = BitmapTools.CreateColorButton(Color.FromArgb(255, 0, 0, 255), true);
                break;
            case DrawingButtons.Eraser:
                _editingMode = InkCanvasEditingMode.EraseByPoint;
                _eraserShape = new EllipseStylusShape(PenSize, PenSize);
                _eraserButtonBitmap = BitmapTools.CreateEraserButton(true);
                break;
        }

        NotifyDrawingButtonsUpdated();
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
        return _iInkCanvasDrawingAttributes;
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

    public void AddToSaveFile(SaveFile saveFile)
    {
        saveFile.Strokes = Strokes;
    }

    public void OpenSaveFile(SaveFile saveFile)
    {
        Strokes = saveFile.Strokes;
        Strokes.StrokesChanged += OnStrokesChanged;
        NotifyDrawingStrokesUpdated();
    }

    public void UpdateGridSize(int gridSize)
    {
        _gridSize = gridSize;
    }

    public void ClearDrawings()
    {
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
        NotifyDrawingShapeButtonsUpdated();
        NotifyDrawingStrokesUpdated();
    }

    public void ApplyShape()
    {
        _isShapeEditorActive = false;
        _shapeStroke = null;
        NotifyDrawingShapeButtonsUpdated();
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
        _iInkCanvasDrawingAttributes.Width = PenSize;
        _iInkCanvasDrawingAttributes.Height = PenSize;

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

            Task.WhenAll(tasks).Wait();
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
        var inkCanvasGridOffset = CalculateInkCanvasGridOffset();
        double inkCanvasGridSize = CalculateInkCanvasGridSize();
        var startPoint = SnapPoint(new Point<double>(addedStroke.StylusPoints.First().X, addedStroke.StylusPoints.First().Y), inkCanvasGridOffset, inkCanvasGridSize);
        var distanceToEdge = Math.Round((double)ShapeSize / Constants.FeetPerGridCell);
        distanceToEdge *= inkCanvasGridSize;

        Strokes.Remove(_shapeStroke);

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

        addedStroke.StylusPoints = points;
        _shapeStroke = addedStroke;
    }
}
