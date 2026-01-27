using DigitalBattleMap.DataClasses;
using DigitalBattleMap.DrawingShapes;
using DigitalBattleMap.FogShapes;
using DigitalBattleMap.Utilities;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using static DigitalBattleMap.Utilities.FileManager;

namespace DigitalBattleMap;

public class SaveFile
{
    private static string _saveFilePath = Path.Combine(Constants.TempSaveFileDirectoryPath, "SaveFile.json");
    private static string _fullBackgrondFilePath = Path.Combine(Constants.TempSaveFileDirectoryPath, "FullBackground.png");
    private static string _gmOverlayFilePath = Path.Combine(Constants.TempSaveFileDirectoryPath, "GMOverlay.png");
    private static object _lock = new();

    public int GridSize { get; set; }

    public bool IsGridShown { get; set; }

    public bool IsFogOfWarEnabled { get; set; }

    public int GridCellsWidth { get; set; }

    public int GridCellsHeight { get; set; }

    public int BackgroundFeetPerGridCell { get; set; }

    public Size<double> CanvasSize { get; set; } = new();

    public Rectangle BackgroundArea { get; set; }

    public List<TokenListItem> TokenList { get; set; } = new();

    public List<DrawingShape> DrawingShapes { get; set; } = new();

    public List<ObjectLink> ObjectLinks { get; set; } = new();

    public List<SelectedArea> FogOfWarAreas { get; set; } = new();

    [JsonIgnore]
    public Bitmap FullBackground { get; set; }

    [JsonIgnore]
    public Bitmap GMOverlay { get; set; }

    public void Save(string path)
    {
        lock (_lock)
        {
            using var tempDirectory = new TempDirectory(Constants.TempSaveFileDirectoryPath);
            FileManager.SaveFile(this, _saveFilePath);

            FullBackground?.Copy().Save(_fullBackgrondFilePath);
            GMOverlay?.Copy().Save(_gmOverlayFilePath);

            for (int i = 0; i < TokenList.Count; i++)
            {
                var tokenImagePath = Path.Combine(Constants.TempSaveFileDirectoryPath, $"Token{i}.png");
                TokenList[i].GetBitmap().Copy().Save(tokenImagePath);
                TokenList[i].Token.ImagePath = "";

                if (TokenList[i].Token.Statblock is MarkdownStatblock markdownStatblock)
                {
                    var markdownPath = Path.Combine(Constants.TempSaveFileDirectoryPath, $"Markdown{i}.md");
                    IO.File.WriteAllText(markdownPath, markdownStatblock.GetMarkdown());
                    markdownStatblock.MarkdownPath = "";
                }
            }

            var pathWithExtension = Path.ChangeExtension(path, ".dbm");
            if (IO.File.Exists(pathWithExtension))
            {
                IO.File.Delete(pathWithExtension);
            }

            IO.ZipFile.CreateFromDirectory(Constants.TempSaveFileDirectoryPath, pathWithExtension);
        }
    }

    public void AutoSave()
    {
        var files = IO.Directory.GetFiles(Constants.AutoSavesPath);
        if(files.Length > Constants.MaxAutoSaves)
        {
            var filesOrderedByDate = files.OrderBy(f => IO.File.GetCreationTime(f));

            for (int i = 0; i < filesOrderedByDate.Count() - Constants.MaxAutoSaves + 1; i++)
            {
                IO.File.Delete(filesOrderedByDate.ElementAt(i));
            }
        }

        var path = Path.Combine(Constants.AutoSavesPath, $"AutoSave_{System.DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}.dbm");
        Save(path);
    }

    public static SaveFile Open(string path)
    {
        lock (_lock)
        {
            using var tempDirectory = new TempDirectory(Constants.TempSaveFileDirectoryPath);
            IO.ZipFile.ExtractToDirectory(path, Constants.TempSaveFileDirectoryPath);

            if (!FileManager.OpenFile(_saveFilePath, out SaveFile saveFile, new DerivedClassJsonConverter<Statblock>(), new DerivedClassJsonConverter<DrawingShape>()))
            {
                saveFile = new SaveFile();
            }

            if (IO.File.Exists(_fullBackgrondFilePath))
            {
                saveFile.FullBackground = IO.File.LoadBitmap(_fullBackgrondFilePath);
            }

            if (IO.File.Exists(_gmOverlayFilePath))
            {
                saveFile.GMOverlay = IO.File.LoadBitmap(_gmOverlayFilePath);
            }

            for (int i = 0; i < saveFile.TokenList.Count; i++)
            {
                var tokenImagePath = Path.Combine(Constants.TempSaveFileDirectoryPath, $"Token{i}.png");
                saveFile.TokenList[i].Token.ImagePath = tokenImagePath;
                saveFile.TokenList[i].GetBitmap();

                if (saveFile.TokenList[i].Token.Statblock is MarkdownStatblock markdownStatblock)
                {
                    var markdownPath = Path.Combine(Constants.TempSaveFileDirectoryPath, $"Markdown{i}.md");
                    markdownStatblock.MarkdownPath = markdownPath;
                    markdownStatblock.GetMarkdown();
                }
            }

            return saveFile;
        }
    }
}
