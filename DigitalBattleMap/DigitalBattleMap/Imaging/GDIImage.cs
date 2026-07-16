using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.Imaging;

internal class GDIImage : IImage
{
    private readonly Bitmap bitmap;

    public GDIImage(int width, int height)
    {
        bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
    }

    internal GDIImage(Bitmap bitmap)
    {
        this.bitmap = bitmap;
    }

    public int Width => bitmap.Width;

    public int Height => bitmap.Height;

    public void Clear(Color color)
    {
        using var gfx = Graphics.FromImage(bitmap);
        gfx.Clear(color);
    }

    public void DrawEllipse(Color color, int lineWidth, Rectangle rectangle)
    {
        using var gfx = Graphics.FromImage(bitmap);
        gfx.DrawEllipse(new Pen(color, lineWidth), rectangle.TopLeftX, rectangle.TopLeftY, rectangle.Width, rectangle.Height);
    }

    public void DrawRectangle(Color color, int lineWidth, Rectangle rectangle)
    {
        using var gfx = Graphics.FromImage(bitmap);
        gfx.DrawRectangle(new Pen(color, lineWidth), rectangle.TopLeftX, rectangle.TopLeftY, rectangle.Width, rectangle.Height);
    }

    public void FillEllipse(Color color, Rectangle rectangle)
    {
        using var gfx = Graphics.FromImage(bitmap);
        gfx.FillEllipse(new SolidBrush(color), rectangle.TopLeftX, rectangle.TopLeftY, rectangle.Width, rectangle.Height);
    }

    public void FillRectangle(Color color, Rectangle rectangle)
    {
        using var gfx = Graphics.FromImage(bitmap);
        gfx.FillRectangle(new SolidBrush(color), rectangle.TopLeftX, rectangle.TopLeftY, rectangle.Width, rectangle.Height);
    }

    public void FillPolygon(Color color, Point<float>[] points, bool? blendColors = true)
    {
        using var gfx = Graphics.FromImage(bitmap);
        if (blendColors.HasValue && !blendColors.Value)
        {
            gfx.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
        }
        gfx.FillPolygon(new SolidBrush(color), points.Select(p => new PointF(p.X, p.Y)).ToArray());
    }

    public void FillSmoothPolygon(Color color, Point<float>[] points)
    {
        using var gfx = Graphics.FromImage(bitmap);
        gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        gfx.FillPolygon(new SolidBrush(color), points.Select(p => new PointF(p.X, p.Y)).ToArray());
    }

    public void DrawLine(Color color, int lineSize, int x1, int y1, int x2, int y2)
    {
        using var gfx = Graphics.FromImage(bitmap);
        gfx.DrawLine(new Pen(color, lineSize), x1, y1, x2, y2);
    }

    public IImage CropTo(Rectangle rectangle)
    {
        var croppedImage = new GDIImage(rectangle.Width, rectangle.Height);
        using var gfx = Graphics.FromImage(croppedImage.bitmap);
        gfx.DrawImage(
            bitmap,
            new System.Drawing.Rectangle(0, 0, croppedImage.Width, croppedImage.Height),
            new System.Drawing.Rectangle(rectangle.TopLeftX, rectangle.TopLeftY, rectangle.Width, rectangle.Height),
            GraphicsUnit.Pixel);

        return croppedImage;
    }

    public IImage ResizeTo(int width, int height)
    {
        var resizedBitmap = OpenCV.Resize(bitmap, new DataClasses.Size<int>(width, height));
        return new GDIImage(resizedBitmap);
    }

    public void DrawImage(IImage image, int x, int y)
    {
        DrawImage(image, Rectangle.FromTopLeft(x, y, image.Width, image.Height));
    }

    public void DrawImage(IImage image, Rectangle target)
    {
        var bitmap = image.ToDrawingBitmap();
        using var gfx = Graphics.FromImage(this.bitmap);
        //if (rectangle.Width != null && height != null)
        {
            gfx.DrawImage(bitmap, target.TopLeftX, target.TopLeftY, target.Width, target.Height);
        }
        //else
        //{
        //    gfx.DrawImage(bitmap, x, y);
        //}
    }

    public void Rotate(BitmapRotation angle)
    {
        var rotate = angle switch
        {
            BitmapRotation.Rotate0 => RotateFlipType.RotateNoneFlipNone,
            BitmapRotation.Rotate90 => RotateFlipType.Rotate90FlipNone,
            BitmapRotation.Rotate180 => RotateFlipType.Rotate180FlipNone,
            BitmapRotation.Rotate270 => RotateFlipType.Rotate270FlipNone,
            _ => throw new NotImplementedException()
        };

        bitmap.RotateFlip(rotate);
    }

    public void MakeColorTransparent(Color color)
    {
        bitmap.MakeTransparent(color);
    }

    public IImage Clone()
    {
        return new GDIImage(new Bitmap(this.bitmap));
    }

    public Stream GetPngStream()
    {
        var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        return stream;
    }

    public byte[] Serialize()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return bitmap.ToPng();
        }
        finally
        {
            Debug.WriteLine($"Serialing image: {sw.ElapsedMilliseconds} ms");
        }
    }

    public BitmapSource ToBitmapSource()
    {
        return bitmap.ToBitmapImage();
    }

    public Bitmap GetBitmap()
    {
        return bitmap;
    }
}
