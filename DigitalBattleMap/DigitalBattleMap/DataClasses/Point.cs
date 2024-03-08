using System;

namespace DigitalBattleMap.DataClasses;

public class Point<T> : IEquatable<Point<T>> where T : IEquatable<T>
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

        var result = new Point<T>
        {
            X = (T)x,
            Y = (T)y
        };
        return result;
    }

    public T X { get; set; }

    public T Y { get; set; }

    public void Rotate(Point<T> rotationOrigin, int angle)
    {
        dynamic radians = (Math.PI / 180) * angle;
        dynamic sin = Math.Sin(radians);
        dynamic cos = Math.Cos(radians);

        dynamic x = X;
        dynamic y = Y;

        // Translate point back to origin
        x -= rotationOrigin.X;
        y -= rotationOrigin.Y;

        // Rotate point
        dynamic xnew = x * cos - y * sin;
        dynamic ynew = x * sin + y * cos;

        // Translate point back
        X = xnew + rotationOrigin.X;
        Y = ynew + rotationOrigin.Y;
    }

    public bool Equals(Point<T>? other)
    {
        return other != null && other.X.Equals(X) && other.Y.Equals(Y);
    }

    public override bool Equals(object? obj)
    {
        return obj is Point<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return $"X: {X}, Y: {Y}";
    }
}
