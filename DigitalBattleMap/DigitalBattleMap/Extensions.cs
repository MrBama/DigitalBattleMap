using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap
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

        public static Point<double> ToPointDouble(this Point<int> point)
        {
            return new Point<double>(point.X, point.Y);
        }

        public static Point<int> ToPointInt(this Point<double> point)
        {
            return new Point<int>((int)point.X, (int)point.Y);
        }

        public static Size<int> ToSizeInt(this Size<double> size)
        {
            return new Size<int>((int)size.Width, (int)size.Height);
        }

        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }
    }
}
