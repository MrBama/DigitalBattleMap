using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DigitalBattleMap
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IWindowService _windowService = new WindowService();

        protected override void OnStartup(StartupEventArgs e)
        {
            MainWindow = new MainWindow(_windowService);
            MainWindow.Show();
        }
    }
}
