using System;
using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public class Polygon
{
    public Polygon()
    {
    }

    public Polygon(Polygon polygon)
    {
        Points = new List<Point<double>>(polygon.Points);
    }

    public List<Point<double>> Points { get; set; }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is Polygon other && other.Points.Equals(Points);
    }
}

