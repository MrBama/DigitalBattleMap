using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace DigitalBattleMap.Utility;

public static class BitmapExtensions
{
    private static readonly EncoderParameters EncoderParameters = new(1) { Param = new[] { new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 75L ) } };
    private static readonly ImageCodecInfo JpgEncoder = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid); 
    private static readonly ImageCodecInfo PngEncoder = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

    public static byte[] ToJpg(this Bitmap bitmap)
    {
        using MemoryStream stream = new ();
        bitmap.Save(stream, JpgEncoder, EncoderParameters);

        var data = stream.ToArray();
        return data;
    }

    public static byte[] ToPng(this Bitmap bitmap)
    {
        using MemoryStream stream = new();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}