using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System.Windows;

namespace DigitalBattleMap
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IWindowService _windowService = new WindowService();

        public App()
        {
            DigitalBattleMap.Startup.CheckForInitialStartup();
            DispatcherUnhandledException += AppDispatcherUnhandledException;
        }

        private void AppDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs args)
        {
            args.Exception.Log();
            MessageBox.Show($"{args.Exception.GetType()}\n\nSee exception log for more info.", "Unhandled exception");
            args.Handled = true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            MainWindow = new MainWindow(_windowService);
            MainWindow.Show();
        }
    }
}
