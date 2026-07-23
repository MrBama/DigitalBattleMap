using DigitalBattleMap.DataClasses;
using System.IO;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.Imaging;

public interface IImage
{
    int Width { get; }

    int Height { get; }

    void Clear(Color color);

    void DrawEllipse(Color color, int lineWidth, Rectangle rectangle);

    void DrawRectangle(Color color, int lineWidth, Rectangle rectangle);

    void FillEllipse(Color color, Rectangle rectangle);

    void FillRectangle(Color color, Rectangle rectangle);

    void FillPolygon(Color color, Point<float>[] points, bool? blendColors = true);

    void FillSmoothPolygon(Color color, Point<float>[] points);

    void DrawLine(Color color, int lineSize, int x1, int y1, int x2, int y2);

    IImage CropTo(Rectangle rectangle);

    IImage ResizeTo(int width, int height);

    void DrawImage(IImage image, int x, int y);

    void DrawImage(IImage image, Rectangle target);

    void Rotate(BitmapRotation angle);

    void MakeColorTransparent(Color color);

    IImage Clone();

    Stream GetPngStream();

    byte[] Serialize();

    BitmapSource ToBitmapSource();
}
