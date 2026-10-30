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
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels;

public class TokenControllerViewModel : ControllerViewModelBase, ITokenLinker
{
    private IWindowService _windowService;
    private IWebCommunication _webCommunication;
    private Bitmap _tokenBitmap;
    private Bitmap _visibleTokensBitmap;
    private IMonsterTokens _monsterTokens;
    private IPlayers _players;
    private Settings _settings;
    private static object _lock = new();
    private bool _changingInitiative = false;
    private bool _blockTokenBitmapCreation = false;
    private bool _isMovePreviewActive = false;
    private Point<double> _movePreviewCellOffset = new();
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
        _webCommunication.OnMoveToken += MoveTokenFromWeb;
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
        _visibleTokensBitmap = BitmapTools.CreateEmptyBitmap();
        VisibleTokensBitmapSource = _visibleTokensBitmap.ToBitmapImage();
        TokenSelectionBitmapSource = BitmapTools.CreateEmptyBitmap().ToBitmapImage();
        _tokenListItemMultiActions = new TokenListItemMultiActions(() => SelectedTokens);
        _tokenListItemMultiActions.OnConditionsChanged += TokenConditionsChanged;
        _tokenBitmapDictionary = new Dictionary<TokenListItem, Bitmap>();
        MouseCanvas = new MouseCanvasViewModel();
        MouseCanvas.SetDefaultMode(MouseCanvasMode.RtsSelection);
        MouseCanvas.OnLeftButtonUp += MouseLeftButtonUp;
        MouseCanvas.OnRectangleAreaSelected += RectangleAreaSelected;
        MouseCanvas.OnRightButtonDown += MouseRightButtonDown;
        MouseCanvas.OnRightButtonUp += MouseRightButtonUp;
        MouseCanvas.OnMouseMove += MouseMove;
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
    public BitmapSource VisibleTokensBitmapSource { get; private set; }
    public BitmapSource TokenSelectionBitmapSource { get => Get<BitmapSource>(); set => Set(value); }
    public Geometry MovePreviewGeometry { get => Get<Geometry>(); private set => Set(value); }
    public string MovePreviewLabel { get => Get<string>(); private set => Set(value); }
    public double MovePreviewLabelX { get => Get<double>(); private set => Set(value); }
    public double MovePreviewLabelY { get => Get<double>(); private set => Set(value); }

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
                    var tokenListItem = new TokenListItem(token, GetUniqueId(token.Name), this, _players, _tokenListItemMultiActions);
                    tokenListItem.OnTokenChanged += TokenChanged;
                    tokenListItem.OnConditionsChanged += TokenConditionsChanged;
                    tokenListItem.OnZLevelChanged += ZLevelChanged;
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
            return _visibleTokensBitmap;
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

    private void MouseRightButtonDown(object? sender, MouseButtonDataEventArgs e)
    {
        lock (_lock)
        {
            if(_pressedKeys.Contains(Key.LeftAlt) || _pressedKeys.Contains(Key.RightAlt))
            {
                ToggleFogOfWar(e);
            }
            else if (SelectedTokens?.Count > 0)
            {
                _isMovePreviewActive = true;
                UpdateMovePreviewLabelPosition(e.Position);
                ShowMovePreview(CalculateMoveCellOffset(e.Position));
            }
        }
    }

    private void MouseRightButtonUp(object? sender, MouseButtonDataEventArgs e)
    {
        lock (_lock)
        {
            if (_isMovePreviewActive)
            {
                ClearMovePreview();
                MoveToken(e);
            }
        }
    }

    private void MouseMove(object? sender, MouseMoveDataEventArgs e)
    {
        lock (_lock)
        {
            if (!_isMovePreviewActive)
                return;

            // The right button was released outside the canvas, which cancels the move
            if (!e.RightButtonDown || !(SelectedTokens?.Count > 0))
            {
                ClearMovePreview();
                return;
            }

            UpdateMovePreviewLabelPosition(e.Position);

            var cellOffset = CalculateMoveCellOffset(e.Position);
            if (cellOffset.X != _movePreviewCellOffset.X || cellOffset.Y != _movePreviewCellOffset.Y)
            {
                ShowMovePreview(cellOffset);
            }
        }
    }

    public override void KeyDown(KeyEventArgs keyEventArgs)
    {
        base.KeyDown(keyEventArgs);

        lock (_lock)
        {
            if (keyEventArgs.Key == Key.Escape && _isMovePreviewActive)
            {
                ClearMovePreview();
            }
        }
    }

    private void ToggleFogOfWar(MouseButtonDataEventArgs e)
    {
        OnToggleFog.Invoke(this, new ToggleFogEventArgs() { position = e.Position });
    }

    private void MoveToken(MouseButtonDataEventArgs e)
    {
        if (SelectedTokens?.Count > 0)
        {
            var gridSize = _mapSize.GridSize;
            var cellOffset = CalculateMoveCellOffset(e.Position);
            if (cellOffset.X == 0 && cellOffset.Y == 0)
                return;

            var offset = new Point<double>(cellOffset.X * gridSize, cellOffset.Y * gridSize);
            var tokensToMove = GetTokensToMove();

            // Update other selected tokens
            _blockTokenBitmapCreation = true;
            foreach (var tokenListItem in tokensToMove)
            {
                tokenListItem.UpdatePosition(Point<int>.Create(offset));
            }
            _blockTokenBitmapCreation = false;

            TokenMoveHistory.Enqueue(new TokenMoveCommand(tokensToMove.Select(t => t.GetTokenIdentifier()).ToList(), Point<int>.Create(cellOffset)));
            CreateTokenBitmapFromCache();
        }
    }

    // The center of all selected tokens moves as close to the cursor as the grid allows
    private Point<double> CalculateMoveCellOffset(Point<double> canvasPosition)
    {
        var newPosition = new Point<double>
        {
            X = canvasPosition.X.Map(0, _mapSize.CanvasWidth, 0, _mapSize.Width),
            Y = canvasPosition.Y.Map(0, _mapSize.CanvasHeight, 0, _mapSize.Height)
        };

        var gridSize = _mapSize.GridSize;
        var bounds = SelectedTokens.Select(t => GetTokenBounds(t, gridSize)).ToList();
        var centerX = (bounds.Min(b => b.Left) + bounds.Max(b => b.Right)) / 2.0;
        var centerY = (bounds.Min(b => b.Top) + bounds.Max(b => b.Bottom)) / 2.0;

        return new Point<double>(
            Math.Floor(((newPosition.X - centerX) / gridSize) + 0.5),
            Math.Floor(((newPosition.Y - centerY) / gridSize) + 0.5));
    }

    // Only a preview for the DM, tokens are not moved until the right button is released
    private void ShowMovePreview(Point<double> cellOffset)
    {
        _movePreviewCellOffset = cellOffset;

        var gridSize = _mapSize.GridSize;
        var scaleX = (double)_mapSize.CanvasWidth / _mapSize.Width;
        var scaleY = (double)_mapSize.CanvasHeight / _mapSize.Height;
        var tokensToMove = GetTokensToMove();
        var geometry = new GeometryGroup();

        foreach (var tokenListItem in TokenList.Where(t => tokensToMove.Contains(t) || IsMovedByLinkedToken(t, tokensToMove)))
        {
            var bounds = GetTokenBounds(tokenListItem, gridSize);
            var centerX = ((bounds.Left + bounds.Right) / 2.0) + (cellOffset.X * gridSize);
            var centerY = ((bounds.Top + bounds.Bottom) / 2.0) + (cellOffset.Y * gridSize);
            var radius = (bounds.Right - bounds.Left) / 2.0;

            geometry.Children.Add(new EllipseGeometry(new System.Windows.Point(centerX * scaleX, centerY * scaleY), radius * scaleX, radius * scaleY));
        }

        geometry.Freeze();
        MovePreviewGeometry = geometry;

        // A diagonal step counts as one grid cell
        var gridCells = Math.Max(Math.Abs(cellOffset.X), Math.Abs(cellOffset.Y));
        MovePreviewLabel = $"{gridCells * Constants.FeetPerGridCell} ft";
    }

    private void ClearMovePreview()
    {
        _isMovePreviewActive = false;
        MovePreviewGeometry = null;
        MovePreviewLabel = null;
    }

    // Next to the cursor, on the other side near the map edges so the label is not cut off
    private void UpdateMovePreviewLabelPosition(Point<double> canvasPosition)
    {
        const double cursorOffset = 16;
        const double labelWidth = 120;
        const double labelHeight = 45;

        MovePreviewLabelX = canvasPosition.X + cursorOffset + labelWidth > _mapSize.CanvasWidth
            ? canvasPosition.X - labelWidth
            : canvasPosition.X + cursorOffset;
        MovePreviewLabelY = canvasPosition.Y + cursorOffset + labelHeight > _mapSize.CanvasHeight
            ? canvasPosition.Y - labelHeight
            : canvasPosition.Y + cursorOffset;
    }

    private List<TokenListItem> GetTokensToMove()
    {
        return SelectedTokens.Where(t => !IsMovedByLinkedToken(t, SelectedTokens)).ToList();
    }

    // The area of the grid cells used by a token
    private (double Left, double Top, double Right, double Bottom) GetTokenBounds(TokenListItem tokenListItem, int gridSize)
    {
        var center = NormilizePositionToGrid(tokenListItem, gridSize);
        var halfTokenSize = Math.Max(1, tokenListItem.Token.GetSizeFactor()) * gridSize / 2.0;
        return (center.X - halfTokenSize, center.Y - halfTokenSize, center.X + halfTokenSize, center.Y + halfTokenSize);
    }

    // A token linked to another selected token is already moved through that link
    private bool IsMovedByLinkedToken(TokenListItem tokenListItem, List<TokenListItem> tokens)
    {
        var tokenLink = tokenListItem.LinkableObject.GetTokenLink();
        while (tokenLink is TokenListItem linkedToken)
        {
            if (tokens.Contains(linkedToken))
                return true;

            tokenLink = linkedToken.LinkableObject.GetTokenLink();
        }
        return false;
    }

    private void MouseLeftButtonUp(object? sender, MouseButtonDataEventArgs e)
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
                var topToken = foundTokens.OrderBy(t => t.ZLevel).Last();
                if (IsShiftPressed())
                {
                    var selectedTokens = new List<TokenListItem>(SelectedTokens ?? new());
                    if (!selectedTokens.Remove(topToken))
                    {
                        selectedTokens.Add(topToken);
                    }
                    SelectedTokens = selectedTokens;
                }
                else
                {
                    SelectedTokens = new() { topToken };
                }
            }
            else if (!IsShiftPressed())
            {
                SelectedTokens = new();
            }
        }
    }

    // Key events only arrive when an element has keyboard focus, the keyboard state is always available
    private static bool IsShiftPressed()
    {
        return Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
    }

    private void RectangleAreaSelected(object? sender, RectangleF e)
    {
        lock (_lock)
        {
            var selectionLeft = ((double)e.Left).Map(0, _mapSize.CanvasWidth, 0, _mapSize.Width);
            var selectionTop = ((double)e.Top).Map(0, _mapSize.CanvasHeight, 0, _mapSize.Height);
            var selectionRight = ((double)e.Right).Map(0, _mapSize.CanvasWidth, 0, _mapSize.Width);
            var selectionBottom = ((double)e.Bottom).Map(0, _mapSize.CanvasHeight, 0, _mapSize.Height);

            var gridSize = _mapSize.GridSize;
            var foundTokens = new List<TokenListItem>();

            foreach (var tokenListItem in TokenList)
            {
                var bounds = GetTokenBounds(tokenListItem, gridSize);

                // A token is selected when the selection area touches it
                if (bounds.Left < selectionRight && bounds.Right > selectionLeft &&
                    bounds.Top < selectionBottom && bounds.Bottom > selectionTop)
                {
                    foundTokens.Add(tokenListItem);
                }
            }

            SelectedTokens = IsShiftPressed() ? (SelectedTokens ?? new()).Union(foundTokens).ToList() : foundTokens;
        }
    }

    private void MoveTokenFromWeb(object? sender, MoveTokenEventArgs e)
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
            var visibleTokensBitmap = BitmapTools.CreateEmptyBitmap();

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

                // Create token bitmaps: DM sees all tokens (hidden ones see-through), players only visible ones
                var orderedTokens = TokenList.OrderBy(t => t.ZLevel).ToList();
                tokenBitmap = BitmapTools.MergeBitmaps(orderedTokens.Select(t => _tokenBitmapDictionary[t]).ToArray());
                visibleTokensBitmap = BitmapTools.MergeBitmaps(orderedTokens.Where(t => t.Visible).Select(t => _tokenBitmapDictionary[t]).ToArray());

                UpdateTokenSelection();
            }
            else
            {
                _tokenBitmapDictionary.Clear();
                TokenSelectionBitmapSource = BitmapTools.CreateEmptyBitmap().ToBitmapImage();
            }

            _visibleTokensBitmap = visibleTokensBitmap;
            VisibleTokensBitmapSource = visibleTokensBitmap.ToBitmapImage();
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
