using System;

namespace DigitalBattleMap.DataClasses;

public class Size<T> where T : IEquatable<T>
{
    public Size()
    {
    }

    public Size(T width, T height)
    {
        Width = width;
        Height = height;
    }

    public Size(Size<T> size)
    {
        Width = size.Width;
        Height = size.Height;
    }

    public static Size<T> Create<T1>(Size<T1> size) where T1 : IEquatable<T1>
    {
        dynamic width = size.Width;
        dynamic height = size.Height;

        var result = new Size<T>
        {
            Width = (T)width,
            Height = (T)height
        };

        return result;
    }

    public T Width { get; set; }

    public T Height { get; set; }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is Size<T> other && other.Width.Equals(Width) && other.Height.Equals(Height);
    }

    public override string ToString()
    {
        return $"W: {Width}, H: {Height}";
    }
}
