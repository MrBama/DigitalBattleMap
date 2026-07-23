
using System.IO;

namespace DigitalBattleMap.Imaging;

internal class ImageFactory
{
    public static IImage Create(int width, int height, Color? baseColor = null)
    {
#if GDI_IMAGE_PROCESSOR
        var image = new GDIImage(width, height);
#elif IMAGE_SHARP_IMAGE_PROCESSOR
        var image = new SharpImage(width, height);
#elif SKIA_IMAGE_PROCESSOR
        var image = new SkiaImage(width, height);
#endif

        if (baseColor != null)
        {
            image.Clear(baseColor.Value);
        }

        return image;
    }

    public static IImage FromDrawingBitmap(System.Drawing.Bitmap bitmap)
    {
#if GDI_IMAGE_PROCESSOR
        return new GDIImage(bitmap);
#else
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
#endif

#if IMAGE_SHARP_IMAGE_PROCESSOR
        return SharpImage.LoadFrom(stream);
#elif SKIA_IMAGE_PROCESSOR
        return SkiaImage.LoadFrom(stream);
#endif
    }
}
