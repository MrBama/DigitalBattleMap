using DigitalBattleMap.DataClasses;
using System.Collections.Generic;
using System;
using System.Linq;

namespace DigitalBattleMap.Utilities;

public partial class Mathematics
{
    /// <summary>
    /// This class calculates which grid cells are covered by a certain polygon.
    /// </summary>
    private static class CoveredGridCellsCalculator
    {
        public static List<GridCell> CalculateCoveredGridCells(List<Point<double>> polygon, double gridSize, double minCoveragePercentage = 1.0)
        {
            var gridCells = new List<GridCell>();
            if (polygon == null || polygon.Count < 3)
            {
                return gridCells;
            }

            // STEP 1: Broad-phase filtering. Instead of evaluating every cell in the grid, 
            // we use a fast scanline and line-trace algorithm to collect a "HashSet" of 
            // candidate cells that the polygon structurally overlaps or touches.
            var candidates = GetCandidateCells(polygon, gridSize);
            double maxCellArea = gridSize * gridSize;

            // STEP 2: Narrow-phase math. Process each candidate cell to calculate its exact intersection area.
            foreach (var cell in candidates)
            {
                // Calculate the physical bounding boundaries for this specific grid cell in world space
                double xMin = cell.X * gridSize;
                double xMax = xMin + gridSize;
                double yMin = cell.Y * gridSize;
                double yMax = yMin + gridSize;

                // Clip the world space polygon strictly against the 4 boundaries of this specific grid cell.
                // This leaves us with a modified, smaller polygon representing ONLY the part inside the cell.
                List<Point<double>> clipped = ClipToAxisAlignedBounds(polygon, xMin, xMax, yMin, yMax);

                // Use the Shoelace formula to find the exact area of the clipped piece
                double intersectionArea = CalculatePolygonArea(clipped);
                double coverageRatio = intersectionArea / maxCellArea;
                double clampedMinCoveragePrecentage = Math.Clamp(minCoveragePercentage, 0.01, 1.0);

                // If the chunk inside the cell meets or exceeds your threshold, keep it.
                if (coverageRatio >= clampedMinCoveragePrecentage)
                {
                    gridCells.Add(cell);
                }
            }

            return gridCells;
        }

        /// <summary>
        /// Clips a polygon against an axis-aligned bounding box (the grid cell) one side at a time.
        /// This method is winding-order independent (works with clockwise or counter-clockwise coordinates).
        /// </summary>
        private static List<Point<double>> ClipToAxisAlignedBounds(List<Point<double>> poly, double xMin, double xMax, double yMin, double yMax)
        {
            List<Point<double>> current = poly;

            // Successively slice the polygon against the Left, Right, Bottom, and Top boundaries
            current = ClipSide(current, isX: true, isGreater: true, limit: xMin);  // Keep parts where X >= xMin
            current = ClipSide(current, isX: true, isGreater: false, limit: xMax);  // Keep parts where X <= xMax
            current = ClipSide(current, isX: false, isGreater: true, limit: yMin);  // Keep parts where Y >= yMin
            current = ClipSide(current, isX: false, isGreater: false, limit: yMax);  // Keep parts where Y <= yMax

            return current;
        }

        /// <summary>
        /// Slices a polygon along a single linear grid line axis.
        /// </summary>
        private static List<Point<double>> ClipSide(List<Point<double>> poly, bool isX, bool isGreater, double limit)
        {
            var output = new List<Point<double>>();
            if (poly.Count == 0) return output;

            // Start by comparing the last vertex of the polygon to the first vertex
            Point<double> s = poly[^1];

            foreach (Point<double> p in poly)
            {
                // Check if the current vertex (p) and previous vertex (s) are on the "inside" side of the grid line
                bool pInside = isX ? (isGreater ? p.X >= limit : p.X <= limit) : (isGreater ? p.Y >= limit : p.Y <= limit);
                bool sInside = isX ? (isGreater ? s.X >= limit : s.X <= limit) : (isGreater ? s.Y >= limit : s.Y <= limit);

                if (pInside)
                {
                    // If moving from outside to inside, find the intersection Point<double> on the grid line and add it
                    if (!sInside) output.Add(GetAxisIntersect(s, p, isX, limit));
                    output.Add(p);
                }
                else if (sInside)
                {
                    // If moving from inside to outside, find the intersection Point<double> on the grid line and add it
                    output.Add(GetAxisIntersect(s, p, isX, limit));
                }
                s = p; // Move to the next edge segment
            }
            return output;
        }

        /// <summary>
        /// Calculates the exact intersection Point<double> where a line segment crosses a grid line boundary.
        /// </summary>
        private static Point<double> GetAxisIntersect(Point<double> s, Point<double> p, bool isX, double limit)
        {
            if (isX)
            {
                if (Math.Abs(p.X - s.X) < 0.000001) return s; // Avoid dividing by zero if lines are parallel
                double t = (limit - s.X) / (p.X - s.X);       // Interpolation factor t (0.0 to 1.0)
                return new Point<double>(limit, s.Y + t * (p.Y - s.Y));
            }
            else
            {
                if (Math.Abs(p.Y - s.Y) < 0.000001) return s;
                double t = (limit - s.Y) / (p.Y - s.Y);
                return new Point<double>(s.X + t * (p.X - s.X), limit);
            }
        }

        /// <summary>
        /// Calculates the surface area of any non-self-intersecting polygon using the Shoelace Formula.
        /// </summary>
        private static double CalculatePolygonArea(List<Point<double>> poly)
        {
            if (poly.Count < 3) return 0.0;
            double area = 0.0;

            // Sum cross-multiplications of vertices around the perimeter
            for (int i = 0; i < poly.Count; i++)
            {
                Point<double> p1 = poly[i];
                Point<double> p2 = poly[(i + 1) % poly.Count];
                area += (p1.X * p2.Y) - (p2.X * p1.Y);
            }
            return Math.Abs(area) / 2.0;
        }

        // --- BROAD-PHASE CANDIDATE GENERATION ---
        private static HashSet<GridCell> GetCandidateCells(List<Point<double>> worldPolygon, double gridSize)
        {
            var touchedCells = new HashSet<GridCell>();

            // Scale the polygon down to a normalized grid coordinate space (where cell dimensions are 1x1)
            List<Point<double>> gridPolygon = worldPolygon.Select(p => new Point<double>(p.X / gridSize, p.Y / gridSize)).ToList();

            // Trace the outer edges of the polygon to catch cells intersected by the perimeter
            for (int i = 0; i < gridPolygon.Count; i++)
            {
                TraceEdge(gridPolygon[i], gridPolygon[(i + 1) % gridPolygon.Count], touchedCells);
            }

            // Run a standard Scanline fill down the centers of rows to catch completely enclosed interior cells
            double minY = gridPolygon.Min(p => p.Y);
            double maxY = gridPolygon.Max(p => p.Y);
            int startRow = (int)Math.Floor(minY);
            int endRow = (int)Math.Floor(maxY);

            for (int y = startRow; y <= endRow; y++)
            {
                double scanY = y + 0.5; // Look right through the center vertical offset of the cell row
                var intersections = new List<double>();

                for (int i = 0; i < gridPolygon.Count; i++)
                {
                    Point<double> p1 = gridPolygon[i];
                    Point<double> p2 = gridPolygon[(i + 1) % gridPolygon.Count];
                    if ((p1.Y <= scanY && p2.Y > scanY) || (p2.Y <= scanY && p1.Y > scanY))
                    {
                        double t = (scanY - p1.Y) / (p2.Y - p1.Y);
                        intersections.Add(p1.X + t * (p2.X - p1.X));
                    }
                }

                intersections.Sort();
                for (int i = 0; i < intersections.Count; i += 2)
                {
                    if (i + 1 >= intersections.Count) break;
                    int startCol = (int)Math.Floor(intersections[i] + 0.00001); // Epsilon offset ensures perfect edge safety
                    int endCol = (int)Math.Floor(intersections[i + 1] - 0.00001);
                    for (int x = startCol; x <= endCol; x++) touchedCells.Add(new GridCell(x, y));
                }
            }
            return touchedCells;
        }

        /// <summary>
        /// Standard integer-based step tracer. Evaluates a line segment in grid space and registers 
        /// every single grid index coordinate it slices through.
        /// </summary>
        private static void TraceEdge(Point<double> p1, Point<double> p2, HashSet<GridCell> cells)
        {
            int x1 = (int)Math.Floor(p1.X); int y1 = (int)Math.Floor(p1.Y);
            int x2 = (int)Math.Floor(p2.X); int y2 = (int)Math.Floor(p2.Y);
            int dx = Math.Abs(x2 - x1); int dy = Math.Abs(y2 - y1);
            int x = x1; int y = y1;
            int sx = (x1 < x2) ? 1 : -1; int sy = (y1 < y2) ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                cells.Add(new GridCell(x, y));
                if (x == x2 && y == y2) break; // Hard integer check stops infinite looping completely
                int e2 = 2 * err;
                if (e2 > -dy && e2 < dx) cells.Add(new GridCell(x + sx, y)); // Captures corner/diagonal overlaps
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
        }
    }
}
