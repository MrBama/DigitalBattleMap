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
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DigitalBattleMap", "Logs", $"{log.DateTime} Exception.log");
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
    }
}
