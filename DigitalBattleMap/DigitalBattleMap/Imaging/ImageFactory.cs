using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap.Imaging;
internal class ImageFactory
{
    public static IImage Create(int width, int height, Color? baseColor = null)
    {
        var image = new GDIImage(width, height);
        if (baseColor != null)
        {
            image.Clear(baseColor.Value);
        }

        return image;
    }
}
