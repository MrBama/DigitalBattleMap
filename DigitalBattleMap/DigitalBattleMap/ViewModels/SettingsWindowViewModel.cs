using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
    private Settings _settings;
    private IWindowService _windowService;
    private ScreenPosition _initialMonitorPosition;

    public SettingsWindowViewModel(Settings settings, IWindowService windowService)
    {
        _settings = settings;
        _windowService = windowService;

        DefaultGridSize = _settings.DefaultGridSize;
        ServerAddress = _settings.ServerAddress;
        SelectedMonitorPosition = _settings.MonitorPosition;
        ShowMapWindow = _settings.ShowMapWindow;

        foreach (var screenPosition in ScreenWrapper.GetScreenPositions())
        {
            MonitorPositions.Add(screenPosition);
        }
    }

    protected override void InitializeCommands()
    {
        SaveCommand = new RelayCommand(p => SaveButtonClicked());
        DownloadMonsterTokensCommand = new RelayCommand(p => DownloadMonsterTokens());
    }

    public ObservableCollection<ScreenPosition> MonitorPositions { get; private set; } = new ObservableCollection<ScreenPosition>();
    public bool MonitorChanged { get; set; }
    public bool MonsterTokensDownloaded { get; set; }

    public ICommand SaveCommand { get; set; }
    public ICommand DownloadMonsterTokensCommand { get; set; }

    public int DefaultGridSize { get => Get<int>(); set => Set(value); }
    public string ServerAddress { get => Get<string>(); set => Set(value); }
    public ScreenPosition SelectedMonitorPosition { get => Get<ScreenPosition>(); set => Set(value); }
    public bool ShowMapWindow { get => Get<bool>(); set => Set(value); }

    private void SaveButtonClicked()
    {
        if(!SelectedMonitorPosition.Equals(_initialMonitorPosition))
        {
            MonitorChanged = true;
        }

        _settings.DefaultGridSize = DefaultGridSize;
        _settings.ServerAddress = ServerAddress;
        _settings.MonitorPosition = SelectedMonitorPosition;
        _settings.ShowMapWindow = ShowMapWindow;

        _settings.Save();
    }

    private void DownloadMonsterTokens()
    {
        var downloadWindowViewModel = new DownloadWindowViewModel(_windowService);
        downloadWindowViewModel.StartDownload();
        _windowService.ShowWindowDialog<DownloadWindow>(downloadWindowViewModel);
        MonsterTokensDownloaded = true;
    }
}
