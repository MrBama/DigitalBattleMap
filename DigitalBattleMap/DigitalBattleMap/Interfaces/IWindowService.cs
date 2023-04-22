using System.Windows;

namespace DigitalBattleMap.Interfaces;

public interface IWindowService
{
    public void ShowWindow<T>(object dataContext) where T : Window, new();
    public void ShowWindowDialog<T>(object dataContext) where T : Window, new();
    public bool ShowOpenFileDialog(out string path, string filter = "All files (*.*)|*.*");
    public bool ShowSaveFileDialog(out string path, string filter = "All files (*.*)|*.*");
    public void CloseAllWindows();
}
