using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap.Imaging;
public struct Rectangle
{
    private Rectangle(int topLeftX, int topLeftY, int width, int height)
    {
        TopLeftX = topLeftX;
        TopLeftY = topLeftY;
        Width = width;
        Height = height;
    }

    public int TopLeftX { get; }

    public int TopLeftY { get; }

    public int Width { get; }

    public int Height { get; }

    public int CenterX => TopLeftX + (Width / 2);

    public int CenterY => TopLeftY + (Height / 2);

    public static Rectangle FromTopLeft(int x, int y, int width, int height)
    {
        return new Rectangle(x, y, width, height);
    }

    public static Rectangle FromCenter(int x, int y, int width, int height)
    {
        return new Rectangle(x - (width / 2), y - (height / 2), width, height);
    }
}
