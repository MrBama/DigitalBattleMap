using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace DigitalBattleMap
{
    public interface IWindowService
    {
        public void ShowWindow<T>(object dataContext) where T : Window, new();

        public void ShowWindowDialog<T>(object dataContext) where T : Window, new();

        public bool ShowFileDialog(out string path);

        public void CloseAllWindows();
    }

    public class WindowService : IWindowService
    {
        private List<Window> _windows = new List<Window>();

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

        public bool ShowFileDialog(out string path)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
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
}
