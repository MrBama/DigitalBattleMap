using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap.Imaging;

internal class ImageFactory
{
    private static readonly ImageProcessor IMAGE_PROCESSOR = ImageProcessor.Skia;

    public static IImage Create(int width, int height, Color? baseColor = null)
    {
        IImage image;
        if (IMAGE_PROCESSOR == ImageProcessor.GDI)
        {
            image = new GDIImage(width, height);
        }
        else if (IMAGE_PROCESSOR == ImageProcessor.ImageSharp)
        {
            image = new SharpImage(width, height);
        }
        else
        {
            image = new SkiaImage(width, height);
        }

        if (baseColor != null)
        {
            image.Clear(baseColor.Value);
        }

        return image;
    }

    public static IImage FromDrawingBitmap(System.Drawing.Bitmap bitmap)
    {
        if (IMAGE_PROCESSOR == ImageProcessor.GDI)
        {
            return new GDIImage(bitmap);
        }
        else if (IMAGE_PROCESSOR == ImageProcessor.ImageSharp)
        {
            var image = new GDIImage(bitmap);
            return new SharpImage(image.ToSharpImage());
        }
        else
        {
            var image = new GDIImage(bitmap);
            return new SkiaImage(image.ToSkiaImage());
        }
    }
}

internal enum ImageProcessor
{
    GDI,
    Skia,
    ImageSharp,
}
