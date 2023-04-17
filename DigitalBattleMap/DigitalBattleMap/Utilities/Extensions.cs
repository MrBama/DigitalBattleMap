using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using DigitalBattleMap.DataClasses;

namespace DigitalBattleMap.Utilities
{
    public static class Extensions
    {
        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
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
    }
}
