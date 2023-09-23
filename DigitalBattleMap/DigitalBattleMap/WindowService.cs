using DigitalBattleMap.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace DigitalBattleMap;

public class WindowService : IWindowService
{
    private List<Window> _windows = new();

    public void ShowWindow<T>(object dataContext) where T : Window, new()
    {
        var window = new T
        {
            DataContext = dataContext
        };
        _windows.Add(window);
        window.Show();
    }

    public void ShowWindowDialog<T>(object dataContext) where T : Window, new()
    {
        var window = new T
        {
            DataContext = dataContext
        };
        window.ShowDialog();
    }

    public bool ShowOpenFileDialog(out string path, string filter)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = filter
        };

        var result = dialog.ShowDialog();
        if (result.HasValue && result.Value)
        {
            path = dialog.FileName;
            return true;
        }
        else
        {
            path = default;
            return false;
        }
    }

    public bool ShowOpenFilesDialog(out List<string> paths, string filter)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = filter,
            Multiselect = true
        };

        var result = dialog.ShowDialog();
        if (result.HasValue && result.Value)
        {
            paths = dialog.FileNames.ToList();
            return true;
        }
        else
        {
            paths = default;
            return false;
        }
    }

    public bool ShowSaveFileDialog(out string path, string defaultFileName, string filter)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = filter,
            FileName = defaultFileName
        };

        var result = dialog.ShowDialog();
        if (result.HasValue && result.Value)
        {
            path = dialog.FileName;
            return true;
        }
        else
        {
            path = default;
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
