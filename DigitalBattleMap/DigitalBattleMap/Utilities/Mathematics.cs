using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalBattleMap.Utilities;

public static class Mathematics
{
    public static List<Point<double>> SnapPointsToGrid(List<Point<double>> points, Point<double> gridOffset, double gridSize)
    {
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

    public static Point<double> SnapPointToGrid(Point<double> point, Point<double> gridOffset, double gridSize)
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

    public static Point<int> CalculateGridOffset(int gridSize)
    {
        var middleGridCellX = (Constants.BitmapSize.Width / 2) - (gridSize / 2);
        var middleGridCellY = (Constants.BitmapSize.Height / 2) - (gridSize / 2);

        var xModulo = middleGridCellX % gridSize;
        var yModulo = middleGridCellY % gridSize;

        var startX = xModulo == 0 ? 0 : xModulo;
        var startY = yModulo == 0 ? 0 : yModulo;

        return new(startX, startY);
    }

    public static List<Point<double>> SnapPointsToCanvasGrid(List<Point<double>> points, ICanvasSize canvasSize, int gridSize, int snapPointGridSize)
    {
        var canvasGridSize = CalculateCanvasGridSize(canvasSize, snapPointGridSize);
        var canvasGridOffset = CalculateCanvasGridOffset(canvasSize, gridSize);
        return SnapPointsToGrid(points, canvasGridOffset, canvasGridSize);
    }

    public static List<Point<double>> SnapPointsToCanvasGrid(List<Point<double>> points, ICanvasSize canvasSize, int gridSize)
    {
        return SnapPointsToCanvasGrid(points, canvasSize, gridSize, gridSize);
    }

    public static Point<double> SnapPointToCanvasGrid(Point<double> point, ICanvasSize canvasSize, int gridSize, int snapPointGridSize)
    {
        var canvasGridSize = CalculateCanvasGridSize(canvasSize, snapPointGridSize);
        var canvasGridOffset = CalculateCanvasGridOffset(canvasSize, gridSize);
        return SnapPointToGrid(point, canvasGridOffset, canvasGridSize);
    }

    public static Point<double> CalculateCanvasGridOffset(ICanvasSize canvasSize, int gridSize)
    {
        var gridOffset = Point<double>.Create(CalculateGridOffset(gridSize));
        return new(gridOffset.X.Map(0, Constants.BitmapSize.Width, 0, canvasSize.Width), gridOffset.Y.Map(0, Constants.BitmapSize.Height, 0, canvasSize.Height));
    }

    public static double CalculateCanvasGridSize(ICanvasSize canvasSize, int gridSize)
    {
        double canvasGridSize = gridSize;
        return canvasGridSize.Map(0, Constants.BitmapSize.Width, 0, canvasSize.Width);
    }
}
