using DigitalBattleMap.Common;
using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels;

public class TokenControllerViewModel : ControllerViewModelBase
{
    private IWindowService _windowService;
    private Bitmap _tokenBitmap;
    private Bitmap _tokenSelectionBitmap;
    private List<Token> _monsterTokens = new();
    private Settings _settings;
    private object _lock = "";

    public TokenControllerViewModel() : base(50)
    {
        Initialize();
    }

    public TokenControllerViewModel(IWindowService windowService, Settings settings, int gridSize) : base(gridSize)
    {
        _windowService = windowService;
        _settings = settings;
        Initialize();
    }

    private void Initialize()
    {
        TokenBitmap = BitmapTools.CreateEmptyBitmap();
        TokenSelectionBitmap = BitmapTools.CreateEmptyBitmap();
        ReloadMonsterTokens();
    }

    protected override void InitializeCommands()
    {
        AddTokenCommand = new RelayCommand(p => AddToken());
        RemoveTokenCommand = new RelayCommand(p => RemoveToken());
        ClearTokensCommand = new RelayCommand(p => ClearTokens());
        TokenUpCommand = new RelayCommand(p => InitiativeUp());
        TokenDownCommand = new RelayCommand(p => InitiativeDown());
        CustomTokensCommand = new RelayCommand(p => CustomTokens());
        SortInitiativeCommand = new RelayCommand(p => SortInitiative());
    }

    public event EventHandler OnTokenBitmapUpdated;

    public BitmapSource TokenBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource TokenSelectionBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public TokenListItem SelectedToken
    {
        get => Get<TokenListItem>();
        set => Set(value, () =>
        {
            UpdateTokenSelection();
            NotifyPropertyChange(nameof(IsTokenSelected));
            NotifyPropertyChange(nameof(IsTokenUpButtonEnabled));
            NotifyPropertyChange(nameof(IsTokenDownButtonEnabled));
        });
    }
    public bool IsTokenSelected { get; set; }
    public bool IsTokenUpButtonEnabled { get => TokenList.IndexOf(SelectedToken) > 0; }
    public bool IsTokenDownButtonEnabled { get => TokenList.IndexOf(SelectedToken) < TokenList.Count - 1; }
    public ObservableCollection<TokenListItem> TokenList { get; set; } = new ObservableCollection<TokenListItem>();
    public BitmapSource MapArrowUpBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Up).ToBitmapImage(); }
    public BitmapSource MapArrowDownBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Down).ToBitmapImage(); }

    public ICommand AddTokenCommand { get; set; }
    public ICommand RemoveTokenCommand { get; set; }
    public ICommand ClearTokensCommand { get; set; }
    public ICommand TokenUpCommand { get; set; }
    public ICommand TokenDownCommand { get; set; }
    public ICommand CustomTokensCommand { get; set; }
    public ICommand SortInitiativeCommand { get; set; }

    private Bitmap TokenBitmap
    {
        get => _tokenBitmap;
        set
        {
            if (value != _tokenBitmap)
            {
                _tokenBitmap = value;
                TokenBitmapSource = value.ToBitmapImage();
            }
        }
    }

    private Bitmap TokenSelectionBitmap
    {
        get => _tokenSelectionBitmap;
        set
        {
            if (value != _tokenSelectionBitmap)
            {
                _tokenSelectionBitmap = value;
                TokenSelectionBitmapSource = value.ToBitmapImage();
            }
        }
    }

    public void ReloadMonsterTokens()
    {
        lock (_lock)
        {
            _monsterTokens = MonsterTokens.GetTokens();
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
                    tokenListItem.Token.OnSizeChanged += TokenChanged;
                    tokenListItem.OnTokenChanged += TokenChanged;
                    tokenListItem.OnZLevelChanged += ZLevelChanged;
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

    public void InitiativeUp()
    {
        lock (_lock)
        {
            var selectedToken = SelectedToken;
            var index = TokenList.IndexOf(SelectedToken);
            TokenList.Remove(selectedToken);
            TokenList.Insert(index - 1, selectedToken);
            SelectedToken = selectedToken;
        }
    }

    public void InitiativeDown()
    {
        lock (_lock)
        {
            var selectedToken = SelectedToken;
            var index = TokenList.IndexOf(SelectedToken);
            TokenList.Remove(selectedToken);
            TokenList.Insert(index + 1, selectedToken);
            SelectedToken = selectedToken;
        }
    }

    public Bitmap GetTokenBitmap()
    {
        lock (_lock)
        {
            return TokenBitmap;
        }
    }

    public override void UpdateGridSize(int gridSize)
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

    public override void SetCanvasSize(Size<double> canvasSize)
    {
        lock (_lock)
        {
            _canvasSize = canvasSize;
        }
    }

    public override void Move(ArrowDirection direction)
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

    public override void AddToSaveFile(SaveFile saveFile)
    {
        lock (_lock)
        {
            saveFile.TokenList = TokenList.ToList();
        }
    }

    public override void OpenSaveFile(SaveFile saveFile)
    {
        lock (_lock)
        {
            ClearTokens();
            foreach (var tokenListItem in saveFile.TokenList)
            {
                TokenList.Add(tokenListItem);
                tokenListItem.Token.OnSizeChanged += TokenChanged;
                tokenListItem.OnTokenChanged += TokenChanged;
                tokenListItem.OnZLevelChanged += ZLevelChanged;
                tokenListItem.Health.InitializeEditorHp();
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
            TokenListItem? tokenListItem = TokenList.SingleOrDefault(t => string.Equals(t.Token.Name, e.Name, StringComparison.CurrentCultureIgnoreCase) && t.Token.PlayerControl);
            if (tokenListItem != null)
            {
                switch (e.Direction)
                {
                    case Direction.North:
                        tokenListItem.Position.Y -= _gridSize;
                        break;
                    case Direction.NorthEast:
                        tokenListItem.Position.X += _gridSize;
                        tokenListItem.Position.Y -= _gridSize;
                        break;
                    case Direction.East:
                        tokenListItem.Position.X += _gridSize;
                        break;
                    case Direction.SouthEast:
                        tokenListItem.Position.X += _gridSize;
                        tokenListItem.Position.Y += _gridSize;
                        break;
                    case Direction.South:
                        tokenListItem.Position.Y += _gridSize;
                        break;
                    case Direction.SouthWest:
                        tokenListItem.Position.X -= _gridSize;
                        tokenListItem.Position.Y += _gridSize;
                        break;
                    case Direction.West:
                        tokenListItem.Position.X -= _gridSize;
                        break;
                    case Direction.NorthWest:
                        tokenListItem.Position.X -= _gridSize;
                        tokenListItem.Position.Y -= _gridSize;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                CreateTokenBitmap();
            }
        }
    }

    public void OnToggleConditionAction(object sender, ToggleConditionActionEventArgs e)
    {
        lock (_lock)
        {
            TokenListItem? tokenListItem = TokenList.SingleOrDefault(t => string.Equals(t.Token.Name, e.Name, StringComparison.CurrentCultureIgnoreCase) && t.Token.PlayerControl);
            if (tokenListItem != null)
            {
                tokenListItem.ToggleCondition(e.Condition);
                CreateTokenBitmap();
            }
        }
    }

    public override void Zoom(double zoomFactor)
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

    public void SortInitiative()
    {
        lock (_lock)
        {
            TokenList.OrderCurrentByDescending(t => t.Initiative);
        }
    }

    private void NotifyTokenBitmapUpdated()
    {
        OnTokenBitmapUpdated?.Invoke(this, new EventArgs());
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
            var bitmap = BitmapTools.CreateEmptyBitmap();
            TokenSelectionBitmap = BitmapTools.CreateEmptyBitmap();

            if (TokenList.Count > 0)
            {
                foreach (var tokenListItem in TokenList.OrderBy(t => t.ZLevel))
                {
                    if (tokenListItem.Visible)
                    {
                        BitmapTools.DrawToken(bitmap, tokenListItem, GetTokenIdString(tokenListItem), _gridSize);
                    }
                }
                UpdateTokenSelection();
            }

            TokenBitmap = bitmap;
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

    private void TokenChanged(object? sender, EventArgs e)
    {
        CreateTokenBitmap();
    }

    private void UpdateTokenSelection()
    {
        var bitmap = BitmapTools.CreateEmptyBitmap();
        if (SelectedToken != null)
        {
            BitmapTools.DrawTokenSelection(bitmap, SelectedToken.Token.GetSizeFactor(), SelectedToken.Position, _gridSize);
        }

        TokenSelectionBitmap = bitmap;
    }

    private void ZLevelChanged(object? sender, ZLevelChangedEventArgs eventArgs)
    {
        lock (_lock)
        {
            if (SelectedToken != null)
            {
                var newZLevel = SelectedToken.ZLevel;

                if (eventArgs.ZLevelDirection == ZLevelDirection.Front)
                {
                    var maxZLevel = TokenList.Max(t => t.ZLevel);
                    newZLevel = maxZLevel + 1;
                }
                else
                {
                    var minZLevel = TokenList.Min(t => t.ZLevel);
                    newZLevel = minZLevel - 1;
                }

                if (newZLevel != SelectedToken.ZLevel)
                {
                    SelectedToken.ZLevel = newZLevel;
                    CreateTokenBitmap();
                }
            }
        }
    }
}
