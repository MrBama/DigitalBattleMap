using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DigitalBattleMap
{
    public class SelectTokenWindowViewModel : INotifyPropertyChanged
    {
        private List<Token> _tokens;
        private string _searchText = "";
        private Token _selectedToken;

        public SelectTokenWindowViewModel()
        {
        }

        public SelectTokenWindowViewModel(List<Token> tokens)
        {
            _tokens = tokens;

            foreach (var token in _tokens)
            {
                TokenList.Add(token);
            }

            AddCommand = new RelayCommand(p => AddButton());
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (value != _searchText)
                {
                    _searchText = value;
                    SearchTextChanged();
                    NotifyPropertyChange();
                }
            }
        }

        public Token SelectedToken
        {
            get => _selectedToken;
            set
            {
                if (value != _selectedToken)
                {
                    _selectedToken = value;
                    OnSelectedTokenChanged();
                    NotifyPropertyChange(nameof(IsTokenSelected));
                    NotifyPropertyChange();
                }
            }
        }

        public ObservableCollection<Token> TokenList { get; set; } = new ObservableCollection<Token>();
        public bool IsTokenSelected { get => SelectedToken != null; }
        public int NumberOfTokens { get; set; } = 1;
        public TokenSize SelectedTokenSize { get; set; } = TokenSize.Medium;
        public List<Token> Tokens { get; set; } = new List<Token>();
        public ICommand AddCommand { get; set; }

        private void NotifyPropertyChange([CallerMemberName] string propertyname = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        private void SearchTextChanged()
        {
            TokenList.Clear();
            foreach (var token in _tokens)
            {
                if (token.Name.ToLower().Contains(SearchText.ToLower()))
                {
                    TokenList.Add(token);
                }
            }
        }

        private void AddButton()
        {
            if(SelectedToken != null)
            {
                for(int i = 0; i < NumberOfTokens; i++)
                {
                    if(SelectedToken.Size == SelectedTokenSize)
                    {
                        Tokens.Add(SelectedToken);
                    }
                    else
                    {
                        Tokens.Add(SelectedToken.Copy(SelectedTokenSize));
                    }   
                }
            }
        }

        private void OnSelectedTokenChanged()
        {
            NumberOfTokens = 1;
            if(SelectedToken != null)
            {
                SelectedTokenSize = SelectedToken.Size;
            }
            else
            {
                SelectedTokenSize = TokenSize.Medium;
            }
            
            NotifyPropertyChange(nameof(NumberOfTokens));
            NotifyPropertyChange(nameof(SelectedTokenSize));
        }
    }
}
