using DigitalBattleMap.DataClasses;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace DigitalBattleMap.DrawingShapes;

public class DrawingShapeInfo
{
    public DrawingShapeInfo()
    {
    }

    public DrawingShapeInfo(DrawingShape drawingShape)
    {
        Color = drawingShape.Color;
        Size = drawingShape.PenSize;
        Points = drawingShape.Points.ToList();
        CentersOfRotation = drawingShape.CentersOfRotation.ToList();
    }

    public Color Color { get; set; } = Colors.Black;
    public double Size { get; set; }
    public List<Point<double>> Points { get; set; } = new();
    public List<Point<double>> CentersOfRotation { get; set; } = new();
}
