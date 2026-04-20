using Clipper2Lib;
using DigitalBattleMap.DataClasses;
using ImageMagick.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBattleMap.UIElements;

/// <summary>
/// Helper class for performing polygon operations on fog shapes.
/// Provides overlap detection and polygon subtraction for non-overlapping fog visualization.
/// </summary>
public static class PolygonUnionHelper
{
    private const double Tolerance = 0.001;
    /// <summary>
    /// Detects if two polygons overlap by checking if any point of one polygon is inside the other
    /// or if any edges intersect.
    /// </summary>
    public static bool PolygonsOverlap(List<Point<double>> polygon1, List<Point<double>> polygon2)
    {
        if (polygon1.Count < 3 || polygon2.Count < 3)
            return false;

        // Check if any point of polygon1 is inside polygon2
        foreach (var point in polygon1)
        {
            if (PointInPolygon(point, polygon2))
                return true;
        }

        // Check if any point of polygon2 is inside polygon1
        foreach (var point in polygon2)
        {
            if (PointInPolygon(point, polygon1))
                return true;
        }

        // Check if any edges intersect
        for (int i = 0; i < polygon1.Count - 1; i++)
        {
            for (int j = 0; j < polygon2.Count - 1; j++)
            {
                if (SegmentsIntersect(polygon1[i], polygon1[i + 1], polygon2[j], polygon2[j + 1]))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Subtracts polygon2 from polygon1 using smart boundary tracing.
    /// Traces polygon2's boundary using edge indices to determine direction.
    /// </summary>
    public static List<Point<double>> SubtractPolygon(List<Point<double>> polygon1, List<Point<double>> polygon2)
    {
        const double scale = 1000.0;

        var subject = new Paths64 { ToPath64(polygon1, scale) };
        var clip = new Paths64 { ToPath64(polygon2, scale) };

        var solution = Clipper.Difference(subject, clip, FillRule.NonZero);

        return FromPaths(solution, scale).FirstOrDefault();
    }

    private static Path64 ToPath64(List<Point<double>> points, double scale)
    {
        var path = new Path64(points.Count);

        foreach (var p in points)
        {
            path.Add(new Point64(
                (long)(p.X * scale),
                (long)(p.Y * scale)
            ));
        }

        return path;
    }

    private static List<List<Point<double>>> FromPaths(Paths64 paths, double scale)
    {
        return paths.Select(path =>
            path.Select(p => new Point<double>(
                p.X / scale,
                p.Y / scale
            )).ToList()
        ).ToList();
    }

    /// <summary>
    /// Finds intersection points between a line segment and a polygon, returning edge indices.
    /// </summary>
    private static List<(Point<double> point, int poly2EdgeIdx)> GetEdgePolygonIntersectionsWithIndices(
        Point<double> edgeStart, Point<double> edgeEnd, List<Point<double>> polygon)
    {
        var intersections = new List<(Point<double>, int)>();

        for (int i = 0; i < polygon.Count; i++)
        {
            var polyStart = polygon[i];
            var polyEnd = polygon[(i + 1) % polygon.Count];

            if (TryGetIntersection(edgeStart, edgeEnd, polyStart, polyEnd, out var intersection))
            {
                // Avoid duplicates
                if (!intersections.Any(p => Distance(p.Item1, intersection) < Tolerance))
                {
                    intersections.Add((intersection, i));
                }
            }
        }

        return intersections;
    }

    /// <summary>
    /// Traces along a polygon's boundary between two intersection points.
    /// Uses the edge indices to determine the correct direction to trace.
    /// </summary>
    private static List<Point<double>> TracePolygonBoundary(List<Point<double>> polygon, Point<double> startPoint, Point<double> endPoint, 
        int startEdgeIdx, int endEdgeIdx)
    {
        var result = new List<Point<double>>();
        var tolerance = 0.001;

        // Add start point
        result.Add(startPoint);

        // Determine the direction to trace (clockwise or counter-clockwise)
        // by checking which direction gets us to the end point with fewer steps
        int currentEdgeIdx = startEdgeIdx;
        int steps = 0;
        int maxSteps = polygon.Count * 2;

        while (steps < maxSteps)
        {
            // Move to next vertex in the polygon
            currentEdgeIdx = (currentEdgeIdx + 1) % polygon.Count;
            var currentVertex = polygon[currentEdgeIdx];

            // Add this vertex to result
            if (Distance(result.Last(), currentVertex) > tolerance)
            {
                result.Add(currentVertex);
            }

            // Check if we've reached or passed the end edge
            if (currentEdgeIdx == endEdgeIdx)
            {
                break;
            }

            steps++;
        }

        // Add end point if not already added
        if (Distance(result.Last(), endPoint) > tolerance)
        {
            result.Add(endPoint);
        }

        return result;
    }

    /// <summary>
    /// Comparer for Point<double> with tolerance.
    /// </summary>
    private class PointComparer : IEqualityComparer<Point<double>>
    {
        private readonly double _tolerance;

        public PointComparer(double tolerance)
        {
            _tolerance = tolerance;
        }

        public bool Equals(Point<double> x, Point<double> y)
        {
            return Distance(x, y) < _tolerance;
        }

        public int GetHashCode(Point<double> obj)
        {
            return obj.GetHashCode();
        }
    }

    /// <summary>
    /// Determines if a point is inside a polygon using the ray casting algorithm.
    /// </summary>
    public static bool PointInPolygon(Point<double> point, List<Point<double>> polygon)
    {
        if (polygon.Count < 3)
            return false;

        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var pi = polygon[i];
            var pj = polygon[j];

            if (((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    /// <summary>
    /// Tries to find the intersection point between two line segments.
    /// Returns true if segments intersect (not just their infinite lines).
    /// </summary>
    private static bool TryGetIntersection(Point<double> p1, Point<double> p2, Point<double> p3, Point<double> p4, out Point<double> intersection)
    {
        intersection = new Point<double>(0, 0);

        var x1 = p1.X;
        var y1 = p1.Y;
        var x2 = p2.X;
        var y2 = p2.Y;
        var x3 = p3.X;
        var y3 = p3.Y;
        var x4 = p4.X;
        var y4 = p4.Y;

        var denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

        // Parallel lines
        if (Math.Abs(denom) < 0.0001)
            return false;

        var t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
        var u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom;

        // Check if intersection point is within both segments
        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
        {
            intersection = new Point<double>(
                x1 + t * (x2 - x1),
                y1 + t * (y2 - y1)
            );
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculates the Euclidean distance between two points.
    /// </summary>
    private static double Distance(Point<double> p1, Point<double> p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Checks if two line segments intersect.
    /// </summary>
    private static bool SegmentsIntersect(Point<double> p1, Point<double> p2, Point<double> p3, Point<double> p4)
    {
        var ccw1 = CCW(p1, p3, p4);
        var ccw2 = CCW(p2, p3, p4);
        var ccw3 = CCW(p1, p2, p3);
        var ccw4 = CCW(p1, p2, p4);

        return ccw1 != ccw2 && ccw3 != ccw4;
    }

    /// <summary>
    /// Counter-clockwise test for three points.
    /// </summary>
    private static bool CCW(Point<double> A, Point<double> B, Point<double> C)
    {
        return (C.Y - A.Y) * (B.X - A.X) > (B.Y - A.Y) * (C.X - A.X);
    }
}
