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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels;

public class TokenControllerViewModel : ControllerViewModelBase, ITokenLinker
{
    private IWindowService _windowService;
    private IWebCommunication _webCommunication;
    private Bitmap _tokenBitmap;
    private IMonsterTokens _monsterTokens;
    private IPlayers _players;
    private Settings _settings;
    private static object _lock = new();
    private bool _changingInitiative = false;
    private bool _blockTokenBitmapCreation = false;
    private TokenListItemMultiActions _tokenListItemMultiActions;
    private Dictionary<TokenListItem, Bitmap> _tokenBitmapDictionary;

    public TokenControllerViewModel()
    {
        // This is required to render MainWindow in editor
        IO.Initialize(new Directory(), new File(), new ZipFile());
        Initialize();
    }

    public TokenControllerViewModel(IWindowService windowService, IWebCommunication webCommunication, IMapSize mapSize, IMonsterTokens monsterTokens, IPlayers players, Settings settings) : base(mapSize)
    {
        Initialize();
        _windowService = windowService;
        _webCommunication = webCommunication;
        _monsterTokens = monsterTokens;
        _players = players;
        _players.OnOrientationChanged += TokensOrientationChanged;
        _webCommunication.OnMoveToken += MoveToken;
        _webCommunication.OnToggleCondition += ToggleCondition;
        _webCommunication.OnGetConditions += GetConditions;
        _webCommunication.OnSetHeight += SetHeight;
        _settings = settings;
        _settings.OnSettingChanged += SettingChanged;
        _mapSize.OnGridSizeChanged += GridSizeChanged;
        HideDungeonMasterFeatures = _settings.HideDungeonMasterFeatures;
    }

    private void Initialize()
    {
        TokenBitmap = BitmapTools.CreateEmptyBitmap();
        TokenSelectionBitmapSource = BitmapTools.CreateEmptyBitmap().ToBitmapImage();
        _tokenListItemMultiActions = new TokenListItemMultiActions(() => SelectedTokens);
        _tokenListItemMultiActions.OnConditionsChanged += TokenConditionsChanged;
        _tokenBitmapDictionary = new Dictionary<TokenListItem, Bitmap>();
        MouseCanvas = new MouseCanvasViewModel();
        MouseCanvas.OnLeftButtonDown += MouseLeftButtonDown;
        MouseCanvas.OnRightButtonDown += MouseRightButtonDown;
        MouseCanvas.OnFixRatioRectangleAreaSelected += FixRatioRectangleAreaSelected;
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
        SelectedItemsChangedCommand = new RelayCommand(p => SelectedItemsChanged((SelectionChangedEventArgs)p));
    }

    public event EventHandler OnTokenBitmapUpdated;
    public event EventHandler<ZoomAndEnhanceEventArgs> OnZoomAndEnhance;
    public event EventHandler<ToggleFogEventArgs> OnToggleFog;

    public StatblocksViewModel StatblocksViewModel { get; set; } = new();
    public CommandHistory<TokenMoveCommand> TokenMoveHistory { get; set; } = new(30);
    public BitmapSource TokenBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public BitmapSource TokenSelectionBitmapSource { get => Get<BitmapSource>(); set => Set(value); }

    public TokenListItem SelectedToken
    {
        get => Get<TokenListItem>();
        set => Set(value, () =>
        {
            NotifyPropertyChange(nameof(IsInitiativeUpButtonEnabled));
            NotifyPropertyChange(nameof(IsInitiativeDownButtonEnabled));
        });
    }
    public List<TokenListItem> SelectedTokens { get => Get<List<TokenListItem>>(); set => Set(value); }
    public bool IsInitiativeUpButtonEnabled { get => IsInitiativeUpAllowed(SelectedToken); }
    public bool IsInitiativeDownButtonEnabled { get => IsInitiativeDownAllowed(SelectedToken); }
    public bool HideDungeonMasterFeatures { get => Get<bool>(); set => Set(value); }
    public ObservableCollection<TokenListItem> TokenList { get; set; } = new ObservableCollection<TokenListItem>();
    public BitmapSource MapArrowUpBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Up).ToBitmapImage(); }
    public BitmapSource MapArrowDownBitmapSource { get => BitmapTools.CreateArrowButton(ArrowDirection.Down).ToBitmapImage(); }
    public BitmapSource UndoBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.UndoIcon.png")).ToBitmapImage(); }
    public BitmapSource RedoBitmapSource { get => IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.RedoIcon.png")).ToBitmapImage(); }
    public MouseCanvasViewModel MouseCanvas { get => Get<MouseCanvasViewModel>(); private set => Set(value); }

    public ICommand AddTokenCommand { get; set; }
    public ICommand RemoveTokenCommand { get; set; }
    public ICommand ClearTokensCommand { get; set; }
    public ICommand TokenUpCommand { get; set; }
    public ICommand TokenDownCommand { get; set; }
    public ICommand CustomTokensCommand { get; set; }
    public ICommand SortInitiativeCommand { get; set; }
    public ICommand UndoTokenMoveCommand { get; set; }
    public ICommand RedoTokenMoveCommand { get; set; }
    public ICommand SelectedItemsChangedCommand { get; set; }

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
                    var tokenListItem = new TokenListItem(token, this, _players, _tokenListItemMultiActions);
                    tokenListItem.OnTokenChanged += TokenChanged;
                    tokenListItem.OnConditionsChanged += TokenConditionsChanged;
                    tokenListItem.OnZLevelChanged += ZLevelChanged;
                    tokenListItem.Id = GetUniqueId(token.Name);
                    tokenListItem.Position = CalculateStartPosition(index);

                    SetPlayerProperties(tokenListItem);
                    TokenList.Add(tokenListItem);
                    StatblocksViewModel.AddToken(tokenListItem);
                }

                SelectedToken = TokenList.Last();
                CreateTokenBitmapFromCache();
            }
        }
    }

    public void RemoveToken()
    {
        lock (_lock)
        {
            if (SelectedToken != null)
            {
                foreach (var tokenListItem in SelectedTokens)
                {
                    if (TokenList.Count(t => t.Token.Name == SelectedToken.Token.Name) == 1)
                    {
                        StatblocksViewModel.RemoveToken(SelectedToken);
                    }

                    SelectedToken.Dispose();
                    TokenList.Remove(SelectedToken);
                }
                CreateTokenBitmapFromCache();
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
            TokenMoveHistory.Clear();
            CreateTokenBitmap();
        }
    }

    public void InitiativeUp()
    {
        lock (_lock)
        {
            _changingInitiative = true;
            var selectedTokens = SelectedTokens;

            foreach (var tokenListItem in selectedTokens.OrderByList(TokenList))
            {
                if (IsInitiativeUpAllowed(tokenListItem))
                {
                    var index = TokenList.IndexOf(tokenListItem);
                    TokenList.Remove(tokenListItem);
                    TokenList.Insert(index - 1, tokenListItem);
                }
            }

            SelectedTokens = selectedTokens;
            _changingInitiative = false;
        }
    }

    public void InitiativeDown()
    {
        lock (_lock)
        {
            _changingInitiative = true;
            var selectedTokens = SelectedTokens;

            foreach (var tokenListItem in selectedTokens.OrderByDescendingList(TokenList))
            {
                if (IsInitiativeDownAllowed(tokenListItem))
                {
                    var index = TokenList.IndexOf(tokenListItem);
                    TokenList.Remove(tokenListItem);
                    TokenList.Insert(index + 1, tokenListItem);
                }
            }

            SelectedTokens = selectedTokens;
            _changingInitiative = false;
        }
    }

    public Bitmap GetTokenBitmap()
    {
        lock (_lock)
        {
            return TokenBitmap;
        }
    }

    private void GridSizeChanged(object? sender, EventArgs e)
    {
        lock (_lock)
        {
            if (TokenList.Count > 0)
            {
                CreateTokenBitmap();
            }
        }
    }

    public override void Move(ArrowDirection direction, int movementCount)
    {
        lock (_lock)
        {
            var distance = _mapSize.GridSize * movementCount;
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
                if (token.LinkableObject.IsLinked())
                {
                    var objectLink = new ObjectLink
                    {
                        LinkableObjectType = typeof(TokenListItem),
                        Index = index,
                        TokenIdentifier = token.LinkableObject.GetLinkIdentifier()
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
                tokenListItem.SetInterfaces(this, _players, _tokenListItemMultiActions);
                tokenListItem.OnTokenChanged += TokenChanged;
                tokenListItem.OnConditionsChanged += TokenConditionsChanged;
                tokenListItem.OnZLevelChanged += ZLevelChanged;
                tokenListItem.Health.InitializeEditorHp();

                SetPlayerProperties(tokenListItem);
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
                    LinkToToken(TokenList[objectLink.Index], objectLink.TokenIdentifier);
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

                    double halfWidth = _mapSize.Width / 2;
                    double halfHeight = _mapSize.Height / 2;

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
        // An object can't link to itself
        var tokenList = TokenList.Where(t => t != linkableObject).ToList();
        var token = TokenList.SingleOrDefault(t => t == linkableObject);

        // Object A cannot be linked to object B when B is already linked to A
        if(linkableObject is TokenListItem tokenListItem)
        {
            tokenList = tokenList.Where(t => !tokenListItem.LinkedObjects.Contains(t.LinkableObject)).ToList();
        }

        var listSelectionWindowViewModel = new ListSelectionWindowViewModel<TokenListItem>(tokenList);
        _windowService.ShowWindowDialog<ListSelectionWindow>(listSelectionWindowViewModel);

        if (listSelectionWindowViewModel.Success)
        {
            linkableObject.LinkableObject.Link(listSelectionWindowViewModel.SelectedItem);
            listSelectionWindowViewModel.SelectedItem.LinkedObjects.Add(linkableObject.LinkableObject);
        }
    }

    public void LinkToToken(ILinkableObject linkableObject, TokenIdentifier tokenIdentifier)
    {
        var tokenListItem = TokenList.SingleOrDefault(t => t.GetTokenIdentifier().Equals(tokenIdentifier));
        if (tokenListItem != null)
        {
            linkableObject.LinkableObject.Link(tokenListItem);
            tokenListItem.LinkedObjects.Add(linkableObject.LinkableObject);
        }
    }

    protected override void CreateBitmap()
    {
        CreateTokenBitmap();
    }

    private void MouseLeftButtonDown(object? sender, MouseButtonDataEventArgs e)
    {
        lock (_lock)
        {
            if(_pressedKeys.Contains(Key.LeftAlt) || _pressedKeys.Contains(Key.RightAlt))
            {
                SelectToken(e);
            }
            else
            {
                ToggleFogOfWar(e);
            }
        }
    }

    private void ToggleFogOfWar(MouseButtonDataEventArgs e)
    {
        OnToggleFog.Invoke(this, new ToggleFogEventArgs() { position = e.Position });
    }

    private void SelectToken(MouseButtonDataEventArgs e)
    {
        if (SelectedToken != null)
        {
            var newPosition = new Point<double>
            {
                X = e.Position.X.Map(0, _mapSize.CanvasWidth, 0, _mapSize.Width),
                Y = e.Position.Y.Map(0, _mapSize.CanvasHeight, 0, _mapSize.Height)
            };

            var gridSize = _mapSize.GridSize;
            var gridOffset = Point<double>.Create(Mathematics.CalculateGridOffset(gridSize));
            var cellX = Math.Floor((SelectedToken.Position.X - gridOffset.X) / gridSize);
            var cellY = Math.Floor((SelectedToken.Position.Y - gridOffset.Y) / gridSize);
            var newCellX = Math.Floor((newPosition.X - gridOffset.X) / gridSize);
            var newCellY = Math.Floor((newPosition.Y - gridOffset.Y) / gridSize);

            var cellOffset = new Point<double>(newCellX - cellX, newCellY - cellY);
            var offset = new Point<double>(cellOffset.X * gridSize, cellOffset.Y * gridSize);

            // Update other selected tokens
            _blockTokenBitmapCreation = true;
            foreach (var tokenListItem in SelectedTokens)
            {
                tokenListItem.UpdatePosition(Point<int>.Create(offset));
            }
            _blockTokenBitmapCreation = false;

            TokenMoveHistory.Enqueue(new TokenMoveCommand(SelectedTokens.Select(t => t.GetTokenIdentifier()).ToList(), Point<int>.Create(cellOffset)));
            CreateTokenBitmapFromCache();
        }
    }

    private void MouseRightButtonDown(object? sender, MouseButtonDataEventArgs e)
    {
        lock (_lock)
        {
            var position = new Point<double>
            {
                X = e.Position.X.Map(0, _mapSize.CanvasWidth, 0, _mapSize.Width),
                Y = e.Position.Y.Map(0, _mapSize.CanvasHeight, 0, _mapSize.Height)
            };

            var gridSize = _mapSize.GridSize;
            var gridOffset = Point<double>.Create(Mathematics.CalculateGridOffset(gridSize));
            var bottomRight = new Point<double>
            {
                X = position.X + gridSize - ((position.X - gridOffset.X) % gridSize),
                Y = position.Y + gridSize - ((position.Y - gridOffset.Y) % gridSize)
            };

            var foundTokens = new List<TokenListItem>();

            foreach (var tokenListItem in TokenList)
            {
                var sizeFactor = Math.Max(1, tokenListItem.Token.GetSizeFactor());
                var topLeft = new Point<double>(bottomRight.X - (sizeFactor * gridSize), bottomRight.Y - (sizeFactor * gridSize));

                if ((tokenListItem.Position.X > topLeft.X && tokenListItem.Position.X < bottomRight.X) &&
                    (tokenListItem.Position.Y > topLeft.Y && tokenListItem.Position.Y < bottomRight.Y))
                {
                    foundTokens.Add(tokenListItem);
                }
            }

            if (foundTokens.Count > 0)
            {
                SelectedTokens = new() { foundTokens.OrderBy(t => t.ZLevel).Last() };
            }
        }
    }

    private void MoveToken(object? sender, MoveTokenEventArgs e)
    {
        lock (_lock)
        {
            TokenListItem? tokenListItem = TokenList.SingleOrDefault(t => t.GetTokenIdentifier().Equals(e.TokenIdentifier));
            if (tokenListItem != null)
            {
                var offset = new Point<int>();
                var gridSize = _mapSize.GridSize;
                switch (e.Direction)
                {
                    case Direction.North:
                        offset.Y -= gridSize;
                        break;
                    case Direction.NorthEast:
                        offset.X += gridSize;
                        offset.Y -= gridSize;
                        break;
                    case Direction.East:
                        offset.X += gridSize;
                        break;
                    case Direction.SouthEast:
                        offset.X += gridSize;
                        offset.Y += gridSize;
                        break;
                    case Direction.South:
                        offset.Y += gridSize;
                        break;
                    case Direction.SouthWest:
                        offset.X -= gridSize;
                        offset.Y += gridSize;
                        break;
                    case Direction.West:
                        offset.X -= gridSize;
                        break;
                    case Direction.NorthWest:
                        offset.X -= gridSize;
                        offset.Y -= gridSize;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unknown direction: {e.Direction}");
                }

                _blockTokenBitmapCreation = true;
                tokenListItem.UpdatePosition(offset);
                _blockTokenBitmapCreation = false;

                TokenMoveHistory.Enqueue(new TokenMoveCommand(tokenListItem.GetTokenIdentifier(), new Point<int>(offset.X / gridSize, offset.Y / gridSize)));           
                CreateTokenBitmapFromCache();
            }
        }
    }

    private void ToggleCondition(object? sender, ToggleConditionEventArgs e)
    {
        lock (_lock)
        {
            TokenListItem? tokenListItem = TokenList.SingleOrDefault(t => t.GetTokenIdentifier().Equals(e.TokenIdentifier));
            if (tokenListItem != null)
            {
                tokenListItem.ToggleCondition(e.Condition);
                _webCommunication.SendMessage(new ConditionsMessage { TokenIdentifier = e.TokenIdentifier, Conditions = tokenListItem.Conditions });
                CreateTokenBitmapFromCache();
            }
        }
    }

    private void GetConditions(object? sender, GetConditionsEventArgs e)
    {
        lock (_lock)
        {
            TokenListItem? tokenListItem = TokenList.SingleOrDefault(t => t.GetTokenIdentifier().Equals(e.TokenIdentifier));
            if (tokenListItem != null && tokenListItem.Conditions.Count > 0)
            {
                _webCommunication.SendMessage(new ConditionsMessage { TokenIdentifier = e.TokenIdentifier, Conditions = tokenListItem.Conditions });
            }
        }
    }

    private void SetHeight(object? sender, SetHeightEventArgs e)
    {
        lock (_lock)
        {
            TokenListItem? tokenListItem = TokenList.SingleOrDefault(t => t.GetTokenIdentifier().Equals(e.TokenIdentifier));
            if (tokenListItem != null)
            {
                tokenListItem.SetHeight(e.Height);
            }
        }
    }

    private void NotifyTokenBitmapUpdated()
    {
        OnTokenBitmapUpdated?.Invoke(this, new EventArgs());
    }

    private Point<int> CalculateStartPosition(int index)
    {
        var position = new Point<int>(_mapSize.Width / 2, _mapSize.Height / 2);
        var gridSize = _mapSize.GridSize;
        var maxGridCellsX = position.X + (gridSize / 2);
        maxGridCellsX /= gridSize;
        var maxGridCellsY = position.Y + (gridSize / 2);
        maxGridCellsY /= gridSize;

        if (index < maxGridCellsX * maxGridCellsY)
        {
            var gridCellsY = index / maxGridCellsX;
            var gridcellsX = index - (gridCellsY * maxGridCellsX);

            position.X += gridcellsX * gridSize;
            position.Y += gridCellsY * gridSize;
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
        if (_pauseBitmapCreation)
            return;

        lock (_lock)
        {
            _tokenBitmapDictionary.Clear();
            DrawBitmap();
        }
    }

    private void CreateTokenBitmapFromCache()
    {
        if (_pauseBitmapCreation)
            return;

        lock (_lock)
        {
            var tokenBitmap = BitmapTools.CreateEmptyBitmap();

            if (TokenList.Count > 0)
            {
                // Remove bitmaps for removed tokens
                foreach (var bitmap in _tokenBitmapDictionary)
                {
                    if(TokenList.SingleOrDefault(t => t.GetTokenIdentifier() == bitmap.Key.GetTokenIdentifier()) == null)
                    {
                        _tokenBitmapDictionary.Remove(bitmap.Key);
                    }
                }
            }

            DrawBitmap();
        }
    }

    private void DrawBitmap()
    {
        if (!_blockTokenBitmapCreation)
        {
            var tokenBitmap = BitmapTools.CreateEmptyBitmap();

            if (TokenList.Count > 0)
            {
                _tokenBitmapDictionary.Clear();

                // Create bitmaps for added or updated tokens
                foreach (var tokenListItem in TokenList)
                {
                    if (!_tokenBitmapDictionary.ContainsKey(tokenListItem))
                    {
                        var bitmap = BitmapTools.CreateEmptyBitmap();
                        BitmapTools.DrawToken(bitmap, tokenListItem, GetTokenIdString(tokenListItem), _mapSize.GridSize);
                        _tokenBitmapDictionary[tokenListItem] = bitmap;
                    }
                }

                // Create token bitmap
                var tokenBitmaps = new List<Bitmap>();
                foreach (var tokenListItem in TokenList.Where(t => t.Visible).OrderBy(t => t.ZLevel))
                {
                    tokenBitmaps.Add(_tokenBitmapDictionary[tokenListItem]);
                }
                tokenBitmap = BitmapTools.MergeBitmaps(tokenBitmaps.ToArray());

                UpdateTokenSelection();
            }
            else
            {
                _tokenBitmapDictionary.Clear();
                TokenSelectionBitmapSource = BitmapTools.CreateEmptyBitmap().ToBitmapImage();
            }

            TokenBitmap = tokenBitmap;
            NotifyTokenBitmapUpdated();
        }
    }

    public bool GetOverviewBitmap(double zoomFactor, out OverviewBitmap overviewBitmap)
    {
        lock (_lock)
        {
            overviewBitmap = new OverviewBitmap();
            if (TokenList.Count > 0)
            {
                var tokenListWithNormilizedPositions = new Dictionary<TokenListItem, Point<int>>();

                foreach (var tokenListItem in TokenList.OrderBy(t => t.ZLevel))
                {
                    var normilizedPosition = NormilizePositionToGrid(tokenListItem, _mapSize.GridSize);
                    tokenListWithNormilizedPositions[tokenListItem] = new Point<int>(
                        (int)Math.Round(normilizedPosition.X * zoomFactor),
                        (int)Math.Round(normilizedPosition.Y * zoomFactor));
                }

                var gridSize = _mapSize.GridSize * zoomFactor;
                overviewBitmap.Bitmap = BitmapTools.CreateTokenOverviewBitmap(tokenListWithNormilizedPositions, (int)Math.Round(gridSize));

                // OffsetFromOrigin = top left of player view to top left of token bounding box
                // Token positions are always relative to top left of the player view (=origin)
                var halfGridSize = (int)Math.Round(gridSize / 2);
                var minTokenPositionX = tokenListWithNormilizedPositions.Min(kv => kv.Value.X - (kv.Key.Token.GetSizeFactor() * halfGridSize));
                var minTokenPositionY = tokenListWithNormilizedPositions.Min(kv => kv.Value.Y - (kv.Key.Token.GetSizeFactor() * halfGridSize));
                overviewBitmap.OffsetFromOrigin = new Point<int>((int)Math.Round(minTokenPositionX), (int)Math.Round(minTokenPositionY));

                return true;
            }

            return false;
        }
    }

    public Point<int> NormilizePositionToGrid(TokenListItem tokenListItem, int gridSize)
    {
        // A position of a token can be anywhere in a grid cell. 
        // This function calculates the position of the center
        // of the grid cell that the token is positioned in.

        var normilizedPosition = new Point<int>();
        var gridOffset = Mathematics.CalculateGridOffset(gridSize);

        var distanceToGridCellBorderX = Mathematics.Modulo<int>(tokenListItem.Position.X - gridOffset.X, gridSize);
        var distanceToGridCellBorderY = Mathematics.Modulo<int>(tokenListItem.Position.Y - gridOffset.Y, gridSize);
        var halfTokenSize = (int)Math.Round(gridSize * tokenListItem.Token.GetSizeFactor() / 2);
        
        // The minimum should be half a grid size in order to center small and tiny tokens
        var distanceToCenterOfToken = Math.Max(halfTokenSize, gridSize / 2);

        normilizedPosition.X = tokenListItem.Position.X - distanceToGridCellBorderX + distanceToCenterOfToken;
        normilizedPosition.Y = tokenListItem.Position.Y - distanceToGridCellBorderY + distanceToCenterOfToken;

        return normilizedPosition;
    }

    public bool IsAnyTokenControlledByPlayer()
    {
        foreach (var tokenListItem in TokenList)
        {
            if(_players.IsTokenControlledByPlayer(tokenListItem.GetTokenIdentifier()))
            {
                return true;
            }
        }

        return false;
    }

    private string GetTokenIdString(TokenListItem tokenListItem)
    {
        var tokenId = "";
        if (TokenList.Count(t => t.Token.Name == tokenListItem.Token.Name && t.Visible) > 1)
        {
            tokenId = tokenListItem.Id.ToString();
        }

        return tokenId;
    }

    private void TokenChanged(object? sender, EventArgs e)
    {
        var tokenListItem = (TokenListItem)sender!;
        _tokenBitmapDictionary.Remove(tokenListItem);
        CreateTokenBitmapFromCache();
    }

    private void TokenConditionsChanged(object? sender, ConditionsChangedEventArgs e)
    {
        var tokenListItem = TokenList.Single(t => t.GetTokenIdentifier().Equals(e.TokenIdentifier));
        if (_players.IsTokenControlledByPlayer(tokenListItem.GetTokenIdentifier()))
        {
            _webCommunication.SendMessage(new ConditionsMessage { TokenIdentifier = e.TokenIdentifier, Conditions = e.NewConditions });
        }
    }

    private void UpdateTokenSelection()
    {
        lock (_lock)
        {
            var bitmap = BitmapTools.CreateEmptyBitmap();
            if (SelectedToken != null)
            {
                foreach (var tokenListItem in SelectedTokens)
                {
                    BitmapTools.DrawTokenSelection(bitmap, tokenListItem.Token.GetSizeFactor(), tokenListItem.Position, _mapSize.GridSize);
                }
            }

            TokenSelectionBitmapSource = bitmap.ToBitmapImage();
        }
    }

    private void ZLevelChanged(object? sender, ZLevelChangedEventArgs eventArgs)
    {
        lock (_lock)
        {
            var tokenListItem = (TokenListItem)sender!;

            if (eventArgs.ZLevelDirection == ZLevelDirection.Front)
            {
                var maxZLevel = TokenList.Max(t => t.ZLevel);
                tokenListItem.ZLevel = maxZLevel + 1;
            }
            else
            {
                var minZLevel = TokenList.Min(t => t.ZLevel);
                tokenListItem.ZLevel = minZLevel - 1;
            }

            _tokenListItemMultiActions.ZLevelChanged(tokenListItem);
            CreateTokenBitmapFromCache();
        }
    }

    private void Undo()
    {
        lock (_lock)
        {
            if (TokenMoveHistory.TryDequeuePreviousCommand(out var tokenMoveCommand))
            {
                var offset = new Point<int>(-tokenMoveCommand.Offset.X, -tokenMoveCommand.Offset.Y);
                foreach (var tokenIdentifier in tokenMoveCommand.TokenIdentifiers)
                {
                    MoveTokenWithCellOffset(tokenIdentifier, offset);
                }
            }
        }
    }

    private void Redo()
    {
        lock (_lock)
        {
            if (TokenMoveHistory.TryDequeueNextCommand(out var tokenMoveCommand))
            {
                var offset = new Point<int>(tokenMoveCommand.Offset);
                foreach (var tokenIdentifier in tokenMoveCommand.TokenIdentifiers)
                {
                    MoveTokenWithCellOffset(tokenIdentifier, offset);
                }
            }
        }
    }

    private void MoveTokenWithCellOffset(TokenIdentifier tokenIdentifier, Point<int> cellOffset)
    {
        var tokenListItem = TokenList.SingleOrDefault(t => t.GetTokenIdentifier().Equals(tokenIdentifier));
        if (tokenListItem != null)
        {
            var offset = new Point<int>(cellOffset.X * _mapSize.GridSize, cellOffset.Y * _mapSize.GridSize);

            _blockTokenBitmapCreation = true;
            tokenListItem.UpdatePosition(offset);
            _blockTokenBitmapCreation = false;

            CreateTokenBitmapFromCache();
        }
    }

    private void SelectedItemsChanged(SelectionChangedEventArgs eventArgs)
    {
        foreach (var tokenListItem in TokenList)
        {
            if (SelectedToken == null || !SelectedToken.GetTokenIdentifier().Equals(tokenListItem.GetTokenIdentifier()))
            {
                tokenListItem.AreConditionsVisible = false;
            }
        }

        if (eventArgs.AddedItems.Count > 0 || eventArgs.RemovedItems.Count > 0)
        {
            if (!_changingInitiative)
            {
                UpdateTokenSelection();
            }
        }
    }

    private bool IsInitiativeUpAllowed(TokenListItem tokenListItem)
    {
        return TokenList.IndexOf(tokenListItem) > 0;
    }

    private bool IsInitiativeDownAllowed(TokenListItem tokenListItem)
    {
        return TokenList.IndexOf(tokenListItem) != -1 && TokenList.IndexOf(tokenListItem) < TokenList.Count - 1;
    }

    private void SettingChanged(object? sender, SettingChangedEventArgs e)
    {
        if (e.SettingName == nameof(Settings.HideDungeonMasterFeatures))
        {
            HideDungeonMasterFeatures = _settings.HideDungeonMasterFeatures;
        }
    }

    private void TokensOrientationChanged(object? sender, TokensOrientationChangedEventArgs e)
    {
        lock (_lock)
        {
            foreach (var tokenIdentifier in e.TokenIdentifiers)
            {
                var tokenListItem = TokenList.SingleOrDefault(t => t.GetTokenIdentifier().Equals(tokenIdentifier));
                if (tokenListItem != null)
                {
                    tokenListItem.Token.Orientation = e.Orientation;
                    InvalidateTokenListItemBitmap(tokenListItem);
                }
            }
            CreateTokenBitmapFromCache();
        }
    }

    private void SetPlayerProperties(TokenListItem tokenListItem)
    {
        var identifier = tokenListItem.GetTokenIdentifier();
        if (_players.IsTokenControlledByPlayer(identifier))
        {
            if (_players.TryGetOrientation(identifier, out var orientation))
            {
                tokenListItem.Token.Orientation = orientation;
            }
            _webCommunication.SendMessage(new ConditionsMessage { TokenIdentifier = identifier, Conditions = tokenListItem.Conditions });
        }
    }

    private void FixRatioRectangleAreaSelected(object? sender, RectangleF rectangle)
    {
        OnZoomAndEnhance?.Invoke(this, new ZoomAndEnhanceEventArgs() { rectangle = rectangle });
    }

    private void InvalidateTokenListItemBitmap(TokenListItem tokenListItem)
    {
        _tokenBitmapDictionary.Remove(tokenListItem);
    }
}
