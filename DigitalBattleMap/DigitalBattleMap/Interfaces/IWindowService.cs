using System.Collections.Generic;
using System.Windows;

namespace DigitalBattleMap.Interfaces;

public interface IWindowService
{
    public void ShowWindow<T>(object dataContext) where T : Window, new();
    public void ShowWindow(object dataContext);
    public void HideWindow(object dataContext);
    public void ShowWindowDialog<T>(object dataContext) where T : Window, new();
    public bool ShowOpenFileDialog(out string path, string filter = "All files (*.*)|*.*");
    public bool ShowOpenFilesDialog(out List<string> paths, string filter = "All files (*.*)|*.*");
    public bool ShowSaveFileDialog(out string path, string defaultFileName = "", string filter = "All files (*.*)|*.*");
    public void CloseAllWindows();
}
