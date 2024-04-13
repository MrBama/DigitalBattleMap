using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using DigitalBattleMap.DataClasses;
using System.Windows.Media;
using System.Runtime.CompilerServices;

namespace DigitalBattleMap.Utilities;

public static class Extensions
{
    public static BitmapSource ToBitmapImage(this Bitmap bitmap)
    {
        // Do not use MemoryStream and BitmapImage because converting a bitmap to png (transparent background) is too slow
        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
        var bitmapSource = BitmapSource.Create(bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgra32, null, bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

        bitmap.UnlockBits(bitmapData);
        bitmapSource.Freeze(); // This is required when a dependency property (ImageSource) is create from a different thread. E.g. Token moves from the web server.
        return bitmapSource;
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

    public static void OrderCurrentBy<TSource, TKey>(this IList<TSource> source, Func<TSource, TKey> keySelector)
    {
        var orderedCollection = new ObservableCollection<TSource>(source.OrderBy(keySelector));
        source.Clear();
        foreach (var item in orderedCollection)
        {
            source.Add(item);
        }
    }

    public static void OrderCurrentByDescending<TSource, TKey>(this IList<TSource> source, Func<TSource, TKey> keySelector)
    {
        var orderedCollection = new ObservableCollection<TSource>(source.OrderByDescending(keySelector));
        source.Clear();
        foreach (var item in orderedCollection)
        {
            source.Add(item);
        }
    }

    public static IList<T> OrderByList<T>(this IList<T> source, IList<T> list)
    {
        var items = new List<Tuple<T, int>>();

        foreach (var item in source)
        {
            var index = list.IndexOf(item);
            items.Add(new(item, index));
        }

        return items.OrderBy(t => t.Item2).Select(i => i.Item1).ToList();
    }

    public static IList<T> OrderByDescendingList<T>(this IList<T> source, IList<T> list)
    {
        var items = new List<Tuple<T, int>>();

        foreach (var item in source)
        {
            var index = list.IndexOf(item);
            items.Add(new(item, index));
        }

        return items.OrderByDescending(t => t.Item2).Select(i => i.Item1).ToList();
    }

    public static byte[] ToPng(this Bitmap bitmap)
    {
        using MemoryStream stream = new();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
    {
        return listToClone.Select(item => (T)item.Clone()).ToList();
    }

    public static T Clone<T>(this T objectToClone) where T : ICloneable
    {
        return (T)objectToClone.Clone();
    }

    public static bool EqualsList<T>(this IEnumerable<T> lhs, IEnumerable<T> rhs) where T : IEquatable<T>
    {
        return lhs.All(rhs.Contains) && lhs.Count() == rhs.Count();
    }

    public static List<string> ToStringList(this IList<TokenIndentifier> tokenIndentifiers)
    {
        return tokenIndentifiers.Select(t => t.GetCombinedString()).ToList();
    }

    public static System.Drawing.Brush ToDrawingBrush(this System.Windows.Media.Color color)
    {
        return new SolidBrush(System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B));
    }
}
