using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Ink;

namespace DigitalBattleMap
{
    public class SaveFile
    {
        private static string _tempDirectoryPath = Path.Combine(Constants.SettingsPath, "Temp");
        private static string _saveFilePath = Path.Combine(_tempDirectoryPath, "SaveFile.json");
        private static string _drawingFilePath = Path.Combine(_tempDirectoryPath, "Drawing.dat");
        private static string _fullBackgrondFilePath = Path.Combine(_tempDirectoryPath, "FullBackground.png");

        public int GridSize { get; set; }

        public bool IsGridShown { get; set; }

        public Rectangle BackgroundArea { get; set; }

        [JsonIgnore]
        public StrokeCollection Strokes { get; set; } = new StrokeCollection();

        [JsonIgnore]
        public Bitmap FullBackground { get; set; }

        public void Save(string path)
        {
            CreateTempDirectory();
            FileManager.SaveFile(this, _saveFilePath);

            using (var fileStream = new FileStream(_drawingFilePath, FileMode.Create))
            {
                Strokes.Save(fileStream);
            }

            if(FullBackground != null)
            {
                FullBackground.Save(_fullBackgrondFilePath);
            }

            var pathWithExtension = Path.ChangeExtension(path, ".dbm");
            if (File.Exists(pathWithExtension))
            {
                File.Delete(pathWithExtension);
            }

            ZipFile.CreateFromDirectory(_tempDirectoryPath, pathWithExtension);
        }

        public static SaveFile Open(string path)
        {
            CreateTempDirectory();
            ZipFile.ExtractToDirectory(path, _tempDirectoryPath);

            SaveFile saveFile;

            if (!FileManager.OpenFile(_saveFilePath, out saveFile))
            {
                saveFile = new SaveFile();
            }

            using (var fileStream = new FileStream(_drawingFilePath, FileMode.Open))
            {
                saveFile.Strokes = new StrokeCollection(fileStream);
            }

            if (File.Exists(_fullBackgrondFilePath))
            {
                // Load bitmap and make a copy
                saveFile.FullBackground = new Bitmap(new Bitmap(_fullBackgrondFilePath));
            }

            return saveFile;
        }

        private static void CreateTempDirectory()
        {
            if (Directory.Exists(_tempDirectoryPath))
            {
                Directory.Delete(_tempDirectoryPath, true);
            }

            Directory.CreateDirectory(_tempDirectoryPath);
        }
    }
}
