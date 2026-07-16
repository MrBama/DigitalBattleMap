using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.Imaging;

public interface IImage
{
    int Width { get; }

    int Height { get; }

    void Clear(Color color);

    void DrawEllipse(Color color, int lineWidth, Rectangle rectangle);

    void DrawRectangle(Color color, int lineWidth, int x, int y, int width, int height);

    void FillEllipse(Color color, int x, int y, int width, int height);

    void FillRectangle(Color color, int x, int y, int width, int height);

    void FillPolygon(Color color, Point<float>[] points, bool? blendColors = true);

    void FillSmoothPolygon(Color color, Point<float>[] points);

    void DrawLine(Color color, int lineSize, int x1, int y1, int x2, int y2);

    IImage CropTo(int x, int y, int width, int height);

    IImage ResizeTo(int width, int height);

    void DrawImage(IImage image, int x, int y, int? width = null, int? height = null);

    void Rotate(BitmapRotation angle);

    void MakeColorTransparent(Color color);

    IImage Clone();

    Stream GetPngStream();

}

public static class ImageExtensions
{
    public static BitmapSource ToBitmapImage(this IImage image)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if (image is GDIImage gdi)
            {
                return gdi.ToDrawingBitmap().ToBitmapImage();
            }
            else if (image is SharpImage sharp)
            {
                return sharp.ToBitmapSource();
            }
            else
            {
                return null;
            }
        }
        finally
        {
            Debug.WriteLine($"ToBitmapImage(): {sw.Elapsed.TotalMilliseconds} ms");
        }
    }

    public static Bitmap ToDrawingBitmap(this IImage image)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if (image is GDIImage gdi)
            {
                return gdi.GetBitmap();
            }
            return (Bitmap)Image.FromStream(image.GetPngStream());
        }
        finally {
            Debug.WriteLine(new StackTrace());
            Debug.WriteLine($"ToDrawingBitmap(): {sw.Elapsed.TotalMilliseconds} ms"); }
    }

    public static SixLabors.ImageSharp.Image<Rgba32> ToSharpImage(this IImage image)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if( image is SharpImage sharp)
            {
                return sharp.GetSharpImage();
            }
            return (SixLabors.ImageSharp.Image<Rgba32>)SixLabors.ImageSharp.Image.Load(image.GetPngStream());
        }
        finally { Debug.WriteLine($"ToSharpImage(): {sw.Elapsed.TotalMilliseconds} ms"); }
    }
}
