using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DigitalBattleMap
{
    public class CustomTokensWindowViewModel : PropertyHandler
    {
        private IWindowService _windowService;
        private Settings _settings;
        private List<string> _monsterTokenNames;

        public CustomTokensWindowViewModel()
        {
        }

        public CustomTokensWindowViewModel(IWindowService windowService, Settings settings, List<string> monsterTokenNames)
        {
            _windowService = windowService;
            _settings = settings;
            _monsterTokenNames = monsterTokenNames;

            foreach (var token in _settings.CustomTokens.OrderBy(t => t.Name))
            {
                TokenList.Add(token);
            }

            AddTokenCommand = new RelayCommand(p => AddToken());
            RemoveTokenCommand = new RelayCommand(p => RemoveToken());
            EditTokenCommand = new RelayCommand(p => EditToken());
        }

        public ObservableCollection<Token> TokenList { get; set; } = new ObservableCollection<Token>();
        public Token SelectedToken { get => Get<Token>(); set => Set(value, () => NotifyPropertyChange(nameof(IsTokenSelected))); }
        public bool IsTokenSelected { get => SelectedToken != null; }
        public ICommand AddTokenCommand { get; set; }
        public ICommand RemoveTokenCommand { get; set; }
        public ICommand EditTokenCommand { get; set; }

        private void Save()
        {
            _settings.CustomTokens = TokenList.ToList();
            _settings.Save();
        }

        private void AddToken()
        {
            var tokenNames = new List<string>(_monsterTokenNames);
            tokenNames.AddRange(_settings.CustomTokens.Select(t => t.Name));
            var createTokenWindowViewModel = new CreateTokenWindowViewModel(_windowService, tokenNames);
            _windowService.ShowWindowDialog<CreateTokenWindow>(createTokenWindowViewModel);

            if (createTokenWindowViewModel.Token != null)
            {
                TokenList.Add(createTokenWindowViewModel.Token);
                Save();
            }
        }

        private void RemoveToken()
        {
            File.Delete(SelectedToken.ImagePath);
            TokenList.Remove(SelectedToken);
            Save();
        }

        private void EditToken()
        {
            var tokenNames = new List<string>(_monsterTokenNames);
            tokenNames.AddRange(_settings.CustomTokens.Select(t => t.Name));
            tokenNames.Remove(SelectedToken.Name);

            var createTokenWindowViewModel = new CreateTokenWindowViewModel(_windowService, tokenNames, SelectedToken);
            _windowService.ShowWindowDialog<CreateTokenWindow>(createTokenWindowViewModel);

            if (createTokenWindowViewModel.Token != null)
            {
                TokenList.Remove(SelectedToken);
                TokenList.Add(createTokenWindowViewModel.Token);
                SelectedToken = createTokenWindowViewModel.Token;
                Save();
            }
        }
    }
}
