using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DrawingCanvas;

public class DrawingShapeInfo
{
    public DrawingShapeInfo(DrawingShape drawingShape)
    {
        Color = drawingShape.Color;
        Size = drawingShape.Size;
        Points = drawingShape.Points.ToList();
    }

    public Brush Color { get; set; }
    public int Size { get; set; }
    public List<Point> Points { get; set; }
}
