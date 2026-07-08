using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap.Imaging;
internal class GDIImage : IImage, IDisposable
{
    private readonly Bitmap bitmap;
    private readonly Graphics gfx;

    public GDIImage(int width, int height)
    {
        bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        gfx = Graphics.FromImage(bitmap);
    }

    public int Width => bitmap.Width;

    public int Height => bitmap.Height;

    public void Clear(Color color)
    {
        gfx.Clear(color);
    }

    public void DrawEllipse(Color color, int lineWidth, int x, int y, int width, int height)
    {
        gfx.DrawEllipse(new Pen(color, lineWidth), x, y, width, height);
    }

    public void FillEllipse(Color color, int x, int y, int width, int height)
    {
        gfx.FillEllipse(new SolidBrush(color), x, y, width, height);
    }

    public void FillRectangle(Color color, int x, int y, int width, int height)
    {
        gfx.FillRectangle(new SolidBrush(color), x, y, width, height);
    }

    public void FillPolygon(Color color, PointF[] points)
    {
        gfx.FillPolygon(new SolidBrush(color), points);
    }

    public void Dispose()
    {
        gfx?.Dispose();
    }

    public Stream GetPngStream()
    {
        var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        return stream;
    }
}
