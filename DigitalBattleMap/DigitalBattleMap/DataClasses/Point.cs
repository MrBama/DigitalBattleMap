using System;

namespace DigitalBattleMap.DataClasses;

public class Point<T> where T : IEquatable<T>
{
    public Point()
    {
    }

    public Point(T x, T y)
    {
        X = x;
        Y = y;
    }

    public Point(Point<T> point)
    {
        X = point.X;
        Y = point.Y;
    }

    public static Point<T> Create<T1>(Point<T1> point) where T1 : IEquatable<T1>
    {
        dynamic x = point.X;
        dynamic y = point.Y;

        var result = new Point<T>();
        result.X = (T)x;
        result.Y = (T)y;
        return result;
    }

    public T X { get; set; }

    public T Y { get; set; }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is Point<T> other && other.X.Equals(X) && other.Y.Equals(Y);
    }

    public override string ToString()
    {
        return $"X: {X}, Y: {Y}";
    }
}
