using DigitalBattleMap.Imaging;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Media.Imaging;

#if IMAGE_SHARP_IMAGE_PROCESSOR
using SixLabors.ImageSharp.PixelFormats;
#elif SKIA_IMAGE_PROCESSOR
using SkiaSharp;
#endif

public static class ImageExtensions
{
    public static BitmapSource ToBitmapImage(this IImage image)
    {
        return image.ToBitmapSource();
    }

    public static Bitmap ToDrawingBitmap(this IImage image)
    {
#if GDI_IMAGE_PROCESSOR
        if (image is GDIImage gdi)
        {
            return gdi.GetBitmap();
        }
#endif
        return (Bitmap)Image.FromStream(image.GetPngStream());
    }

#if IMAGE_SHARP_IMAGE_PROCESSOR
    public static SixLabors.ImageSharp.Image<Rgba32> ToSharpImage(this IImage image)
    {
        if (image is SharpImage sharp)
        {
            return sharp.GetSharpImage();
        }

        return SixLabors.ImageSharp.Image.Load<Rgba32>(image.GetPngStream());
    }
#endif

#if SKIA_IMAGE_PROCESSOR
    public static SKBitmap ToSkiaImage(this IImage image)
    {
        if (image is SkiaImage skia)
        {
            return skia.GetSkBitmap();
        }

        return SKBitmap.Decode(image.GetPngStream());
    }
#endif
}
