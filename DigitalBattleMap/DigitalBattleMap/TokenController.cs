using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
    public class TokenController
    {
        private IWindowService _windowService;
        private Bitmap _tokenBitmap;
        private List<Token> _tokens = new List<Token>();
        private TokenListItem _selectedToken;

        public TokenController(IWindowService windowService)
        {
            _windowService = windowService;
            _tokenBitmap = BitmapTools.CreateEmptyBitmap();
            ReloadTokens();
        }

        public event EventHandler TokensUpdated;

        public TokenListItem SelectedToken
        { 
            get => _selectedToken;
            set
            {
                if(value != _selectedToken)
                {
                    _selectedToken = value;
                    NotifyTokensUpdated();
                }
            }
        }

        public ObservableCollection<TokenListItem> TokenList { get; set; } = new ObservableCollection<TokenListItem>();
        public bool IsTokenSelected { get => SelectedToken != null; }

        public void ReloadTokens()
        {
            _tokens = new List<Token>();
            _tokens.AddRange(MonsterTokens.GetTokens());
        }
        
        public void AddToken()
        {
            var selectTokenWindowViewModel = new SelectTokenWindowViewModel(_tokens);
            _windowService.ShowWindowDialog<SelectTokenWindow>(selectTokenWindowViewModel);

            foreach (var token in selectTokenWindowViewModel.Tokens)
            {
                var tokenListItem = new TokenListItem();
                tokenListItem.Token = token;
                tokenListItem.Id = GetUniqueId(token.Name);
                TokenList.Add(tokenListItem);
            }        
        }

        public void RemoveToken()
        {
            if(SelectedToken != null)
            {
                TokenList.Remove(SelectedToken);
            }
        }

        public void ClearTokens()
        {
            TokenList.Clear();
        }

        private void NotifyTokensUpdated()
        {
            TokensUpdated?.Invoke(this, new EventArgs());
        }

        private int GetUniqueId(string tokenName)
        {
            var tokens = TokenList.Where(t => t.Token.Name == tokenName).ToList();

            if(tokens.Count != 0)
            {
                return tokens.Max(t => t.Id) + 1;
            }
            else
            {
                return 1;
            }
        }
    }
}
