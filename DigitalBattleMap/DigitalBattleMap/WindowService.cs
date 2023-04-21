using System.Collections.Generic;
using System.Windows;

namespace DigitalBattleMap;

public interface IWindowService
{
    public void ShowWindow<T>(object dataContext) where T : Window, new();

    public void ShowWindowDialog<T>(object dataContext) where T : Window, new();

    public bool ShowOpenFileDialog(out string path, string filter = "All files (*.*)|*.*");

    public bool ShowSaveFileDialog(out string path, string filter = "All files (*.*)|*.*");

    public void CloseAllWindows();
}

public class WindowService : IWindowService
{
    private List<Window> _windows = new();

    public void ShowWindow<T>(object dataContext) where T : Window, new()
    {
        var window = new T();
        window.DataContext = dataContext;
        _windows.Add(window);
        window.Show();
    }

    public void ShowWindowDialog<T>(object dataContext) where T : Window, new()
    {
        var window = new T();
        window.DataContext = dataContext;
        window.ShowDialog();
    }

    public bool ShowOpenFileDialog(out string path, string filter)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog();
        dialog.Filter = filter;

        var result = dialog.ShowDialog();
        if (result.HasValue && result.Value)
        {
            path = dialog.FileName;
            return true;
        }
        else
        {
            path = "";
            return false;
        }
    }

    public bool ShowSaveFileDialog(out string path, string filter)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog();
        dialog.Filter = filter;

        var result = dialog.ShowDialog();
        if (result.HasValue && result.Value)
        {
            path = dialog.FileName;
            return true;
        }
        else
        {
            path = "";
            return false;
        }
    }

    public void CloseAllWindows()
    {
        foreach (var window in _windows)
        {
            window.Close();
        }
    }
}
