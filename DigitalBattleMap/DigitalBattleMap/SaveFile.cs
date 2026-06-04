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
    private static object _lock = new();

    public int GridSize { get; set; }

    public bool IsGridShown { get; set; }

    public int GridCellsWidth { get; set; }

    public int GridCellsHeight { get; set; }

    public int BackgroundFeetPerGridCell { get; set; }

    public Size<double> CanvasSize { get; set; } = new();

    public Rectangle BackgroundArea { get; set; }

    public List<TokenListItem> TokenList { get; set; } = new();

    public List<DrawingShape> DrawingShapes { get; set; } = new();

    public List<ObjectLink> ObjectLinks { get; set; } = new();

    public List<FogShape> FogShapes { get; set; } = new();

    public bool IsFillFogEnabled { get; set; }

    [JsonIgnore]
    public Bitmap FullBackground { get; set; }

    [JsonIgnore]
    public Bitmap GMOverlay { get; set; }

    public void Save(string path)
    {
        lock (_lock)
        {
            using var tempDirectory = new TempDirectory();
            FileManager.SaveFile(this, GetSaveFilePath(tempDirectory.Path));

            FullBackground?.Copy().Save(GetFullBackgroundFilePath(tempDirectory.Path));
            GMOverlay?.Copy().Save(GetGMOverlayFilePath(tempDirectory.Path));

            for (int i = 0; i < TokenList.Count; i++)
            {
                var tokenImagePath = Path.Combine(tempDirectory.Path, $"Token{i}.png");
                TokenList[i].GetBitmap().Copy().Save(tokenImagePath);
                TokenList[i].Token.ImagePath = "";

                if (TokenList[i].Token.Statblock is MarkdownStatblock markdownStatblock)
                {
                    var markdownPath = Path.Combine(tempDirectory.Path, $"Markdown{i}.md");
                    IO.File.WriteAllText(markdownPath, markdownStatblock.GetMarkdown());
                    markdownStatblock.MarkdownPath = "";
                }
            }

            var pathWithExtension = Path.ChangeExtension(path, ".dbm");
            if (IO.File.Exists(pathWithExtension))
            {
                IO.File.Delete(pathWithExtension);
            }

            IO.ZipFile.CreateFromDirectory(tempDirectory.Path, pathWithExtension);
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
            using var tempDirectory = new TempDirectory();
            IO.ZipFile.ExtractToDirectory(path, tempDirectory.Path);

            if (!FileManager.OpenFile(GetSaveFilePath(tempDirectory.Path), out SaveFile saveFile, 
                new DerivedClassJsonConverter<Statblock>(), 
                new DerivedClassJsonConverter<DrawingShape>(),
                new DerivedClassJsonConverter<FogShape>()))
            {
                saveFile = new SaveFile();
            }

            if (IO.File.Exists(GetFullBackgroundFilePath(tempDirectory.Path)))
            {
                saveFile.FullBackground = IO.File.LoadBitmap(GetFullBackgroundFilePath(tempDirectory.Path));
            }

            if (IO.File.Exists(GetGMOverlayFilePath(tempDirectory.Path)))
            {
                saveFile.GMOverlay = IO.File.LoadBitmap(GetGMOverlayFilePath(tempDirectory.Path));
            }

            for (int i = 0; i < saveFile.TokenList.Count; i++)
            {
                var tokenImagePath = Path.Combine(tempDirectory.Path, $"Token{i}.png");
                saveFile.TokenList[i].Token.ImagePath = tokenImagePath;
                saveFile.TokenList[i].GetBitmap();

                if (saveFile.TokenList[i].Token.Statblock is MarkdownStatblock markdownStatblock)
                {
                    var markdownPath = Path.Combine(tempDirectory.Path, $"Markdown{i}.md");
                    markdownStatblock.MarkdownPath = markdownPath;
                    markdownStatblock.GetMarkdown();
                }
            }

            return saveFile;
        }
    }

    private static string GetSaveFilePath(string tempDirectoryPath)
    {
        return Path.Combine(tempDirectoryPath, "SaveFile.json");
    }

    private static string GetFullBackgroundFilePath(string tempDirectoryPath)
    {
        return Path.Combine(tempDirectoryPath, "FullBackground.png");
    }

    private static string GetGMOverlayFilePath(string tempDirectoryPath)
    {
        return Path.Combine(tempDirectoryPath, "GMOverlay.png");
    }
}
