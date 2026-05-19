using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DigitalBattleMap.UIElements;

public static class HexGeometry
{
    public static readonly double Sqrt3 = Math.Sqrt(3.0);

    // Axial direction vectors for the 6 flat-top hex neighbors.
    private static readonly (int dq, int dr)[] NeighborDirections =
    {
        ( 1,  0), (-1,  0),
        ( 0,  1), ( 0, -1),
        ( 1, -1), (-1,  1),
    };

    // Returns all 6 axial neighbors of (q, r), filtered to lie within the even-q grid.
    // rOff accounts for the even-q offset: column q starts at row -q/2.
    public static IEnumerable<(int q, int r)> GetNeighbors(int q, int r, int gridWidth, int gridHeight)
    {
        foreach (var (dq, dr) in NeighborDirections)
        {
            int nq = q + dq;
            int nr = r + dr;
            if (nq < 0 || nq >= gridWidth) continue;
            int rOff = nq / 2;
            if (nr < -rOff || nr >= gridHeight - rOff) continue;
            yield return (nq, nr);
        }
    }

    public static Point[] GetCorners(double centerX, double centerY, double size)
    {
        var corners = new Point[6];
        for (int i = 0; i < 6; i++)
        {
            double angle = Math.PI / 3.0 * i;
            corners[i] = new Point(centerX + size * Math.Cos(angle), centerY + size * Math.Sin(angle));
        }
        return corners;
    }

    public static (double X, double Y) AxialToPixel(int q, int r, double size)
    {
        return (size * 1.5 * q, size * (Sqrt3 / 2.0 * q + Sqrt3 * r));
    }

    public static (int Q, int R) PixelToAxial(double x, double y, double size)
    {
        double q = 2.0 / 3.0 * x / size;
        double r = (-1.0 / 3.0 * x + Sqrt3 / 3.0 * y) / size;
        return RoundAxial(q, r);
    }

    public static StreamGeometry CreateStreamGeometry(double cx, double cy, double size)
    {
        var sg = new StreamGeometry();
        using var ctx = sg.Open();
        var c = GetCorners(cx, cy, size);
        ctx.BeginFigure(c[0], true, true);
        ctx.PolyLineTo(new[] { c[1], c[2], c[3], c[4], c[5] }, true, false);
        return sg;
    }

    private static (int Q, int R) RoundAxial(double q, double r)
    {
        double s = -q - r;
        int rq = (int)Math.Round(q);
        int rr = (int)Math.Round(r);
        int rs = (int)Math.Round(s);
        double dq = Math.Abs(rq - q);
        double dr = Math.Abs(rr - r);
        double ds = Math.Abs(rs - s);
        if (dq > dr && dq > ds) rq = -rr - rs;
        else if (dr > ds) rr = -rq - rs;
        return (rq, rr);
    }
}
