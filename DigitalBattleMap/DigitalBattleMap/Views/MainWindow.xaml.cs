using DigitalBattleMap.Interfaces;
using DigitalBattleMap.ViewModels;
using System.Windows;

namespace DigitalBattleMap.Views;

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
 