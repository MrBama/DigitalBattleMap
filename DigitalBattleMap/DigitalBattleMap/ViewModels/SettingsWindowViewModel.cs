using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DigitalBattleMap
{
    public class SettingsWindowViewModel
    {
        private Settings _settings;
        private IWindowService _windowService;
        private ScreenPosition _initialMonitorPosition;

        public SettingsWindowViewModel(Settings settings, IWindowService windowService)
        {
            _settings = settings;
            _windowService = windowService;
            SaveCommand = new RelayCommand(p => SaveButtonClicked());
            DownloadMonsterTokensCommand = new RelayCommand(p => DownloadMonsterTokens());
            _initialMonitorPosition = _settings.MonitorPosition;

            foreach (var screenPosition in ScreenWrapper.GetScreenPositions())
            {
                MonitorPositions.Add(screenPosition);
            }
        }

        public ICommand SaveCommand { get; set; }
        public ICommand DownloadMonsterTokensCommand { get; set; }
        public ObservableCollection<ScreenPosition> MonitorPositions { get; private set; } = new ObservableCollection<ScreenPosition>();
        public bool MonitorChanged { get; set; }
        public bool MonsterTokensDownloaded { get; set; }

        public int DefaultGridSize 
        {
            get => _settings.DefaultGridSize; 
            set
            {
                if(value != _settings.DefaultGridSize)
                {
                    _settings.DefaultGridSize = value;
                }
            }
        }

        public string ServerAddress
        {
            get => _settings.ServerAddress;
            set
            {
                if (value != _settings.ServerAddress)
                {
                    _settings.ServerAddress = value;
                }
            }
        }

        public ScreenPosition SelectedMonitorPosition
        {
            get => _settings.MonitorPosition;
            set
            {
                if (value != _settings.MonitorPosition)
                {
                    _settings.MonitorPosition = value;
                }
            }
        }

        private void SaveButtonClicked()
        {
            if(!SelectedMonitorPosition.Equals(_initialMonitorPosition))
            {
                MonitorChanged = true;
            }

            _settings.Save();
        }

        private void DownloadMonsterTokens()
        {
            var downloadWindowViewModel = new DownloadWindowViewModel();
            downloadWindowViewModel.StartDownload();
            _windowService.ShowWindowDialog<DownloadWindow>(downloadWindowViewModel);
            MonsterTokensDownloaded = true;
        }
    }
}
