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
    public class SelectTokenWindowViewModel : PropertyHandler
    {
        private List<Token> _tokens = new List<Token>();

        public SelectTokenWindowViewModel()
        {
            InitializeProperties();
        }

        public SelectTokenWindowViewModel(List<Token> tokens)
        {
            InitializeProperties();
            _tokens = tokens;

            foreach (var token in _tokens)
            {
                TokenList.Add(token);
            }

            AddCommand = new RelayCommand(p => AddButton());
        }

        public string SearchText { get => Get<string>(); set => Set(value, SearchTextChanged); }
        public Token SelectedToken { get => Get<Token>(); set => Set(value, OnSelectedTokenChanged); }
        public int NumberOfTokens { get => Get<int>(); set => Set(value); }
        public TokenSize SelectedTokenSize { get => Get<TokenSize>(); set => Set(value); }
        public ObservableCollection<Token> TokenList { get; set; } = new ObservableCollection<Token>();
        public bool IsTokenSelected { get => SelectedToken != null; }
        public List<Token> AddedTokens { get; set; } = new List<Token>();
        public ICommand AddCommand { get; set; }

        private void InitializeProperties()
        {
            SearchText = "";
            NumberOfTokens = 1;
            SelectedTokenSize = TokenSize.Medium;
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
            if (SelectedToken != null)
            {
                for (int i = 0; i < NumberOfTokens; i++)
                {
                    AddedTokens.Add(SelectedToken.Copy(SelectedTokenSize));
                }
            }
        }

        private void OnSelectedTokenChanged()
        {
            NumberOfTokens = 1;
            if (SelectedToken != null)
            {
                SelectedTokenSize = SelectedToken.Size;
            }
            else
            {
                SelectedTokenSize = TokenSize.Medium;
            }

            NotifyPropertyChange(nameof(IsTokenSelected));
        }
    }
}
