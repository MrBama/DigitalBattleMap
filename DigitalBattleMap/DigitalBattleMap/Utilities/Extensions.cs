using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using DigitalBattleMap.DataClasses;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Windows.Documents;

namespace DigitalBattleMap.Utilities;

public static class Extensions
{
    public static BitmapImage ToBitmapImage(this Bitmap bitmap)
    {
        using var memory = new MemoryStream();
        bitmap.Save(memory, ImageFormat.Png);
        memory.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memory;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }

    public static void Log(this Exception exception)
    {
        var log = new ExceptionLog(exception);
        string path = Path.Combine(Constants.SettingsPath, "Logs", $"{log.DateTime} Exception.log");
        FileManager.SaveFile(log, path);
    }

    public static T Map<T>(this T input, T inMin, T inMax, T outMin, T outMax) where T : struct
    {
        dynamic inputD = input;
        dynamic inMinD = inMin;
        dynamic inMaxD = inMax;
        dynamic outMinD = outMin;
        dynamic outMaxD = outMax;

        return (inputD - inMinD) * (outMaxD - outMinD) / (inMaxD - inMinD) + outMinD;
    }

    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
    {
        return source.Select((item, index) => (item, index));
    }

    public static void OrderCurrentBy<TSource, TKey>(this ObservableCollection<TSource> source, Func<TSource, TKey> keySelector)
    {
        var orderedCollection = new ObservableCollection<TSource>(source.OrderBy(keySelector));
        source.Clear();
        foreach (var item in orderedCollection)
        {
            source.Add(item);
        }
    }

    public static void OrderCurrentByDescending<TSource, TKey>(this ObservableCollection<TSource> source, Func<TSource, TKey> keySelector)
    {
        var orderedCollection = new ObservableCollection<TSource>(source.OrderByDescending(keySelector));
        source.Clear();
        foreach (var item in orderedCollection)
        {
            source.Add(item);
        }
    }

    public static byte[] ToPng(this Bitmap bitmap)
    {
        using MemoryStream stream = new();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    public static Size<int> GetSize(this Rectangle rectangle)
    {
        return new Size<int>(rectangle.Width, rectangle.Height);
    }

    public static Rectangle GetRectangle<T>(this Size<T> size) where T : IEquatable<T>
    {
        dynamic widthD = size.Width;
        dynamic heightD = size.Height;
        return new Rectangle(0, 0, (int)widthD, (int)heightD);
    }

    public static Rectangle Map(this Rectangle rectangle, Rectangle inRectangle, Rectangle outRectangle)
    {
        var x = ((double)rectangle.X).Map(inRectangle.X, inRectangle.X + inRectangle.Width, outRectangle.X, outRectangle.X + outRectangle.Width);
        var y = ((double)rectangle.Y).Map(inRectangle.Y, inRectangle.Y + inRectangle.Height, outRectangle.Y, outRectangle.Y + outRectangle.Height);
        var width = ((double)rectangle.Width).Map(0, inRectangle.Width, 0, outRectangle.Width);
        var height = ((double)rectangle.Height).Map(0, inRectangle.Height, 0, outRectangle.Height);

        return new Rectangle((int)Math.Round(x), (int)Math.Round(y), (int)Math.Round(width), (int)Math.Round(height));
    }
}
