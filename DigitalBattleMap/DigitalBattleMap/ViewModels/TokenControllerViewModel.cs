using DigitalBattleMap.Common;
using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels;

public class TokenControllerViewModel : ControllerViewModelBase, ITokenLinker
{
    private IWindowService _windowService;
    private IWebCommunication _webCommunication;
    private IMouseCanvas _mouseCanvas;
    private Bitmap _tokenBitmap;
    private IMonsterTokens _monsterTokens;
    private IPlayerJoiner _playerJoiner;
    private Settings _settings;
    private static object _lock = new();

    public TokenControllerViewModel()
    {
        // This is required to render MainWindow in editor
        IO.Initialize(new Directory(), new File(), new ZipFile());
        Initialize();
    }

    public TokenControllerViewModel(IWindowService windowService, IWebCommunication webCommunication, ICanvasSize canvasSize, IMouseCanvas mouseCanvas, IMonsterTokens monsterTokens, IPlayerJoiner playerJoiner, Settings settings, int gridSize) : base(canvasSize, gridSize)
    {
        _windowService = windowService;
        _webCommunication = webCommunication;
        _mouseCanvas = mouseCanvas;
        _monsterTokens = monsterTokens;
        _playerJoiner = playerJoiner;
        _webCommunication.OnMoveToken += MoveToken;
        _webCommunication.OnToggleCondition += ToggleCondition;
        _webCommunication.OnGetConditions += GetConditions;
        _mouseCanvas.SubscribeMouseDown(TabIndex.Tokens, MouseDown);
        _settings = settings;
        Initialize();
    }

    private void Initialize()
    {
        TokenBitmap = BitmapTools.CreateEmptyBitmap();
        TokenSelectionBitmapSource = BitmapTools.CreateEmptyBitmap().ToBitmapImage();
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
        UndoTokenMoveCommand = new RelayCommand(p => Undo());
        RedoTokenMoveCommand = new RelayCommand(p => Redo());
    }

    public event EventHandler OnTokenBitmapUpdated;

    public StatblocksViewModel StatblocksViewModel { get; set; } = new();
    public CommandHistory<TokenOffset> TokenOffsetHistory { get; set; } = new(10);
    public BitmapSource TokenBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource TokenSelectionBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public TokenListItem SelectedToken
    {
        get => Get<TokenListItem>();
        set => Set(value, () =>
        {
            UpdateTokenSelection();
            NotifyPropertyChange(nameof(IsTokenUpButtonEnabled));
            NotifyPropertyChange(nameof(IsTokenDownButtonEnabled));
        });
    }
    public bool IsTokenUpButtonEnabled { get => TokenList.IndexOf(SelectedToken) > 0; }
    public bool IsTokenDownButtonEnabled { get => TokenList.IndexOf(SelectedToken) < TokenList.Count - 1; }
    public ObservableCollection<TokenListItem> TokenList { get; set; } = new ObservableCollection<TokenListItem>();
    public BitmapSource MapArrowUpBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Up).ToBitmapImage(); }
    public BitmapSource MapArrowDownBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Down).ToBitmapImage(); }
    public BitmapSource UndoBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.UndoIcon.png")).ToBitmapImage(); }
    public BitmapSource RedoBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.RedoIcon.png")).ToBitmapImage(); }

    public ICommand AddTokenCommand { get; set; }
    public ICommand RemoveTokenCommand { get; set; }
    public ICommand ClearTokensCommand { get; set; }
    public ICommand TokenUpCommand { get; set; }
    public ICommand TokenDownCommand { get; set; }
    public ICommand CustomTokensCommand { get; set; }
    public ICommand SortInitiativeCommand { get; set; }
    public ICommand UndoTokenMoveCommand { get; set; }
    public ICommand RedoTokenMoveCommand { get; set; }

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

    public void AddToken()
    {
        lock (_lock)
        {
            var tokens = new List<Token>(_monsterTokens.GetTokens().Clone());
            tokens.AddRange(_settings.CustomTokens.Clone());

            var selectTokenWindowViewModel = new SelectTokenWindowViewModel(tokens, _settings.TokenGroups);
            _windowService.ShowWindowDialog<SelectTokenWindow>(selectTokenWindowViewModel);

            if (selectTokenWindowViewModel.AddedTokens.Count > 0)
            {
                foreach (var (token, index) in selectTokenWindowViewModel.AddedTokens.WithIndex())
                {
                    var tokenListItem = new TokenListItem(token, this, _playerJoiner);
                    tokenListItem.Token.OnSizeChanged += TokenChanged;
                    tokenListItem.OnTokenChanged += TokenChanged;
                    tokenListItem.OnConditionsChanged += TokenConditionsChanged;
                    tokenListItem.OnZLevelChanged += ZLevelChanged;
                    tokenListItem.Id = GetUniqueId(token.Name);
                    tokenListItem.Position = CalculateStartPosition(index);

                    TokenList.Add(tokenListItem);
                    StatblocksViewModel.AddToken(tokenListItem);
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
                if (TokenList.Count(t => t.Token.Name == SelectedToken.Token.Name) == 1)
                {
                    StatblocksViewModel.RemoveToken(SelectedToken);
                }

                SelectedToken.Dispose();
                TokenList.Remove(SelectedToken);
                CreateTokenBitmap();
            }
        }
    }

    public void ClearTokens()
    {
        lock (_lock)
        {
            foreach (var tokenListItem in TokenList)
            {
                tokenListItem.Dispose();
            }
            TokenList.Clear();
            StatblocksViewModel.Clear();
            TokenOffsetHistory.Clear();
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

    private void MouseDown(Point<double> point)
    {
        lock (_lock)
        {
            if (SelectedToken != null)
            {
                var newPosition = new Point<double>
                {
                    X = point.X.Map(0, _canvasSize.Width, 0, Constants.BitmapSize.Width),
                    Y = point.Y.Map(0, _canvasSize.Height, 0, Constants.BitmapSize.Height)
                };

                var gridOffset = Point<double>.Create(BitmapTools.CalculateGridOffset(_gridSize));
                var cellX = Math.Floor((SelectedToken.Position.X - gridOffset.X) / _gridSize);
                var cellY = Math.Floor((SelectedToken.Position.Y - gridOffset.Y) / _gridSize);
                var newCellX = Math.Floor((newPosition.X - gridOffset.X) / _gridSize);
                var newCellY = Math.Floor((newPosition.Y - gridOffset.Y) / _gridSize);

                var cellOffset = new Point<double>(newCellX - cellX, newCellY - cellY);
                var offset = new Point<double>(cellOffset.X * _gridSize, cellOffset.Y * _gridSize);
                foreach (var linkedObject in SelectedToken.LinkedObjects)
                {
                    linkedObject.UpdatePosition(Point<int>.Create(offset));
                }

                SelectedToken.Position = Point<int>.Create(newPosition);
                TokenOffsetHistory.Enqueue(new TokenOffset(SelectedToken.GetTokenIndentifier(), Point<int>.Create(cellOffset)));
                CreateTokenBitmap();
            }
        }
    }

    public override void Move(ArrowDirection direction, int movementCount)
    {
        lock (_lock)
        {
            var distance = _gridSize * movementCount;
            foreach (var tokenListItem in TokenList)
            {
                switch (direction)
                {
                    case ArrowDirection.Up:
                        tokenListItem.Position.Y += distance;
                        break;
                    case ArrowDirection.Down:
                        tokenListItem.Position.Y -= distance;
                        break;
                    case ArrowDirection.Left:
                        tokenListItem.Position.X += distance;
                        break;
                    case ArrowDirection.Right:
                        tokenListItem.Position.X -= distance;
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
            foreach ((var token, var index) in TokenList.WithIndex())
            {
                if (token.IsLinked())
                {
                    var objectLink = new ObjectLink
                    {
                        LinkableObjectType = typeof(TokenListItem),
                        Index = index,
                        TokenIndentifier = token.GetLinkIdentifier()
                    };
                    saveFile.ObjectLinks.Add(objectLink);
                }
            }
        }
    }

    public override void OpenSaveFile(SaveFile saveFile)
    {
        lock (_lock)
        {
            ClearTokens();
            foreach (var tokenListItem in saveFile.TokenList)
            {
                tokenListItem.Token.OnSizeChanged += TokenChanged;
                tokenListItem.OnTokenChanged += TokenChanged;
                tokenListItem.OnConditionsChanged += TokenConditionsChanged;
                tokenListItem.OnZLevelChanged += ZLevelChanged;
                tokenListItem.Health.InitializeEditorHp();
                tokenListItem.SetInterfaces(this, _playerJoiner);

                if (tokenListItem.Token.PlayerControl)
                {
                    _webCommunication.SendMessage(new ConditionsMessage { TokenIndentifier = tokenListItem.GetTokenIndentifier(), Conditions = tokenListItem.Conditions });
                }

                TokenList.Add(tokenListItem);
                StatblocksViewModel.AddToken(tokenListItem);
            }

            CreateTokenBitmap();
        }
    }

    public void OpenObjectLinks(List<ObjectLink> objectLinks)
    {
        lock (_lock)
        {
            foreach (var objectLink in objectLinks)
            {
                if (objectLink.LinkableObjectType == typeof(TokenListItem))
                {
                    LinkToToken(TokenList[objectLink.Index], objectLink.TokenIndentifier);
                }
            }
        }
    }

    public void CustomTokens()
    {
        lock (_lock)
        {
            var customTokensWindowViewModel = new CustomTokensWindowViewModel(_windowService, _monsterTokens, _settings);
            _windowService.ShowWindowDialog<CustomTokensWindow>(customTokensWindowViewModel);
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

                    double halfWidth = Constants.BitmapSize.Width / 2;
                    double halfHeight = Constants.BitmapSize.Height / 2;

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

    public void LinkToToken(ILinkableObject linkableObject)
    {
        var listSelectionWindowViewModel = new ListSelectionWindowViewModel<TokenListItem>(TokenList);
        _windowService.ShowWindowDialog<ListSelectionWindow>(listSelectionWindowViewModel);

        if (listSelectionWindowViewModel.Success)
        {
            if (listSelectionWindowViewModel.SelectedItem != linkableObject)
            {
                linkableObject.Link(listSelectionWindowViewModel.SelectedItem);
                listSelectionWindowViewModel.SelectedItem.LinkedObjects.Add(linkableObject);
            }
        }
    }

    public void LinkToToken(ILinkableObject linkableObject, TokenIndentifier tokenIndentifier)
    {
        var tokenListItem = TokenList.SingleOrDefault(t => t.GetTokenIndentifier().Equals(tokenIndentifier));
        if (tokenListItem != null)
        {
            linkableObject.Link(tokenListItem);
            tokenListItem.LinkedObjects.Add(linkableObject);
        }
    }

    private void MoveToken(object sender, MoveTokenEventArgs e)
    {
        lock (_lock)
        {
            TokenListItem? tokenListItem = TokenList.SingleOrDefault(t => t.GetTokenIndentifier().Equals(e.TokenIndentifier) && t.Token.PlayerControl);
            if (tokenListItem != null)
            {
                var offset = new Point<int>();
                switch (e.Direction)
                {
                    case Direction.North:
                        offset.Y -= _gridSize;
                        break;
                    case Direction.NorthEast:
                        offset.X += _gridSize;
                        offset.Y -= _gridSize;
                        break;
                    case Direction.East:
                        offset.X += _gridSize;
                        break;
                    case Direction.SouthEast:
                        offset.X += _gridSize;
                        offset.Y += _gridSize;
                        break;
                    case Direction.South:
                        offset.Y += _gridSize;
                        break;
                    case Direction.SouthWest:
                        offset.X -= _gridSize;
                        offset.Y += _gridSize;
                        break;
                    case Direction.West:
                        offset.X -= _gridSize;
                        break;
                    case Direction.NorthWest:
                        offset.X -= _gridSize;
                        offset.Y -= _gridSize;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unknown direction: {e.Direction}");
                }

                // Block UI thread to update everything at the same time
                var uiThread = new UIThreadResource();
                uiThread.Claim();
                var tasks = new List<Task>();

                foreach (var linkedObject in tokenListItem.LinkedObjects)
                {
                    var task = Task.Run(() => linkedObject.UpdatePosition(offset));
                    tasks.Add(task);
                }

                tokenListItem.Position.X += offset.X;
                tokenListItem.Position.Y += offset.Y;

                TokenOffsetHistory.Enqueue(new TokenOffset(tokenListItem.GetTokenIndentifier(), new Point<int>(offset.X / _gridSize, offset.Y / _gridSize)));
                CreateTokenBitmap();
                uiThread.Release();
                Task.WaitAll(tasks.ToArray());
            }
        }
    }

    private void ToggleCondition(object sender, ToggleConditionEventArgs e)
    {
        lock (_lock)
        {
            TokenListItem? tokenListItem = TokenList.SingleOrDefault(t => t.GetTokenIndentifier().Equals(e.TokenIndentifier) && t.Token.PlayerControl);
            if (tokenListItem != null)
            {
                tokenListItem.ToggleCondition(e.Condition);
                _webCommunication.SendMessage(new ConditionsMessage { TokenIndentifier = e.TokenIndentifier, Conditions = tokenListItem.Conditions });
                CreateTokenBitmap();
            }
        }
    }

    private void GetConditions(object sender, GetConditionsEventArgs e)
    {
        lock (_lock)
        {
            TokenListItem? tokenListItem = TokenList.SingleOrDefault(t => t.GetTokenIndentifier().Equals(e.TokenIndentifier) && t.Token.PlayerControl);
            if (tokenListItem != null && tokenListItem.Conditions.Count > 0)
            {
                _webCommunication.SendMessage(new ConditionsMessage { TokenIndentifier = e.TokenIndentifier, Conditions = tokenListItem.Conditions });
            }
        }
    }

    private void NotifyTokenBitmapUpdated()
    {
        OnTokenBitmapUpdated?.Invoke(this, new EventArgs());
    }

    private Point<int> CalculateStartPosition(int index)
    {
        var position = new Point<int>(Constants.BitmapSize.Width / 2, Constants.BitmapSize.Height / 2);
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
            else
            {
                TokenSelectionBitmapSource = BitmapTools.CreateEmptyBitmap().ToBitmapImage();
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

    private void TokenConditionsChanged(object? sender, ConditionsChangedEventArgs e)
    {
        var tokenListItem = TokenList.Single(t => t.GetTokenIndentifier().Equals(e.TokenIndentifier));
        if (tokenListItem.Token.PlayerControl)
        {
            _webCommunication.SendMessage(new ConditionsMessage { TokenIndentifier = e.TokenIndentifier, Conditions = e.NewConditions });
        }
    }

    private void UpdateTokenSelection()
    {
        var bitmap = BitmapTools.CreateEmptyBitmap();
        if (SelectedToken != null)
        {
            BitmapTools.DrawTokenSelection(bitmap, SelectedToken.Token.GetSizeFactor(), SelectedToken.Position, _gridSize);
        }

        TokenSelectionBitmapSource = bitmap.ToBitmapImage();
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

    private void Undo()
    {
        lock (_lock)
        {
            if (TokenOffsetHistory.TryDequeuePreviousCommand(out var tokenOffset))
            {
                var offset = new Point<int>(-tokenOffset.Offset.X, -tokenOffset.Offset.Y);
                MoveTokenWithCellOffset(tokenOffset.TokenIndentifier, offset);
            }
        }
    }

    private void Redo()
    {
        lock (_lock)
        {
            if (TokenOffsetHistory.TryDequeueNextCommand(out var tokenOffset))
            {
                var offset = new Point<int>(tokenOffset.Offset);
                MoveTokenWithCellOffset(tokenOffset.TokenIndentifier, offset);
            }
        }
    }

    private void MoveTokenWithCellOffset(TokenIndentifier tokenIndentifier, Point<int> cellOffset)
    {
        var tokenListItem = TokenList.SingleOrDefault(t => t.GetTokenIndentifier().Equals(tokenIndentifier));
        if (tokenListItem != null)
        {
            var offset = new Point<int>(cellOffset.X * _gridSize, cellOffset.Y * _gridSize);

            foreach (var linkedObject in tokenListItem.LinkedObjects)
            {
                linkedObject.UpdatePosition(Point<int>.Create(offset));
            }

            tokenListItem.Position.X += offset.X;
            tokenListItem.Position.Y += offset.Y;
            CreateTokenBitmap();
        }
    }
}
