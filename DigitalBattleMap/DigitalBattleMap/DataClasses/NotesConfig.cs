using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public class NotesConfig
{
    public List<string> FolderPaths { get; set; } = new();
    public string SelectedFilePath { get; set; }
}
