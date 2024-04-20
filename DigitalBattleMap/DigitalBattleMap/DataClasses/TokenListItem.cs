using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Input;
using System;
using Newtonsoft.Json;
using DigitalBattleMap.Common;

namespace DigitalBattleMap.DataClasses;

public class TokenListItem : PropertyHandler, ITokenLink, ILinkableObject, IDisposable
{
    private Bitmap _bitmap;
    private ITokenLink _tokenLink;
    private ITokenLinker _tokenLinker;
    private IPlayers _players;
    private ITokenListItemMultiActions _multiActions;

    public TokenListItem()
    {
        LinkToTokenButtonText = "Link to token";
        Conditions = new List<Condition>();
        Visible = true;

        TokenSizeChangedCommand = new RelayCommand(p => TokenSizeChanged((string)p));
        TokenOrientationChangedCommand = new RelayCommand(p => TokenOrientationChanged((string)p));
        ConditionChangedCommand = new RelayCommand(p => ConditionChanged((string)p));
        ClearAllConditionsCommand = new RelayCommand(p => ClearAllConditions());
        TokenVisibilityCommand = new RelayCommand(p => ToggleTokenVisibility());
        MoveToFrontCommand = new RelayCommand(p => MoveToFront());
        MoveToBackCommand = new RelayCommand(p => MoveToBack());
        LinkToTokenCommand = new RelayCommand(p => LinkToDifferentToken());
        AddToPlayerCommand = new RelayCommand(p => AddToPlayer());
        ExpandConditionsCommand = new RelayCommand(p => ExpandConditions());
    }

    public TokenListItem(Token token, ITokenLinker tokenLinker, IPlayers players, ITokenListItemMultiActions multiActions) : this()
    {
        Token = token;
        _tokenLinker = tokenLinker;
        _players = players;
        _multiActions = multiActions;

        if (token.Hp != null)
        {
            Health.InitializeEditorHp(token.Hp ?? default);
        }

        Health.OnHpChanged += HealthChanged;
        Health.OnMaxHpChanged += MaxHealthChanged;
        Token.OnSizeChanged += TokenSizeChanged;
        Token.OnOrientationChanged += TokenOrientationChanged;
    }

    public event EventHandler OnTokenChanged;
    public event EventHandler<ConditionsChangedEventArgs> OnConditionsChanged;
    public event EventHandler<ZLevelChangedEventArgs> OnZLevelChanged;

    public Token Token { get; set; }
    public Point<int> Position { get; set; } = new Point<int>();
    public int Id { get; set; }
    public List<Condition> Conditions { get => Get<List<Condition>>(); set => Set(value); }
    public bool Visible { get => Get<bool>(); set => Set(value); }
    public int ZLevel { get; set; }
    public int Initiative { get => Get<int>(); set => Set(value, () => _multiActions?.InitiativeChanged(this)); }
    public TokenHealth Health { get; set; } = new TokenHealth();

    [JsonIgnore]
    public bool AreConditionsVisible { get => Get<bool>(); set => Set(value); }

    [JsonIgnore]
    public List<ILinkableObject> LinkedObjects { get; set; } = new();
    [JsonIgnore]
    public string LinkToTokenButtonText { get => Get<string>(); set => Set(value); }
    [JsonIgnore]
    public ICommand TokenSizeChangedCommand { get; set; }
    [JsonIgnore]
    public ICommand TokenOrientationChangedCommand { get; set; }
    [JsonIgnore]
    public ICommand ConditionChangedCommand { get; set; }
    [JsonIgnore]
    public ICommand ClearAllConditionsCommand { get; set; }
    [JsonIgnore]
    public ICommand TokenVisibilityCommand { get; set; }
    [JsonIgnore]
    public ICommand MoveToFrontCommand { get; set; }
    [JsonIgnore]
    public ICommand MoveToBackCommand { get; set; }
    [JsonIgnore]
    public ICommand LinkToTokenCommand { get; set; }
    [JsonIgnore]
    public ICommand AddToPlayerCommand { get; set; }
    [JsonIgnore]
    public ICommand ExpandConditionsCommand { get; set; }

    public Bitmap GetBitmap()
    {
        if (_bitmap == null)
        {
            _bitmap = IO.File.LoadBitmap(Token.ImagePath);
        }

        return _bitmap;
    }

    public void SetInterfaces(ITokenLinker tokenLinker, IPlayers players, ITokenListItemMultiActions multiActions)
    {
        _tokenLinker = tokenLinker;
        _players = players;
        _multiActions = multiActions;

        Token.OnSizeChanged += TokenSizeChanged;
        Token.OnOrientationChanged += TokenOrientationChanged;
        Health.OnHpChanged += HealthChanged;
        Health.OnMaxHpChanged += MaxHealthChanged;
    }

    public void ToggleCondition(Condition condition)
    {
        if (!Conditions.Contains(condition))
        {
            if (condition == Condition.Death)
            {
                Conditions.Clear();
            }
            Conditions.Add(condition);
        }
        else
        {
            Conditions.Remove(condition);
        }

        NotifyPropertyChange(nameof(Conditions));
    }

    public void Unlink(ILinkableObject linkableObject)
    {
        LinkedObjects.Remove(linkableObject);
    }

    public TokenIndentifier GetTokenIndentifier()
    {
        return new TokenIndentifier(Token.Name, Id);
    }

    public void Dispose()
    {
        Unlink();

        foreach (var linkedObject in LinkedObjects)
        {
            linkedObject.DisposeLink();
        }
    }

    public void UpdatePosition(Point<int> offset)
    {
        Position.X += offset.X;
        Position.Y += offset.Y;

        foreach (var linkedObject in LinkedObjects)
        {
            linkedObject.UpdatePosition(offset);
        }
    }

    public void Link(ITokenLink tokenLink)
    {
        _tokenLink?.Unlink(this);
        _tokenLink = tokenLink;
        RefershLinkToTokenButtonText();
    }

    public void Unlink()
    {
        _tokenLink?.Unlink(this);
        _tokenLink = null;
        RefershLinkToTokenButtonText();
    }

    public bool IsLinked()
    {
        return _tokenLink != null;
    }

    public TokenIndentifier GetLinkIdentifier()
    {
        return _tokenLink.GetTokenIndentifier();
    }

    public void DisposeLink()
    {
        _tokenLink = null;
        RefershLinkToTokenButtonText();
    }

    public override string ToString()
    {
        return $"{Token.Name} ID: {Id}";
    }

    private void TokenSizeChanged(string size)
    {
        Token.Size = Enum.Parse<TokenSize>(size);
    }

    private void TokenOrientationChanged(string orientation)
    {
        Token.Orientation = Enum.Parse<TokenOrientation>(orientation);
    }

    private void ConditionChanged(string conditionString)
    {
        var condition = Enum.Parse<Condition>(conditionString);
        ToggleCondition(condition);

        _multiActions.ConditionsChanged(this);
        NotifyConditionsChanged();
        NotifyTokenChanged();
    }

    private void ClearAllConditions()
    {
        Conditions.Clear();
        _multiActions.ConditionsChanged(this);
        NotifyConditionsChanged();
        NotifyTokenChanged();
        NotifyPropertyChange(nameof(Conditions));
    }

    private void NotifyTokenChanged()
    {
        OnTokenChanged?.Invoke(this, new EventArgs());
    }

    private void NotifyConditionsChanged()
    {
        OnConditionsChanged?.Invoke(this, new ConditionsChangedEventArgs { TokenIndentifier = GetTokenIndentifier(), NewConditions = Conditions });
    }

    private void ToggleTokenVisibility()
    {
        Visible = !Visible;
        _multiActions.VisibilityChanged(this);
        NotifyTokenChanged();
    }

    private void MoveToFront()
    {
        OnZLevelChanged?.Invoke(this, new ZLevelChangedEventArgs { ZLevelDirection = ZLevelDirection.Front });
    }

    private void MoveToBack()
    {
        OnZLevelChanged?.Invoke(this, new ZLevelChangedEventArgs { ZLevelDirection = ZLevelDirection.Back });
    }

    private void LinkToDifferentToken()
    {
        if (!IsLinked())
        {
            _tokenLinker.LinkToToken(this);
        }
        else
        {
            Unlink();
        }
    }

    private void RefershLinkToTokenButtonText()
    {
        if (IsLinked())
        {
            var linkIdentifier = GetLinkIdentifier();
            LinkToTokenButtonText = $"Unlink from {linkIdentifier.Name} {linkIdentifier.Id}";
        }
        else
        {
            LinkToTokenButtonText = "Link to token";
        }
    }

    private void AddToPlayer()
    {
        _players.AddTokenToPlayer(GetTokenIndentifier());
    }

    private void ExpandConditions()
    {
        AreConditionsVisible = !AreConditionsVisible;
    }

    private void TokenSizeChanged(object? sender, EventArgs e)
    {
        _multiActions.TokenSizeChanged(this);
    }
    private void TokenOrientationChanged(object? sender, EventArgs e)
    {
        _multiActions.TokenOrientationChanged(this);
    }
    

    private void HealthChanged(object? sender, EventArgs e)
    {
        _multiActions.HealthChanged(this);
    }

    private void MaxHealthChanged(object? sender, EventArgs e)
    {
        _multiActions.MaxHealthChanged(this);
    }
}
