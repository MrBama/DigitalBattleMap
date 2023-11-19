using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Ink;
using static DigitalBattleMap.Utilities.FileManager;

namespace DigitalBattleMap;

public class SaveFile
{
    private static string _saveFilePath = Path.Combine(Constants.TempDirectoryPath, "SaveFile.json");
    private static string _drawingFilePath = Path.Combine(Constants.TempDirectoryPath, "Drawing.dat");
    private static string _fullBackgrondFilePath = Path.Combine(Constants.TempDirectoryPath, "FullBackground.png");

    public int GridSize { get; set; }

    public bool IsGridShown { get; set; }

    public bool IsFogOfWarEnabled { get; set; }

    public int GridCellsWidth { get; set; }

    public int GridCellsHeight { get; set; }

    public int BackgroundFeetPerGridCell { get; set; }

    public Size<double> CanvasSize { get; set; } = new();

    public Rectangle BackgroundArea { get; set; }

    public Campaign Campaign { get; set; } = new();

    public List<TokenListItem> TokenList { get; set; } = new();

    public List<DrawingShapeSave> DrawingShapes { get; set; } = new();

    public List<ObjectLink> ObjectLinks { get; set; } = new();

    public List<FogOfWarArea> FogOfWarAreas { get; set; } = new();

    [JsonIgnore]
    public StrokeCollection Strokes { get; set; } = new();

    [JsonIgnore]
    public Bitmap FullBackground { get; set; }

    public void Save(string path)
    {
        using var tempDirectory = new TempDirectory(Constants.TempDirectoryPath);
        FileManager.SaveFile(this, _saveFilePath);

        using (var fileStream = new FileStream(_drawingFilePath, FileMode.Create))
        {
            Strokes.Save(fileStream);
        }

        FullBackground?.Save(_fullBackgrondFilePath);

        for (int i = 0; i < TokenList.Count; i++)
        {
            var tokenImagePath = Path.Combine(Constants.TempDirectoryPath, $"Token{i}.png");
            TokenList[i].GetBitmap().Save(tokenImagePath);
            TokenList[i].Token.ImagePath = "";

            if (TokenList[i].Token.Statblock is MarkdownStatblock markdownStatblock)
            {
                var markdownPath = Path.Combine(Constants.TempDirectoryPath, $"Markdown{i}.md");
                IO.File.WriteAllText(markdownPath, markdownStatblock.GetMarkdown());
                markdownStatblock.MarkdownPath = "";
            }
        }

        var pathWithExtension = Path.ChangeExtension(path, ".dbm");
        if (IO.File.Exists(pathWithExtension))
        {
            IO.File.Delete(pathWithExtension);
        }

        IO.ZipFile.CreateFromDirectory(Constants.TempDirectoryPath, pathWithExtension);
    }

    public static SaveFile Open(string path)
    {
        using var tempDirectory = new TempDirectory(Constants.TempDirectoryPath);
        IO.ZipFile.ExtractToDirectory(path, Constants.TempDirectoryPath);

        if (!FileManager.OpenFile(_saveFilePath, new DerivedClassJsonConverter<Statblock>(), out SaveFile saveFile))
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
            var tokenImagePath = Path.Combine(Constants.TempDirectoryPath, $"Token{i}.png");
            saveFile.TokenList[i].Token.ImagePath = tokenImagePath;
            saveFile.TokenList[i].GetBitmap();

            if (saveFile.TokenList[i].Token.Statblock is MarkdownStatblock markdownStatblock)
            {
                var markdownPath = Path.Combine(Constants.TempDirectoryPath, $"Markdown{i}.md");
                markdownStatblock.MarkdownPath = markdownPath;
                markdownStatblock.GetMarkdown();
            }
        }

        return saveFile;
    }
}
