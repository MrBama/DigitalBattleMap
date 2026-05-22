using DigitalBattleMap.Common;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Input;

namespace DigitalBattleMap.DataClasses;

public class TokenListItem : PropertyHandler, ITokenLink, ILinkableObject, IDisposable
{
    private Bitmap _bitmap;
    private ITokenLinker _tokenLinker;
    private IPlayers _players;
    private ITokenListItemMultiActions _multiActions;

    public TokenListItem()
    {
        Conditions = new List<Condition>();
        Visible = true;
        Height = 0;
        LinkableObject = new LinkableObject(UpdatePosition);

        TokenSizeChangedCommand = new RelayCommand(p => TokenSizeChanged((TokenSize)p));
        TokenOrientationChangedCommand = new RelayCommand(p => TokenOrientationChanged((TokenOrientation)p));
        ConditionChangedCommand = new RelayCommand(p => ConditionChanged((Condition)p));
        ConditionHeightCommand = new RelayCommand(p => ConditionHeightChanged());
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
    public int Height { get => Get<int>(); set => Set(value); }
    public TokenHealth Health { get; set; } = new TokenHealth();

    [JsonIgnore]
    public bool AreConditionsVisible { get => Get<bool>(); set => Set(value); }

    [JsonIgnore]
    public List<LinkableObject> LinkedObjects { get; set; } = new();
    [JsonIgnore]
    public LinkableObject LinkableObject { get; private set; }
    [JsonIgnore]
    public ICommand TokenSizeChangedCommand { get; set; }
    [JsonIgnore]
    public ICommand TokenOrientationChangedCommand { get; set; }
    [JsonIgnore]
    public ICommand ConditionChangedCommand { get; set; }
    [JsonIgnore]
    public ICommand ConditionHeightCommand { get; set; }
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

    public void Unlink(LinkableObject linkableObject)
    {
        LinkedObjects.Remove(linkableObject);
    }

    public TokenIdentifier GetTokenIdentifier()
    {
        return new TokenIdentifier(Token.Name, Id);
    }

    public void Dispose()
    {
        LinkableObject.Dispose();

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

        NotifyTokenChanged();
    }

    public void SetHeight(int height)
    {
        if(Height != height)
        {
            Height = height;
            ToggleHeightCondition();
            NotifyConditionsChanged();
            NotifyTokenChanged();
        }
    }

    public override string ToString()
    {
        return $"{Token.Name} ID: {Id}";
    }

    private void TokenSizeChanged(TokenSize size)
    {
        Token.Size = size;
        _multiActions.TokenSizeChanged(this);
        NotifyTokenChanged();
    }

    private void TokenOrientationChanged(TokenOrientation orientation)
    {
        Token.Orientation = orientation;
        _multiActions.TokenOrientationChanged(this);
        NotifyTokenChanged();
    }

    private void ConditionChanged(Condition condition)
    {
        ToggleCondition(condition);

        _multiActions.ConditionsChanged(this);
        NotifyConditionsChanged();
        NotifyTokenChanged();
    }

    private void ConditionHeightChanged()
    {
        ToggleHeightCondition();
        _multiActions.HeightChanged(this);
        NotifyConditionsChanged();
        NotifyTokenChanged();
    }

    private void ToggleHeightCondition()
    {
        var height = Condition.Height;
        if (Height != 0)
        {
            if (!Conditions.Contains(height))
            {
                Conditions.Add(height);
            }
        }
        else
        {
            if (Conditions.Contains(height))
            {
                Conditions.Remove(height);
            }
        }
        NotifyPropertyChange(nameof(Conditions));
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
        OnConditionsChanged?.Invoke(this, new ConditionsChangedEventArgs { TokenIdentifier = GetTokenIdentifier(), NewConditions = Conditions });
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
        if (!LinkableObject.IsLinked())
        {
            _tokenLinker.LinkToToken(this);
        }
        else
        {
            LinkableObject.Unlink();
        }
    }

    private void AddToPlayer()
    {
        _players.AddTokenToPlayer(GetTokenIdentifier());
    }

    private void ExpandConditions()
    {
        AreConditionsVisible = !AreConditionsVisible;
    }

    private void HealthChanged(object? sender, EventArgs e)
    {
        if(Health.Hp == "0")
        {
            if(!Conditions.Contains(Condition.Death))
            {
                ToggleCondition(Condition.Death);
                NotifyConditionsChanged();
                NotifyTokenChanged();
            }
        }
        _multiActions.HealthChanged(this);
    }

    private void MaxHealthChanged(object? sender, EventArgs e)
    {
        _multiActions.MaxHealthChanged(this);
    }
}
