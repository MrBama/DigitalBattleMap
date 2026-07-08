using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DigitalBattleMap.Imaging;

public interface IImage
{
    int Width { get; }

    int Height { get; }

    void DrawEllipse(Color color, int lineWidth, int x, int y, int width, int height);

    void FillEllipse(Color color, int x, int y, int width, int height);

    void FillRectangle(Color color, int x, int y, int width, int height);

    void FillPolygon(Color color, PointF[] points);

    Stream GetPngStream();
}

public static class ImageExtensions
{
    public static Bitmap ToDrawingBitmap(this IImage image)
    {
        return (Bitmap)Image.FromStream(image.GetPngStream());
    }
}