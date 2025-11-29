using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
    private Settings _settings;
    private IWindowService _windowService;

    public SettingsWindowViewModel(Settings settings, IWindowService windowService)
    {
        _settings = settings;
        _windowService = windowService;

        DefaultGridSize = _settings.DefaultGridSize;
        ServerAddress = _settings.ServerAddress;
        SelectedMonitorPosition = _settings.MonitorPosition;
        ShowMapWindow = _settings.ShowMapWindow;
        HideDungeonMasterFeatures = _settings.HideDungeonMasterFeatures;
        HasBlackBackground = _settings.HasBlackBackground;
        IsAutoSaveEnabled = _settings.IsAutoSaveEnabled;

        foreach (var screenPosition in ScreenWrapper.GetScreenPositions())
        {
            MonitorPositions.Add(screenPosition);
        }
    }

    protected override void InitializeCommands()
    {
        SaveCommand = new RelayCommand(p => SaveButtonClicked());
        DownloadMonsterTokensCommand = new RelayCommand(p => DownloadMonsterTokens());
        WebExtensionsCommand = new RelayCommand(p => ManageWebExtensions());
        ImportCommand = new RelayCommand(p => Import());
        ExportCommand = new RelayCommand(p => Export());
    }

    public ObservableCollection<ScreenPosition> MonitorPositions { get; private set; } = new ObservableCollection<ScreenPosition>();
    public bool MonsterTokensDownloaded { get; set; }

    public ICommand SaveCommand { get; set; }
    public ICommand DownloadMonsterTokensCommand { get; set; }
    public ICommand WebExtensionsCommand { get; set; }
    public ICommand ImportCommand { get; set; }
    public ICommand ExportCommand { get; set; }

    public int DefaultGridSize { get => Get<int>(); set => Set(value); }
    public string ServerAddress { get => Get<string>(); set => Set(value); }
    public ScreenPosition SelectedMonitorPosition { get => Get<ScreenPosition>(); set => Set(value); }
    public bool ShowMapWindow { get => Get<bool>(); set => Set(value); }
    public bool HideDungeonMasterFeatures { get => Get<bool>(); set => Set(value); }
    public bool HasBlackBackground { get => Get<bool>(); set => Set(value); }
    public bool IsAutoSaveEnabled { get => Get<bool>(); set => Set(value); }

    private void SaveButtonClicked()
    {
        _settings.DefaultGridSize = DefaultGridSize;
        _settings.ServerAddress = ServerAddress;
        _settings.MonitorPosition = SelectedMonitorPosition;
        _settings.ShowMapWindow = ShowMapWindow;
        _settings.HideDungeonMasterFeatures = HideDungeonMasterFeatures;
        _settings.HasBlackBackground = HasBlackBackground;
        _settings.IsAutoSaveEnabled = IsAutoSaveEnabled;

        _settings.Save();
    }

    private void DownloadMonsterTokens()
    {
        var downloadWindowViewModel = new DownloadWindowViewModel(_windowService);
        downloadWindowViewModel.StartDownload();
        _windowService.ShowWindowDialog<DownloadWindow>(downloadWindowViewModel);
        MonsterTokensDownloaded = true;
    }

    private void ManageWebExtensions()
    {
        var webExtensionsViewModel = new WebExtensionsViewModel(_settings.WebExtensionVersions, _windowService);
        _windowService.ShowWindowDialog<WebExtensionsWindow>(webExtensionsViewModel);
        if(webExtensionsViewModel.InstalledOrUpdatedExtension)
        {
            _settings.Save();
        }
    }

    private void Import()
    {
        if (_windowService.ShowOpenFilesDialog(out List<string> paths, "(*.campaign, *.tokengroup, *.token)|*.campaign;*.tokengroup;*.token"))
        {
            foreach (var path in paths)
            {
                ImportExport.Import(path, _settings);
            }
        }
    }

    private void Export()
    {
        var exportViewModel = new ExportWindowViewModel(_windowService, _settings);
        _windowService.ShowWindowDialog<ExportWindow>(exportViewModel);
    }
}
