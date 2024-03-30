using System;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawingCanvas;
public class DrawingCanvasViewModel : PropertyHandler
{
    private RadioButton _radioButton = RadioButton.Black;

    public DrawingCanvasViewModel()
    {
        ShapeCollection = new();
        ActiveShape = new StrokeDrawingShape(ApplyActiveShape);
        RadioButtonBlack = true;
        CreateRectangleCommand = new RelayCommand(p => CreateRectangle());
        CreateCircleCommand = new RelayCommand(p => CreateCircle());
        CreateConeCommand = new RelayCommand(p => CreateCone());
        EditShapeCommand = new RelayCommand(p => EditShape());
        ApplyEditShapeCommand = new RelayCommand(p => ApplyEditShape());
        CancelEditShapeCommand = new RelayCommand(p => CancelEditShape());
    }

    public DrawingShape ActiveShape { get => Get<DrawingShape>(); set => Set(value); }
    public DrawingShape SelectedShape { get => Get<DrawingShape>(); set => Set(value); }
    public DrawingShapeCollection ShapeCollection { get => Get<DrawingShapeCollection>(); set => Set(value); }
    public bool RadioButtonBlack { get => Get<bool>(); set => Set(value, () => SetRadioButton(value, RadioButton.Black)); }
    public bool RadioButtonRed { get => Get<bool>(); set => Set(value, () => SetRadioButton(value, RadioButton.Red)); }
    public bool RadioButtonEraser { get => Get<bool>(); set => Set(value, () => SetRadioButton(value, RadioButton.Eraser)); }
    public ICommand CreateRectangleCommand { get; set; }
    public ICommand CreateCircleCommand { get; set; }
    public ICommand CreateConeCommand { get; set; }
    public ICommand EditShapeCommand { get; set; }
    public ICommand ApplyEditShapeCommand { get; set; }
    public ICommand CancelEditShapeCommand { get; set; }

    private void ApplyActiveShape()
    {
        if (!ShapeCollection.Contains(ActiveShape))
        {
            ShapeCollection.Add(ActiveShape);
        }

        ActiveShape = new StrokeDrawingShape(ApplyActiveShape)
        {
            Color = GetColor(),
            Size = ActiveShape.Size
        };
    }

    private void SetRadioButton(bool isChecked, RadioButton radioButton)
    {
        if (isChecked && _radioButton != radioButton)
        {
            RadioButtonChanged(_radioButton, radioButton);
            _radioButton = radioButton;
        }
    }

    private void RadioButtonChanged(RadioButton previousRadioButton, RadioButton newRadioButton)
    {
        if (previousRadioButton == RadioButton.Eraser)
        {
            ActiveShape = new StrokeDrawingShape(ApplyActiveShape)
            {
                Color = GetColor(),
                Size = ActiveShape.Size
            };
        }

        switch (newRadioButton)
        {
            case RadioButton.Black:
                ActiveShape.Color = Brushes.Black;
                break;
            case RadioButton.Red:
                ActiveShape.Color = Brushes.Red;
                break;
            case RadioButton.Eraser:
                ActiveShape = new EraserDrawingShape(ShapeCollection)
                {
                    Size = ActiveShape.Size
                };
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private Brush GetColor()
    {
        if (RadioButtonBlack)
        {
            return Brushes.Black;
        }
        else
        {
            return Brushes.Red;
        }
    }

    private void CreateRectangle()
    {
        ActiveShape = new RectangleDrawingShape(ApplyActiveShape)
        {
            Color = GetColor(),
            Size = ActiveShape.Size
        };
    }

    private void CreateCircle()
    {
        ActiveShape = new CircleDrawingShape(ApplyActiveShape)
        {
            Color = GetColor(),
            Size = ActiveShape.Size
        };
    }

    private void CreateCone()
    {
        ActiveShape = new ConeDrawingShape(ApplyActiveShape)
        {
            Color = GetColor(),
            Size = ActiveShape.Size
        };
    }

    private void EditShape()
    {
        if (SelectedShape != null)
        {
            ActiveShape = SelectedShape;
            ActiveShape.EditShape();
        }
    }

    private void ApplyEditShape()
    {
        ActiveShape.ApplyShape();
    }

    private void CancelEditShape()
    {
        ActiveShape.CancelEditShape();
        ActiveShape = new StrokeDrawingShape(ApplyActiveShape)
        {
            Color = GetColor(),
            Size = ActiveShape.Size
        };
    }

    private enum RadioButton
    {
        Black,
        Red,
        Eraser
    }
}