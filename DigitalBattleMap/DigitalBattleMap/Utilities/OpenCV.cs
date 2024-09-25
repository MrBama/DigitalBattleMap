using DigitalBattleMap.DataClasses;
using OpenCvSharp.Extensions;
using System.Drawing;

namespace DigitalBattleMap.Utilities;

public static class OpenCV
{
    public static Bitmap Resize(Bitmap bitmap, Size<int> size)
    {
        var mat = BitmapConverter.ToMat(bitmap);
        var resizedMat = mat.Resize(new OpenCvSharp.Size(size.Width, size.Height));
        return BitmapConverter.ToBitmap(resizedMat);
    }
}
