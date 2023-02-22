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
        private ICommand _saveCommand;
        private ScreenPosition _initialMonitorPosition;

        public SettingsWindowViewModel(Settings settings)
        {
            _settings = settings;
            _saveCommand = new RelayCommand(p => SaveButtonClicked());
            _initialMonitorPosition = _settings.MonitorPosition;

            foreach (var screenPosition in ScreenWrapper.GetScreenPositions())
            {
                MonitorPositions.Add(screenPosition);
            }
        }

        public ICommand SaveCommand { get => _saveCommand; }
        public ObservableCollection<ScreenPosition> MonitorPositions { get; private set; } = new ObservableCollection<ScreenPosition>();
        public bool MonitorChanged { get; set; }

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
    }
}
