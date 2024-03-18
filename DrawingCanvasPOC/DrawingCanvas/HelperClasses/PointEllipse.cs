using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DrawingCanvas;

public class PointEllipse : Shape
{
    private DrawingShape _drawingShape;

    public PointEllipse(DrawingShape drawingShape, Point point)
    {
        _drawingShape = drawingShape;
        Point = point;
        Stroke = drawingShape.Color;
        Fill = drawingShape.Color;
        StrokeThickness = 0;
    }

    public Point Point { get; set; }

    public bool IsPartOfShape(DrawingShape drawingShape)
    {
        return _drawingShape == drawingShape;
    }

    protected override Geometry DefiningGeometry
    {
        get
        {
            // Not sure why the 0.8 is required but otherwise the ellipse is too big
            double ellipseRadius = _drawingShape.Size * 0.8 / 2;
            return new EllipseGeometry(Point, ellipseRadius, ellipseRadius);
        }
    }
}

//public class DrawingShapePath : Shape
//{
//    private DrawingShape _drawingShape;

//    public DrawingShapePath(DrawingShape drawingShape)
//    {
//        _drawingShape = drawingShape;
//        Stroke = drawingShape.Color;
//        StrokeThickness = drawingShape.Size;
//    }

//    public bool IsPartOfShape(DrawingShape drawingShape)
//    {
//        return _drawingShape == drawingShape;
//    }

//    protected override Geometry DefiningGeometry
//    {
//        get
//        {
//            PathFigure myPathFigure = new PathFigure();
//            myPathFigure.StartPoint = _drawingShape.Points.First();

//            PathSegmentCollection myPathSegmentCollection = new PathSegmentCollection();


//            foreach (var point in _drawingShape.Points)
//            {
//                myPathSegmentCollection.Add(new LineSegment(point, true));
//            }

//            myPathFigure.Segments = myPathSegmentCollection;

//            PathFigureCollection myPathFigureCollection = new PathFigureCollection();
//            myPathFigureCollection.Add(myPathFigure);

//            PathGeometry myPathGeometry = new PathGeometry();
//            myPathGeometry.Figures = myPathFigureCollection;

//            return myPathGeometry;
//        }
//    }
//}
