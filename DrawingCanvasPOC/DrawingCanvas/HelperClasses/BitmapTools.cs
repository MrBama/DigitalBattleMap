using System.Collections.Generic;
using System;
using System.Windows;

namespace DrawingCanvas;

public static class BitmapTools
{
    public static void SmoothLine(IList<Point> points, int penSize)
    {
        bool smoothRequired = true;

        while (smoothRequired)
        {
            smoothRequired = false;
            for (int i = 0; i < points.Count - 1; i++)
            {
                var coord1 = points[i];
                var coord2 = points[i + 1];

                var dist = Math.Sqrt(Math.Pow(coord1.X - coord2.X, 2) + Math.Pow(coord1.Y - coord2.Y, 2));
                if (dist > (penSize / 6))
                {
                    var newPoint = new Point((coord1.X + coord2.X) / 2, (coord1.Y + coord2.Y) / 2);
                    points.Insert(i + 1, newPoint);
                    smoothRequired = true;
                }
            }
        }
    }
}
