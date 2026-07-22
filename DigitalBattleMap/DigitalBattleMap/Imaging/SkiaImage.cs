
using DigitalBattleMap.DataClasses;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.Imaging;

internal class SkiaImage : IImage
{
    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear);

    private SKBitmap bitmap;

    public SkiaImage(int width, int height)
    {
        bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
    }

    public SkiaImage(SKBitmap bitmap)
    {
        this.bitmap = bitmap;
    }

    public int Width => bitmap.Width;

    public int Height => bitmap.Height;

    public void Clear(Color color)
    {
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
    }

    public IImage Clone()
    {
        return new SkiaImage(bitmap.Copy());
    }

    public IImage CropTo(Rectangle rectangle)
    {
        var cropped = new SKBitmap(rectangle.Width, rectangle.Height);

        using var canvas = new SKCanvas(cropped);

        canvas.DrawBitmap(
            bitmap,
            new SKRect(
                rectangle.TopLeftX,
                rectangle.TopLeftY,
                rectangle.TopLeftX + rectangle.Width,
                rectangle.TopLeftY + rectangle.Height),
            new SKRect(
                0,
                0,
                rectangle.Width,
                rectangle.Height),
            Sampling);

        return new SkiaImage(cropped);
    }

    public void DrawEllipse(Color color, int lineWidth, Rectangle rectangle)
    {
        using var canvas = new SKCanvas(bitmap);

        using var paint = new SKPaint
        {
            Color = color,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = lineWidth,
            IsAntialias = true
        };

        canvas.DrawOval(
            rectangle.CenterX,
            rectangle.CenterY,
            rectangle.Width / 2f,
            rectangle.Height / 2f,
            paint);
    }

    public void DrawLine(Color color, int lineWidth, int x1, int y1, int x2, int y2)
    {
        using var canvas = new SKCanvas(bitmap);

        using var paint = new SKPaint
        {
            Color = color,
            StrokeWidth = lineWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        canvas.DrawLine(x1, y1, x2, y2, paint);
    }

    public void DrawRectangle(Color color, int lineWidth, Rectangle rectangle)
    {
        using var canvas = new SKCanvas(bitmap);

        using var paint = new SKPaint
        {
            Color = color,
            StrokeWidth = lineWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        canvas.DrawRect(
            rectangle.TopLeftX,
            rectangle.TopLeftY,
            rectangle.Width,
            rectangle.Height,
            paint);
    }

    public void FillRectangle(Color color, Rectangle rectangle)
    {
        using var canvas = new SKCanvas(bitmap);

        using var paint = new SKPaint
        {
            Color = color,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawRect(
            rectangle.TopLeftX,
            rectangle.TopLeftY,
            rectangle.Width,
            rectangle.Height,
            paint);
    }

    public void FillEllipse(Color color, Rectangle rectangle)
    {
        using var canvas = new SKCanvas(bitmap);

        using var paint = new SKPaint
        {
            Color = color,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        canvas.DrawOval(
            rectangle.CenterX,
            rectangle.CenterY,
            rectangle.Width / 2f,
            rectangle.Height / 2f,
            paint);
    }

    public void FillPolygon(Color color, Point<float>[] points, bool? blendColors = true)
    {
        if (points.Length == 0)
        {
            return;
        }

        using var canvas = new SKCanvas(bitmap);

        using var paint = new SKPaint
        {
            Color = color,
            Style = SKPaintStyle.Fill,
            IsAntialias = blendColors ?? true
        };

        var builder = new SKPathBuilder();

        builder.MoveTo(points[0].X, points[0].Y);

        foreach (var point in points.Skip(1))
        {
            builder.LineTo(point.X, point.Y);
        }

        builder.Close();

        using var path = builder.Detach();

        canvas.DrawPath(path, paint);
    }

    public void FillSmoothPolygon(Color color, Point<float>[] points)
    {
        FillPolygon(color, points, true);
    }

    public void DrawImage(IImage image, int x, int y)
    {
        DrawImage(
            image,
            Rectangle.FromTopLeft(0, 0, image.Width, image.Height),
            Rectangle.FromTopLeft(x, y, image.Width, image.Height));
    }

    public void DrawImage(IImage image, Rectangle target)
    {
        DrawImage(
            image,
            Rectangle.FromTopLeft(0, 0, image.Width, image.Height),
            target);
    }

    private void DrawImage(IImage image, Rectangle source, Rectangle target)
    {
        var sourceBitmap = image.ToSkiaImage();

        using var canvas = new SKCanvas(bitmap);

        canvas.DrawBitmap(
            sourceBitmap,
            new SKRect(
                source.TopLeftX,
                source.TopLeftY,
                source.TopLeftX + source.Width,
                source.TopLeftY + source.Height),
            new SKRect(
                target.TopLeftX,
                target.TopLeftY,
                target.TopLeftX + target.Width,
                target.TopLeftY + target.Height),
            Sampling);
    }

    public void MakeColorTransparent(Color color)
    {
        var target = color;

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) == target)
                {
                    bitmap.SetPixel(x, y, SKColors.Transparent);
                }
            }
        }
    }

    public IImage ResizeTo(int width, int height)
    {
        var resized = bitmap.Resize(
            new SKImageInfo(width, height),
            Sampling);

        return new SkiaImage(resized);
    }

    public void Rotate(BitmapRotation angle)
    {
        SKBitmap rotated;

        switch (angle)
        {
            case BitmapRotation.Rotate0:
                return;

            case BitmapRotation.Rotate90:
                {
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);

                    using var canvas = new SKCanvas(rotated);

                    canvas.Translate(rotated.Width, 0);
                    canvas.RotateDegrees(90);
                    canvas.DrawBitmap(bitmap, 0, 0, Sampling);

                    break;
                }

            case BitmapRotation.Rotate180:
                {
                    rotated = new SKBitmap(bitmap.Width, bitmap.Height);

                    using var canvas = new SKCanvas(rotated);

                    canvas.Translate(rotated.Width, rotated.Height);
                    canvas.RotateDegrees(180);
                    canvas.DrawBitmap(bitmap, 0, 0, Sampling);

                    break;
                }

            case BitmapRotation.Rotate270:
                {
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);

                    using var canvas = new SKCanvas(rotated);

                    canvas.Translate(0, rotated.Height);
                    canvas.RotateDegrees(270);
                    canvas.DrawBitmap(bitmap, 0, 0, Sampling);

                    break;
                }

            default:
                throw new NotImplementedException();
        }

        bitmap = rotated;
    }

    public Stream GetPngStream()
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        var stream = new MemoryStream(data.ToArray());
        stream.Position = 0;
        return stream;
    }

    public byte[] Serialize()
    {
        var sw = Stopwatch.StartNew();

        try
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Webp, 0);

            return data.ToArray();
        }
        finally
        {
            Debug.WriteLine($"Serializing image: {sw.ElapsedMilliseconds} ms");
        }
    }

    public SKBitmap GetSkBitmap()
    {
        return bitmap;
    }

    public SkiaImage ToSkiaImage()
    {
        return this;
    }

    public BitmapSource ToBitmapSource()
    {
        int stride = bitmap.Width * 4;

        var bitmapSource = BitmapSource.Create(
            bitmap.Width,
            bitmap.Height,
            96,
            96,
            System.Windows.Media.PixelFormats.Bgra32,
            null,
            bitmap.GetPixels(),
            bitmap.ByteCount,
            stride);

        bitmapSource.Freeze();

        return bitmapSource;
    }
}
