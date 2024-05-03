using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows;

namespace DigitalBattleMap.Utilities;

public static class CursorCreator
{
    public static Cursor Create(Brush brush, int size)
    {
        return Create(brush, new Pen(), size);
    }

    public static Cursor Create(Brush brush, Pen pen, int size)
    {
        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            var fillSize = (int)Math.Max(size - pen.Thickness * 2, 1);
            drawingContext.DrawEllipse(brush, pen, new Point(size / 2, size / 2), fillSize / 2, fillSize / 2);
            drawingContext.Close();
        }
        var renderTargetBitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(drawingVisual);

        return CreateCursor(renderTargetBitmap, size / 2, size / 2);
    }

    private static Cursor CreateCursor(BitmapSource bitmapSource, int hotspotX, int hotspotY)
    {
        using var pngMemoryStream = new MemoryStream();
        var pngEncoder = new PngBitmapEncoder();
        pngEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
        pngEncoder.Save(pngMemoryStream);

        var pngBytes = pngMemoryStream.ToArray();
        var size = pngBytes.GetLength(0);

        using (var memortyStream = new MemoryStream())
        {
            //Reserved must be zero; 2 bytes
            memortyStream.Write(BitConverter.GetBytes((short)0), 0, 2);

            //image type 1 = ico 2 = cur; 2 bytes
            memortyStream.Write(BitConverter.GetBytes((short)2), 0, 2);

            //number of images; 2 bytes
            memortyStream.Write(BitConverter.GetBytes((short)1), 0, 2);

            //image width in pixels
            memortyStream.WriteByte(32);

            //image height in pixels
            memortyStream.WriteByte(32);

            //Number of Colors in the color palette. Should be 0 if the image doesn't use a color palette
            memortyStream.WriteByte(0);

            //reserved must be 0
            memortyStream.WriteByte(0);

            //2 bytes. In CUR format: Specifies the horizontal coordinates of the hotspot in number of pixels from the left.
            memortyStream.Write(BitConverter.GetBytes((short)hotspotX), 0, 2);
            //2 bytes. In CUR format: Specifies the vertical coordinates of the hotspot in number of pixels from the top.
            memortyStream.Write(BitConverter.GetBytes((short)hotspotY), 0, 2);

            //Specifies the size of the image's data in bytes
            memortyStream.Write(BitConverter.GetBytes(size), 0, 4);

            //Specifies the offset of BMP or PNG data from the beginning of the ICO/CUR file
            memortyStream.Write(BitConverter.GetBytes(22), 0, 4);

            memortyStream.Write(pngBytes, 0, size); //write the png data.
            memortyStream.Seek(0, SeekOrigin.Begin);
            return new Cursor(memortyStream);
        }
    }
}
