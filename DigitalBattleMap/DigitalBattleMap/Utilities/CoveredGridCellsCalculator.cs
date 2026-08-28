using DigitalBattleMap.DataClasses;
using System.Collections.Generic;
using System;

namespace DigitalBattleMap.Utilities;

public partial class Mathematics
{
    /// <summary>
    /// This class calculates which grid cells are covered by a certain polygon.
    /// </summary>
    private static class CoveredGridCellsCalculator
    {
        private const double MARGIN_PRECENTAGE = 0.01;

        /// <summary>
        /// Returns the grid cells whose centers fall inside the given polygon.
        /// </summary>
        public static List<GridCell> CalculateCoveredGridCells(List<Point<double>> polygon, double cellSize)
        {
            var coveredCells = new List<GridCell>();

            if (polygon == null || polygon.Count < 3)
                return coveredCells;

            // Apply scale expansion with a margin around the centroid
            // This is to avoid edge cases
            List<Point<double>> expandedPolygon = InflatePolygon(polygon, MARGIN_PRECENTAGE);

            // 1. Calculate bounding box using the expanded polygon
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            foreach (var p in expandedPolygon)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            // 2. Align bounding box to grid indices
            int startCol = (int)Math.Floor((minX) / cellSize);
            int endCol = (int)Math.Floor((maxX) / cellSize);
            int startRow = (int)Math.Floor((minY) / cellSize);
            int endRow = (int)Math.Floor((maxY) / cellSize);

            // 3. Test each cell center against the expanded polygon
            for (int col = startCol; col <= endCol; col++)
            {
                for (int row = startRow; row <= endRow; row++)
                {
                    double cellMinX = col * cellSize;
                    double cellMinY = row * cellSize;
                    double centerX = cellMinX + cellSize / 2.0;
                    double centerY = cellMinY + cellSize / 2.0;

                    if (IsPointInPolygon(new Point<double>(centerX, centerY), expandedPolygon))
                    {
                        coveredCells.Add(new GridCell(col, row));
                    }
                }
            }

            return coveredCells;
        }

        public static List<Point<double>> InflatePolygon(List<Point<double>> polygon, double marginPercentage)
        {
            int count = polygon.Count;
            double centroidX = 0;
            double centroidY = 0;

            foreach (var p in polygon)
            {
                centroidX += p.X;
                centroidY += p.Y;
            }

            centroidX /= count;
            centroidY /= count;

            double scale = 1.0 + marginPercentage;
            var expanded = new List<Point<double>>(count);

            foreach (var p in polygon)
            {
                double newX = centroidX + (p.X - centroidX) * scale;
                double newY = centroidY + (p.Y - centroidY) * scale;
                expanded.Add(new Point<double>(newX, newY));
            }

            return expanded;
        }

        /// <summary>
        /// Ray-casting algorithm to determine if a point is inside a polygon.
        /// </summary>
        public static bool IsPointInPolygon(Point<double> point, List<Point<double>> polygon)
        {
            bool inside = false;
            int count = polygon.Count;

            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                Point<double> p1 = polygon[i];
                Point<double> p2 = polygon[j];

                if (((p1.Y > point.Y) != (p2.Y > point.Y)) &&
                    (point.X < (p2.X - p1.X) * (point.Y - p1.Y) / (p2.Y - p1.Y) + p1.X))
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
