using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
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
}
