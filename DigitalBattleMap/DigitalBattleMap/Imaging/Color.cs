using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap.Imaging;
public struct Color
{
    public Color(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public byte R { get; }

    public byte G { get; }

    public byte B { get; }

    public byte A { get; }

    public static Color Black => new Color(0, 0, 0, 255);

    public static implicit operator System.Drawing.Color(Color color)
    {
        // TODO (Bas): Remove this
        return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static implicit operator Color(System.Drawing.Color color)
    {
        // TODO (Bas): Remove this
        return new Color(color.R, color.G, color.B, color.A);
    }

    public static implicit operator Color(System.Windows.Media.Color color)
    {
        // TODO (Bas): Remove this
        return new Color(color.R, color.G, color.B, color.A);
    }

    public static implicit operator SixLabors.ImageSharp.Color(Color color)
    {
        // TODO (Bas): Remove this
        return SixLabors.ImageSharp.Color.FromRgba(color.R, color.G, color.B, color.A);
    }
}
