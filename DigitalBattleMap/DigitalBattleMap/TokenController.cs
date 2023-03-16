using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace DigitalBattleMap
{
    public class TokenController
    {
        private IWindowService _windowService;
        private Bitmap _tokenBitmap;
        private Bitmap _tokenSelectionBitmap;
        private List<Token> _monsterTokens = new List<Token>();
        private TokenListItem _selectedToken;
        private Size<int> _bitmapSize;
        private Size<double> _canvasSize;
        private int _gridSize;
        private Settings _settings;

        public TokenController(IWindowService windowService, Settings settings, int gridSize)
        {
            _windowService = windowService;
            _settings = settings;
            _gridSize = gridSize;
            _tokenBitmap = BitmapTools.CreateEmptyBitmap();
            _tokenSelectionBitmap = BitmapTools.CreateEmptyBitmap();
            _bitmapSize = BitmapTools.GetBitmapSize();
            ReloadMonsterTokens();
        }

        public event EventHandler TokenEditorUpdated;
        public event EventHandler TokenBitmapUpdated;

        public TokenListItem SelectedToken
        {
            get => _selectedToken;
            set
            {
                if (value != _selectedToken)
                {
                    _selectedToken = value;
                    UpdateTokenSelection();
                    NotifyTokenEditorUpdated();
                }
            }
        }

        public ObservableCollection<TokenListItem> TokenList { get; set; } = new ObservableCollection<TokenListItem>();

        public void ReloadMonsterTokens()
        {
            _monsterTokens = MonsterTokens.GetTokens();
        }

        public bool IsTokenSelected()
        {
            return SelectedToken != null;
        }

        public void AddToken()
        {
            var tokens = new List<Token>(_monsterTokens);
            tokens.AddRange(_settings.CustomTokens);

            var selectTokenWindowViewModel = new SelectTokenWindowViewModel(tokens);
            _windowService.ShowWindowDialog<SelectTokenWindow>(selectTokenWindowViewModel);

            if (selectTokenWindowViewModel.AddedTokens.Count > 0)
            {
                foreach (var token in selectTokenWindowViewModel.AddedTokens)
                {
                    var tokenListItem = new TokenListItem();
                    tokenListItem.Token = token;
                    tokenListItem.Token.SizeChanged += TokenSizeChanged;
                    tokenListItem.Id = GetUniqueId(token.Name);
                    tokenListItem.Position = new Point<int>(_bitmapSize.Width / 2, _bitmapSize.Height / 2);
                    TokenList.Add(tokenListItem);
                }

                SelectedToken = TokenList.Last();
                CreateTokenBitmap();
            }
        }

        public void RemoveToken()
        {
            if (SelectedToken != null)
            {
                TokenList.Remove(SelectedToken);
                CreateTokenBitmap();
            }
        }

        public void ClearTokens()
        {
            TokenList.Clear();
            CreateTokenBitmap();
        }

        public bool IsUpButtonEnabled()
        {
            return TokenList.IndexOf(SelectedToken) > 0;
        }

        public bool IsDownButtonEnabled()
        {
            return TokenList.IndexOf(SelectedToken) < TokenList.Count - 1;
        }

        public void TokenUp()
        {
            var selectedToken = SelectedToken;
            var index = TokenList.IndexOf(SelectedToken);
            TokenList.Remove(selectedToken);
            TokenList.Insert(index - 1, selectedToken);
            SelectedToken = selectedToken;
            NotifyTokenEditorUpdated();
            CreateTokenBitmap();
        }

        public void TokenDown()
        {
            var selectedToken = SelectedToken;
            var index = TokenList.IndexOf(SelectedToken);
            TokenList.Remove(selectedToken);
            TokenList.Insert(index + 1, selectedToken);
            SelectedToken = selectedToken;
            NotifyTokenEditorUpdated();
            CreateTokenBitmap();
        }

        public BitmapSource GetTokenBitmapSource()
        {
            return _tokenBitmap.ToBitmapImage();
        }

        public BitmapSource GetTokenSelectionBitmapSource()
        {
            return _tokenSelectionBitmap.ToBitmapImage();
        }

        public void UpdateGridSize(int gridSize)
        {
            _gridSize = gridSize;
            if (TokenList.Count > 0)
            {
                CreateTokenBitmap();
            }
        }

        public void MouseDown(Point<double> point)
        {
            if (SelectedToken != null)
            {
                var newPosition = new Point<int>();
                newPosition.X = (int)point.X.Map(0, _canvasSize.Width, 0, _bitmapSize.Width);
                newPosition.Y = (int)point.Y.Map(0, _canvasSize.Height, 0, _bitmapSize.Height);
                SelectedToken.Position = newPosition;

                CreateTokenBitmap();
            }
        }

        public void SetCanvasSize(Size<double> canvasSize)
        {
            _canvasSize = canvasSize;
        }

        public void MoveTokens(ArrowDirection direction)
        {
            foreach (var tokenListItem in TokenList)
            {
                switch (direction)
                {
                    case ArrowDirection.Up:
                        tokenListItem.Position.Y -= _gridSize;
                        break;
                    case ArrowDirection.Down:
                        tokenListItem.Position.Y += _gridSize;
                        break;
                    case ArrowDirection.Left:
                        tokenListItem.Position.X -= _gridSize;
                        break;
                    case ArrowDirection.Right:
                        tokenListItem.Position.X += _gridSize;
                        break;
                }
            }

            CreateTokenBitmap();
        }

        public void AddToSaveFile(SaveFile saveFile)
        {
            saveFile.TokenList = TokenList.ToList();
        }

        public void OpenSaveFile(SaveFile saveFile)
        {
            ClearTokens();
            foreach (var tokenListItem in saveFile.TokenList)
            {
                TokenList.Add(tokenListItem);
                tokenListItem.Token.SizeChanged += TokenSizeChanged;
            }

            CreateTokenBitmap();
        }

        public void CustomTokens()
        {
            var customTokensWindowViewModel = new CustomTokensWindowViewModel(_windowService, _settings, _monsterTokens.Select(t => t.Name).ToList());
            _windowService.ShowWindowDialog<CustomTokensWindow>(customTokensWindowViewModel);
        }

        private void NotifyTokenEditorUpdated()
        {
            TokenEditorUpdated?.Invoke(this, new EventArgs());
        }

        private void NotifyTokenBitmapUpdated()
        {
            TokenBitmapUpdated?.Invoke(this, new EventArgs());
        }

        private int GetUniqueId(string tokenName)
        {
            var tokens = TokenList.Where(t => t.Token.Name == tokenName).ToList();

            if (tokens.Count != 0)
            {
                return tokens.Max(t => t.Id) + 1;
            }
            else
            {
                return 1;
            }
        }

        private void CreateTokenBitmap()
        {
            _tokenBitmap = BitmapTools.CreateEmptyBitmap();
            _tokenSelectionBitmap = BitmapTools.CreateEmptyBitmap();

            if (TokenList.Count > 0)
            {
                foreach (var tokenListItem in TokenList)
                {   
                    BitmapTools.DrawToken(_tokenBitmap, tokenListItem.GetBitmap(), tokenListItem.Token.GetSizeFactor(), tokenListItem.Position, GetTokenIdString(tokenListItem), _gridSize);
                }
                UpdateTokenSelection();
            }

            NotifyTokenBitmapUpdated();
        }

        private string GetTokenIdString(TokenListItem tokenListItem)
        {
            var tokenId = "";
            if (TokenList.Count(t => t.Token.Name == tokenListItem.Token.Name) > 1)
            {
                tokenId = tokenListItem.Id.ToString();
            }

            return tokenId;
        }

        private void TokenSizeChanged(object? sender, EventArgs e)
        {
            CreateTokenBitmap();
        }

        private void UpdateTokenSelection()
        {
            _tokenSelectionBitmap = BitmapTools.CreateEmptyBitmap();
            if (SelectedToken != null)
            {
                BitmapTools.DrawTokenSelection(_tokenSelectionBitmap, SelectedToken.Token.GetSizeFactor(), SelectedToken.Position, _gridSize);
            }

            NotifyTokenBitmapUpdated();
        }
    }
}
