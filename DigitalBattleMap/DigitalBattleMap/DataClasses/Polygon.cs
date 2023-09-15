using System;
using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public class Polygon<T> where T : IEquatable<T>
{
    public Polygon()
    {
    }

    public Polygon(Polygon<T> polygon)
    {
        Points = new List<Point<T>>(polygon.Points);
    }

    public List<Point<T>> Points { get; set; }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is Polygon<T> other && other.Points.Equals(Points);
    }
}

