using DigitalBattleMap.DataClasses;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap.Imaging;
internal class SharpImage : IImage
{
    private readonly Image<Rgba32> image;

    public SharpImage(int width, int height)
    {
        image = new Image<Rgba32>(width, height);
    }

    public SharpImage(Image<Rgba32> image)
    {
        this.image = image;
    }

    public int Width => image.Width;

    public int Height => image.Height;

    public void Clear(Color color)
    {
        image.Mutate(i => i.Clear(color));
    }

    public IImage Clone()
    {
        return new SharpImage(image.Clone());
    }

    public IImage CropTo(int x, int y, int width, int height)
    {
        //var croppedImage = image.Clone();
        //croppedImage.Mutate(i => i.Crop(new Rectangle(Math.Max(0, x), Math.Max(0, y), Math.Min(croppedImage.Width, width), Math.Min(croppedImage.Height, height))));
        var croppedImage = new Image<Rgba32>(width, height);
        croppedImage.Mutate(i => i.DrawImage(image, new Point(0, 0), new SixLabors.ImageSharp.Rectangle(x, y, width, height), 1));
        return new SharpImage(croppedImage);
    }

    public void DrawEllipse(Color color, int lineWidth, Rectangle rectangle)
    {
        var ellipse = new EllipsePolygon(rectangle.CenterX, rectangle.CenterY, rectangle.Width, rectangle.Height);
        image.Mutate(x => x.Draw(color, lineWidth, ellipse));
    }

    public void DrawImage(IImage image, int x, int y, int? width = null, int? height = null)
    {
        var imageToDraw = image.ToSharpImage();
        if (width != null && height != null)
        {
            this.image.Mutate(i => i.DrawImage(imageToDraw, new SixLabors.ImageSharp.Rectangle(x, y, width.Value, height.Value), 1));
        }
        else
        {
            this.image.Mutate(i => i.DrawImage(imageToDraw, new Point(x, y), 1));
        }
    }

    public void DrawLine(Color color, int lineSize, int x1, int y1, int x2, int y2)
    {
        image.Mutate(x => x.DrawLine(color, lineSize, new Point(x1, y1), new Point(x2, y2)));
    }

    public void DrawRectangle(Color color, int lineWidth, int x, int y, int width, int height)
    {
        var rectangle = new RectangularPolygon(x, y, width, height);
        image.Mutate(x => x.Draw(color, lineWidth, rectangle));
    }

    public void FillEllipse(Color color, int x, int y, int width, int height)
    {
        var ellipse = new EllipsePolygon(x, y, width, height);
        image.Mutate(x => x.Fill(color, ellipse));
    }

    public void FillPolygon(Color color, Point<float>[] points, bool? blendColors = true)
    {
        var polyPoints = points.Select(p => new PointF(p.X, p.Y)).ToArray();
        image.Mutate(x => x.FillPolygon(color, polyPoints));
    }

    public void FillRectangle(Color color, int x, int y, int width, int height)
    {
        var rectangle = new SixLabors.ImageSharp.Rectangle(x, y, width, height);
        image.Mutate(x => x.Fill(color, rectangle));
    }

    public void FillSmoothPolygon(Color color, Point<float>[] points)
    {
        var polyPoints = points.Select(p => new PointF(p.X, p.Y)).ToArray();
        image.Mutate(x => x.FillPolygon(new() { GraphicsOptions = new() { Antialias = true } }, color, polyPoints));
    }

    public Stream GetPngStream()
    {
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;
        return stream;
    }

    public void MakeColorTransparent(Color color)
    {
        var brush = new RecolorBrush(color, SixLabors.ImageSharp.Color.Transparent, 1);
        image.Mutate(x => x.Clear(brush));
    }

    public IImage ResizeTo(int width, int height)
    {
        var clonedImage = image.Clone();
        clonedImage.Mutate(x => x.Resize(width, height));
        return new SharpImage(clonedImage);
    }

    public void Rotate(BitmapRotation angle)
    {
        var mode = angle switch
        {
            BitmapRotation.Rotate0 => RotateMode.None,
            BitmapRotation.Rotate90 => RotateMode.Rotate90,
            BitmapRotation.Rotate180 => RotateMode.Rotate180,
            BitmapRotation.Rotate270 => RotateMode.Rotate270,
            _ => throw new NotImplementedException()
        };

        image.Mutate(x => x.Rotate(mode));
    }
}
