using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalBattleMap.Utilities;

public static partial class Mathematics
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
        var gridOrigin = new Point<int>(Constants.MapSize.Width / 2, Constants.MapSize.Height / 2);
        return CalculateGridOffset(gridSize, gridOrigin);
    }

    public static Point<int> CalculateGridOffset(int gridSize, Point<int> gridOrigin)
    {
        return new(gridOrigin.X % gridSize, gridOrigin.Y % gridSize);
    }

    public static List<Point<double>> SnapPointsToCanvasGrid(List<Point<double>> points, IMapSize mapSize)
    {
        var canvasGridOffset = CalculateCanvasGridOffset(mapSize);
        return SnapPointsToGrid(points, canvasGridOffset, mapSize.CanvasGridSize);
    }

    public static Point<double> SnapPointToCanvasGrid(Point<double> point, IMapSize mapSize, double snapPointGridSize)
    {
        var canvasGridOffset = CalculateCanvasGridOffset(mapSize);
        return SnapPointToGrid(point, canvasGridOffset, snapPointGridSize);
    }

    // Snaps via integer map space so the result matches the exact pixel position the grid bitmap uses.
    // mapSpaceGridSize should be mapSize.GridSize (full grid) or mapSize.GridSize / 2.0 (half grid).
    public static Point<double> SnapPointToCanvasGridExact(Point<double> canvasPoint, IMapSize mapSize, double mapSpaceGridSize)
    {
        var gridOffset = CalculateGridOffset(mapSize.GridSize);
        var mapX = canvasPoint.X.Map(0.0, mapSize.CanvasWidth, 0.0, (double)mapSize.Width);
        var mapY = canvasPoint.Y.Map(0.0, mapSize.CanvasHeight, 0.0, (double)mapSize.Height);

        var snappedMapX = Math.Round((mapX - gridOffset.X) / mapSpaceGridSize, MidpointRounding.AwayFromZero) * mapSpaceGridSize + gridOffset.X;
        var snappedMapY = Math.Round((mapY - gridOffset.Y) / mapSpaceGridSize, MidpointRounding.AwayFromZero) * mapSpaceGridSize + gridOffset.Y;

        return new Point<double>(
            snappedMapX.Map(0.0, (double)mapSize.Width, 0.0, mapSize.CanvasWidth),
            snappedMapY.Map(0.0, (double)mapSize.Height, 0.0, mapSize.CanvasHeight));
    }

    public static Point<double> CalculateCanvasGridOffset(IMapSize mapSize)
    {
        var gridOffset = Point<double>.Create(CalculateGridOffset(mapSize.GridSize));
        return new(gridOffset.X.Map(0, mapSize.Width, 0, mapSize.CanvasWidth), gridOffset.Y.Map(0, mapSize.Height, 0, mapSize.CanvasHeight));
    }

    // C# modulo (e.g. 5 % 2) is actually calculating the remainder instead of an actual modulo
    public static T Modulo<T>(int left, int right)
    {
        dynamic leftd = left;
        dynamic rightd = right;
        return (Math.Abs(leftd * rightd) + leftd) % rightd;
    }

    public static T Min<T>(IEnumerable<T> values)
    {
        if (values.Count() == 0)
        {
            throw new ArgumentException("There should be atleast 1 number");
        }

        var min = values.First();

        for (int i = 1; i < values.Count(); i++)
        {
            dynamic number1 = min!;
            dynamic number2 = values.ElementAt(i)!;
            min = Math.Min(number1, number2);
        }

        return min;
    }

    public static T Max<T>(IEnumerable<T> values)
    {
        if (values.Count() == 0)
        {
            throw new ArgumentException("There should be atleast 1 number");
        }

        var max = values.First();

        for (int i = 1; i < values.Count(); i++)
        {
            dynamic number1 = max!;
            dynamic number2 = values.ElementAt(i)!;
            max = Math.Max(number1, number2);
        }

        return max;
    }

    public static List<GridCell> CalculateCoveredGridCells(List<Point<double>> polygon, double gridSize, double minCoveragePercentage = 1.0)
    {
        return CoveredGridCellsCalculator.CalculateCoveredGridCells(polygon, gridSize, minCoveragePercentage);
    }
}
