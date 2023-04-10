using System.Windows;

namespace DigitalBattleMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(IWindowService windowService)
        {
            InitializeComponent();
            var viewModel = new MainWindowViewModel(windowService);
            DataContext = viewModel;
        }
    }
}
