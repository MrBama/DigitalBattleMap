using System.Windows.Ink;
using System.Windows.Media;

namespace DigitalBattleMap.DataClasses;

public class DrawingShape
{
    public DrawingShapeType DrawingShapeType { get; set; }
    public int Radius { get; set; }
    public Stroke Stroke { get; set; }
    public Brush Color { get => GetColor(); }
    public DrawingButton DrawingButton { get; set; }

    private Brush GetColor()
    {
        var brush = System.Windows.Media.Brushes.Transparent;
        if (Stroke != null)
        {
            brush = new SolidColorBrush(Stroke.DrawingAttributes.Color);
        }

        return brush;
    }
}

public class DrawingShapeSave
{
    public DrawingShapeSave()
    {
    }

    public DrawingShapeSave(DrawingShape drawingShape, int strokeIndex)
    {
        DrawingShapeType = drawingShape.DrawingShapeType;
        Radius = drawingShape.Radius;
        DrawingButton = drawingShape.DrawingButton;
        StrokeIndex = strokeIndex;
    }

    public DrawingShapeType DrawingShapeType { get; set; }
    public int Radius { get; set; }
    public DrawingButton DrawingButton { get; set; }
    public int StrokeIndex { get; set; }
}
