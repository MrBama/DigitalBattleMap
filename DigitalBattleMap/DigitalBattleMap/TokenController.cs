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
        private object _lock = "";

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
        public event EventHandler SelectedTokenBitmapUpdated;

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
            lock (_lock)
            {
                _monsterTokens = MonsterTokens.GetTokens();
            }
        }

        public bool IsTokenSelected()
        {
            lock (_lock)
            {
                return SelectedToken != null;
            }
        }

        public void AddToken()
        {
            lock (_lock)
            {
                var tokens = new List<Token>(_monsterTokens);
                tokens.AddRange(_settings.CustomTokens);

                var selectTokenWindowViewModel = new SelectTokenWindowViewModel(tokens, _settings.TokenGroups);
                _windowService.ShowWindowDialog<SelectTokenWindow>(selectTokenWindowViewModel);

                if (selectTokenWindowViewModel.AddedTokens.Count > 0)
                {
                    foreach (var (token, index) in selectTokenWindowViewModel.AddedTokens.WithIndex())
                    {
                        var tokenListItem = new TokenListItem();
                        tokenListItem.Token = token;
                        tokenListItem.Token.SizeChanged += TokenSizeChanged;
                        tokenListItem.ConditionsChanged += ConditionsChanged;
                        tokenListItem.Id = GetUniqueId(token.Name);
                        tokenListItem.Position = CalculateStartPosition(index);
                        TokenList.Add(tokenListItem);
                    }

                    SelectedToken = TokenList.Last();
                    CreateTokenBitmap();
                }
            }
        }

        public void RemoveToken()
        {
            lock (_lock)
            {
                if (SelectedToken != null)
                {
                    TokenList.Remove(SelectedToken);
                    CreateTokenBitmap();
                }
            }
        }

        public void ClearTokens()
        {
            lock (_lock)
            {
                TokenList.Clear();
                CreateTokenBitmap();
            }
        }

        public bool IsUpButtonEnabled()
        {
            lock (_lock)
            {
                return TokenList.IndexOf(SelectedToken) > 0;
            }
        }

        public bool IsDownButtonEnabled()
        {
            lock (_lock)
            {
                return TokenList.IndexOf(SelectedToken) < TokenList.Count - 1;
            }
        }

        public void TokenUp()
        {
            lock (_lock)
            {
                var selectedToken = SelectedToken;
                var index = TokenList.IndexOf(SelectedToken);
                TokenList.Remove(selectedToken);
                TokenList.Insert(index - 1, selectedToken);
                SelectedToken = selectedToken;
                NotifyTokenEditorUpdated();
                CreateTokenBitmap();
            }
        }

        public void TokenDown()
        {
            lock (_lock)
            {
                var selectedToken = SelectedToken;
                var index = TokenList.IndexOf(SelectedToken);
                TokenList.Remove(selectedToken);
                TokenList.Insert(index + 1, selectedToken);
                SelectedToken = selectedToken;
                NotifyTokenEditorUpdated();
                CreateTokenBitmap();
            }
        }

        public BitmapSource GetTokenBitmapSource()
        {
            lock (_lock)
            {
                return _tokenBitmap.ToBitmapImage();
            }
        }

        public Bitmap GetTokenBitmap()
        {
            lock (_lock)
            {
                return _tokenBitmap;
            }
        }

        public BitmapSource GetTokenSelectionBitmapSource()
        {
            lock (_lock)
            {
                return _tokenSelectionBitmap.ToBitmapImage();
            }
        }

        public void UpdateGridSize(int gridSize)
        {
            lock (_lock)
            {
                _gridSize = gridSize;
                if (TokenList.Count > 0)
                {
                    CreateTokenBitmap();
                }
            }
        }

        public void MouseDown(Point<double> point)
        {
            lock (_lock)
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
        }

        public void SetCanvasSize(Size<double> canvasSize)
        {
            lock (_lock)
            {
                _canvasSize = canvasSize;
            }
        }

        public void MoveTokens(ArrowDirection direction)
        {
            lock (_lock)
            {
                foreach (var tokenListItem in TokenList)
                {
                    switch (direction)
                    {
                        case ArrowDirection.Up:
                            tokenListItem.Position.Y += _gridSize;
                            break;
                        case ArrowDirection.Down:
                            tokenListItem.Position.Y -= _gridSize;
                            break;
                        case ArrowDirection.Left:
                            tokenListItem.Position.X += _gridSize;
                            break;
                        case ArrowDirection.Right:
                            tokenListItem.Position.X -= _gridSize;
                            break;
                    }
                }

                CreateTokenBitmap();
            }
        }

        public void AddToSaveFile(SaveFile saveFile)
        {
            lock (_lock)
            {
                saveFile.TokenList = TokenList.ToList();
            }
        }

        public void OpenSaveFile(SaveFile saveFile)
        {
            lock (_lock)
            {
                ClearTokens();
                foreach (var tokenListItem in saveFile.TokenList)
                {
                    TokenList.Add(tokenListItem);
                    tokenListItem.Token.SizeChanged += TokenSizeChanged;
                    tokenListItem.ConditionsChanged += ConditionsChanged;
                }

                CreateTokenBitmap();
            }
        }

        public void CustomTokens()
        {
            lock (_lock)
            {
                var customTokensWindowViewModel = new CustomTokensWindowViewModel(_windowService, _settings, _monsterTokens);
                _windowService.ShowWindowDialog<CustomTokensWindow>(customTokensWindowViewModel);
            }
        }

        public void OnMoveTokenAction(object sender, MoveTokenActionEventArgs e)
        {
            lock (_lock)
            {
                var tokenListItem = TokenList.SingleOrDefault(t => t.Token.Name.ToLower() == e.Name.ToLower() && t.Id == e.Id && t.Token.PlayerControl);
                if (tokenListItem != null)
                {
                    switch (e.Direction)
                    {
                        case TokenDirection.UpLeft:
                            tokenListItem.Position.X -= _gridSize;
                            tokenListItem.Position.Y -= _gridSize;
                            break;
                        case TokenDirection.Up:
                            tokenListItem.Position.Y -= _gridSize;
                            break;
                        case TokenDirection.UpRight:
                            tokenListItem.Position.X += _gridSize;
                            tokenListItem.Position.Y -= _gridSize;
                            break;
                        case TokenDirection.Left:
                            tokenListItem.Position.X -= _gridSize;
                            break;
                        case TokenDirection.Right:
                            tokenListItem.Position.X += _gridSize;
                            break;
                        case TokenDirection.DownLeft:
                            tokenListItem.Position.X -= _gridSize;
                            tokenListItem.Position.Y += _gridSize;
                            break;
                        case TokenDirection.Down:
                            tokenListItem.Position.Y += _gridSize;
                            break;
                        case TokenDirection.DownRight:
                            tokenListItem.Position.X += _gridSize;
                            tokenListItem.Position.Y += _gridSize;
                            break;
                        default:
                            break;
                    }

                    CreateTokenBitmap();
                }
            }
        }

        public void Zoom(double zoomFactor)
        {
            lock (_lock)
            {
                if (TokenList.Count > 0)
                {
                    foreach (var tokenListItem in TokenList)
                    {
                        double newX = tokenListItem.Position.X;
                        double newY = tokenListItem.Position.Y;

                        double halfWidth = _bitmapSize.Width / 2;
                        double halfHeight = _bitmapSize.Height / 2;

                        newX -= halfWidth;
                        newX *= zoomFactor;
                        newX += halfWidth;

                        newY -= halfHeight;
                        newY *= zoomFactor;
                        newY += halfHeight;

                        tokenListItem.Position.X = (int)Math.Round(newX);
                        tokenListItem.Position.Y = (int)Math.Round(newY);
                    }

                    CreateTokenBitmap();
                }
            }
        }

        private void NotifyTokenEditorUpdated()
        {
            TokenEditorUpdated?.Invoke(this, new EventArgs());
        }

        private void NotifyTokenBitmapUpdated()
        {
            TokenBitmapUpdated?.Invoke(this, new EventArgs());
        }

        private void NotifySelectedTokenBitmapUpdated()
        {
            SelectedTokenBitmapUpdated?.Invoke(this, new EventArgs());
        }

        private Point<int> CalculateStartPosition(int index)
        {
            var position = new Point<int>(_bitmapSize.Width / 2, _bitmapSize.Height / 2);
            var maxGridCellsX = position.X + (_gridSize / 2);
            maxGridCellsX /= _gridSize;
            var maxGridCellsY = position.Y + (_gridSize / 2);
            maxGridCellsY /= _gridSize;

            if (index < maxGridCellsX * maxGridCellsY)
            {
                var gridCellsY = index / maxGridCellsX;
                var gridcellsX = index - (gridCellsY * maxGridCellsX);

                position.X += gridcellsX * _gridSize;
                position.Y += gridCellsY * _gridSize;
            }

            return position;
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
            lock (_lock)
            {
                _tokenBitmap = BitmapTools.CreateEmptyBitmap();
                _tokenSelectionBitmap = BitmapTools.CreateEmptyBitmap();

                if (TokenList.Count > 0)
                {
                    foreach (var tokenListItem in TokenList)
                    {
                        BitmapTools.DrawToken(_tokenBitmap, tokenListItem, GetTokenIdString(tokenListItem), _gridSize);
                    }
                    UpdateTokenSelection();
                }

                NotifyTokenBitmapUpdated();
            }
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

        private void ConditionsChanged(object? sender, EventArgs e)
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

            NotifySelectedTokenBitmapUpdated();
        }
    }
}
