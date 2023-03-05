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
                    NotifyPropertyChange(nameof(AddButtonEnabled));
                    NotifyPropertyChange();
                }
            }
        }

        public ObservableCollection<Token> TokenList { get; set; } = new ObservableCollection<Token>();
        public bool AddButtonEnabled { get => SelectedToken != null; }

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
    }
}
