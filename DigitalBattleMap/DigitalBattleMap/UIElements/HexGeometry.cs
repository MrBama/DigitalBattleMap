using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DigitalBattleMap.UIElements;

public static class HexGeometry
{
    public static readonly double Sqrt3 = Math.Sqrt(3.0);

    // Axial direction vectors for the 6 flat-top hex neighbors.
    public static readonly (int dq, int dr)[] NeighborDirections =
    {
        ( 1,  0), (-1,  0),
        ( 0,  1), ( 0, -1),
        ( 1, -1), (-1,  1),
    };

    // dir d's opposite: 0↔1, 2↔3, 4↔5
    private static readonly int[] OppositeDir = { 1, 0, 3, 2, 5, 4 };

    // Which two corner indices (from GetCorners) form the edge shared with neighbor in that direction.
    // Derived from flat-top geometry: dir0=(1,0) shares corners 0-1, etc.
    private static readonly (int a, int b)[] EdgeCorners =
    {
        (0, 1), (3, 4), (1, 2), (4, 5), (5, 0), (2, 3)
    };

    // For each corner index 0-5, the two edge directions of THIS hex that touch that corner.
    private static readonly (int d1, int d2)[] CornerEdgeDirsTable =
    {
        (0, 4),  // corner 0
        (0, 2),  // corner 1
        (2, 5),  // corner 2
        (1, 5),  // corner 3
        (1, 3),  // corner 4
        (3, 4),  // corner 5
    };

    // For each corner index 0-5, the canonical edge at (q+dq, r+dr, dir) that is the
    // third edge sharing that vertex (from a neighboring hex, already in canonical form).
    private static readonly (int dq, int dr, int dir)[] ThirdEdgeTable =
    {
        (+1, -1, 2),  // corner 0
        ( 0, +1, 4),  // corner 1
        (-1, +1, 0),  // corner 2
        (-1,  0, 2),  // corner 3
        (-1,  0, 4),  // corner 4
        ( 0, -1, 0),  // corner 5
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

    // Returns the neighbour of (q,r) in direction dir (no bounds check).
    public static (int Q, int R) GetNeighborAt(int q, int r, int dir)
    {
        var (dq, dr) = NeighborDirections[dir];
        return (q + dq, r + dr);
    }

    // Returns the two corner indices that form the edge in direction dir.
    public static (int A, int B) GetEdgeCornerIndices(int dir) => EdgeCorners[dir];

    // Returns the midpoint of the edge shared with the neighbour in direction dir.
    public static Point GetEdgeMidpoint(double cx, double cy, double size, int dir)
    {
        var corners = GetCorners(cx, cy, size);
        var (a, b) = EdgeCorners[dir];
        return new Point((corners[a].X + corners[b].X) / 2.0, (corners[a].Y + corners[b].Y) / 2.0);
    }

    public static (int d1, int d2) GetCornerEdgeDirs(int cornerIdx) => CornerEdgeDirsTable[cornerIdx];
    public static (int dq, int dr, int dir) GetThirdEdgeAtCorner(int cornerIdx) => ThirdEdgeTable[cornerIdx];

    // Returns the canonical (Q, R, dir) for the edge between (q,r) and its dir-neighbour.
    // Canonical = the side whose (Q,R) is lexicographically smaller.
    public static (int Q, int R, int Dir) CanonicalizeEdge(int q, int r, int dir)
    {
        var (nq, nr) = GetNeighborAt(q, r, dir);
        if (q < nq || (q == nq && r <= nr))
            return (q, r, dir);
        return (nq, nr, OppositeDir[dir]);
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
