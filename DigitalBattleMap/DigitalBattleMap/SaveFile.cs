using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Ink;

namespace DigitalBattleMap;

public class SaveFile
{
    private static string _tempDirectoryPath = Path.Combine(Constants.SettingsPath, "Temp");
    private static string _saveFilePath = Path.Combine(_tempDirectoryPath, "SaveFile.json");
    private static string _drawingFilePath = Path.Combine(_tempDirectoryPath, "Drawing.dat");
    private static string _fullBackgrondFilePath = Path.Combine(_tempDirectoryPath, "FullBackground.png");

    public int GridSize { get; set; }

    public bool IsGridShown { get; set; }

    public int GridCellsWidth { get; set; }

    public int GridCellsHeight { get; set; }

    public Size<double> CanvasSize { get; set; } = new();

    public Rectangle BackgroundArea { get; set; }

    public List<TokenListItem> TokenList { get; set; } = new();

    public List<DrawingShapeSave> DrawingShapes { get; set; } = new();

    public List<ObjectLink> ObjectLinks { get; set; } = new();

    [JsonIgnore]
    public StrokeCollection Strokes { get; set; } = new();

    [JsonIgnore]
    public Bitmap FullBackground { get; set; }

    public void Save(string path)
    {
        using var tempDirectory = new TempDirectory(_tempDirectoryPath);
        FileManager.SaveFile(this, _saveFilePath);

        using (var fileStream = new FileStream(_drawingFilePath, FileMode.Create))
        {
            Strokes.Save(fileStream);
        }

        FullBackground?.Save(_fullBackgrondFilePath);

        for (int i = 0; i < TokenList.Count; i++)
        {
            var tokenImagePath = Path.Combine(_tempDirectoryPath, $"Token{i}.png");
            TokenList[i].GetBitmap().Save(tokenImagePath);
            TokenList[i].Token.ImagePath = "";
        }

        var pathWithExtension = Path.ChangeExtension(path, ".dbm");
        if (IO.File.Exists(pathWithExtension))
        {
            IO.File.Delete(pathWithExtension);
        }

        IO.ZipFile.CreateFromDirectory(_tempDirectoryPath, pathWithExtension);
    }

    public static SaveFile Open(string path)
    {
        using var tempDirectory = new TempDirectory(_tempDirectoryPath);
        IO.ZipFile.ExtractToDirectory(path, _tempDirectoryPath);

        if (!FileManager.OpenFile(_saveFilePath, out SaveFile saveFile))
        {
            saveFile = new SaveFile();
        }

        using (var fileStream = new FileStream(_drawingFilePath, FileMode.Open))
        {
            saveFile.Strokes = new StrokeCollection(fileStream);
        }

        if (IO.File.Exists(_fullBackgrondFilePath))
        {
            saveFile.FullBackground = IO.File.LoadBitmap(_fullBackgrondFilePath);
        }

        for (int i = 0; i < saveFile.TokenList.Count; i++)
        {
            var tokenImagePath = Path.Combine(_tempDirectoryPath, $"Token{i}.png");
            saveFile.TokenList[i].Token.ImagePath = tokenImagePath;
            saveFile.TokenList[i].GetBitmap();
        }

        return saveFile;
    }

    private class TempDirectory : IDisposable
    {
        private string _path;

        public TempDirectory(string path)
        {
            _path = path;
            if (IO.Directory.Exists(_path))
            {
                IO.Directory.Delete(_path, true);
            }

            IO.Directory.CreateDirectory(_path);
        }

        public void Dispose()
        {
            if (IO.Directory.Exists(_path))
            {
                IO.Directory.Delete(_path, true);
            }
        }
    }
}
