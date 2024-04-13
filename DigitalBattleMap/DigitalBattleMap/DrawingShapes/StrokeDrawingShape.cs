using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalBattleMap.DrawingShapes;

public class StrokeDrawingShape : DrawingShape
{
    public StrokeDrawingShape(Action applyShapeCallback, ICanvasSize canvasSize, int gridSize) : base(applyShapeCallback, canvasSize, gridSize)
    {
    }

    public override bool IsErasable => true;

    protected override void ButtonDown(Point<double> position)
    {
        Points.Add(position);
    }

    protected override void ButtonUp(Point<double> position)
    {
        if (SnapToGrid)
        {
            Points = new ObservableCollection<Point<double>>(SnapPointsToGrid(Points.ToList()));
        }
        ApplyShape();
    }

    protected override void MouseMove(Point<double> position, bool buttonDown)
    {
        if (buttonDown)
        {
            if (!Points.Contains(position))
            {
                Points.Add(position);
            }
        }
    }

    private List<Point<double>> SnapPointsToGrid(List<Point<double>> points)
    {
        var gridOffset = CalculateCanvasGridOffset();
        double gridSize = CalculateCanvasGridSize();

        var tasks = new List<Task>();
        object lockObject = "";
        var snappedPoints = new List<Point<double>>();

        foreach (var point in points)
        {
            var task = Task.Run(() =>
            {
                var snappedPoint = SnapPointToGrid(point, gridOffset, gridSize);

                lock (lockObject)
                {
                    if (!snappedPoints.Contains(snappedPoint))
                    {
                        snappedPoints.Add(snappedPoint);
                    }
                }
            });
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());
        return snappedPoints;
    }

    private Point<double> SnapPointToGrid(Point<double> point, Point<double> gridOffset, double gridSize)
    {
        var result = new Point<double>(point);
        var x = result.X - gridOffset.X;
        var y = result.Y - gridOffset.Y;
        var leftOverX = x % gridSize;
        var leftOverY = y % gridSize;

        if (leftOverX < gridSize / 2)
        {
            result.X -= leftOverX;
        }
        else
        {
            result.X += (gridSize - leftOverX);
        }

        if (leftOverY < gridSize / 2)
        {
            result.Y -= leftOverY;
        }
        else
        {
            result.Y += (gridSize - leftOverY);
        }

        return result;
    }

    private Point<double> CalculateCanvasGridOffset()
    {
        var gridOffset = Point<double>.Create(BitmapTools.CalculateGridOffset(_gridSize));
        return new(gridOffset.X.Map(0, Constants.BitmapSize.Width, 0, _canvasSize.Width), gridOffset.Y.Map(0, Constants.BitmapSize.Height, 0, _canvasSize.Height));
    }

    private double CalculateCanvasGridSize()
    {
        double inkCanvasGridSize = _gridSize;
        return inkCanvasGridSize.Map(0, Constants.BitmapSize.Width, 0, _canvasSize.Width);
    }
}
