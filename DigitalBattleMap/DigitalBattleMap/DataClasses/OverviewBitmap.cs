using DigitalBattleMap.Imaging;
using System;

namespace DigitalBattleMap.DataClasses;

public class OverviewBitmap
{
    public IImage Bitmap { get; set; }

    // This is the offset from top left of the player area (origin) to top left of the bitmap
    public Point<int> OffsetFromOrigin { get; set; }

    public void Resize(double zoomFactor)
    {
        var zoomedSize = new Size<int>(
            (int)Math.Round(Bitmap.Width * zoomFactor),
            (int)Math.Round(Bitmap.Height * zoomFactor));
        Bitmap = BitmapTools.ResizeBitmap(Bitmap, zoomedSize);
        OffsetFromOrigin = new Point<int>(
            (int)Math.Round(OffsetFromOrigin.X * zoomFactor),
            (int)Math.Round(OffsetFromOrigin.Y * zoomFactor));
    }
}
