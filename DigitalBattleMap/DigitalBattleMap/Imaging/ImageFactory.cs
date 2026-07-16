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
    private static readonly bool USE_GDI_IMAGES = false;

    public static IImage Create(int width, int height, Color? baseColor = null)
    {
        IImage image;
        if (USE_GDI_IMAGES)
        {
            image = new GDIImage(width, height);
        }
        else
        {
            image = new SharpImage(width, height);
        }

        if (baseColor != null)
        {
            image.Clear(baseColor.Value);
        }

        return image;
    }

    public static IImage FromDrawingBitmap(System.Drawing.Bitmap bitmap)
    {
        if (USE_GDI_IMAGES)
        {
            return new GDIImage(bitmap);
        }
        else
        {
            var image = new GDIImage(bitmap);
            return new SharpImage(image.ToSharpImage());
        }
    }
}
