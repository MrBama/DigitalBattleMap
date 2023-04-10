using System;

namespace DigitalBattleMap
{
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
}
