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

    public TokenListItem()
    {
        LinkToTokenButtonText = "Link to token";

        TokenSizeChangedCommand = new RelayCommand(p => TokenSizeChanged((string)p));
        PlayerControlCommand = new RelayCommand(p => PlayerControlToggled());
        ConditionChangedCommand = new RelayCommand(p => ConditionChanged((string)p));
        ClearAllConditionsCommand = new RelayCommand(p => ClearAllConditions());
        TokenVisibilityCommand = new RelayCommand(p => ToggleTokenVisibility());
        MoveToFrontCommand = new RelayCommand(p => MoveToFront());
        MoveToBackCommand = new RelayCommand(p => MoveToBack());
        LinkToTokenCommand = new RelayCommand(p => LinkToDifferentToken());
    }

    public delegate void ZLevelChangedEventHandler(object sender, ZLevelChangedEventArgs e);
    public delegate void ConditionsChangedEventHandler(object sender, ConditionsChangedEventArgs e);

    public event EventHandler OnTokenChanged;
    public event ConditionsChangedEventHandler OnConditionsChanged;
    public event ZLevelChangedEventHandler OnZLevelChanged;

    public Token Token { get; set; }
    public Point<int> Position { get; set; } = new Point<int>();
    public int Id { get; set; }
    public List<Condition> Conditions { get; set; } = new List<Condition>();
    public bool Visible { get; set; } = true;
    public int ZLevel { get; set; }
    public int Initiative { get; set; }
    public TokenHealth Health { get; set; } = new TokenHealth();

    [JsonIgnore]
    public List<ILinkableObject> LinkedObjects { get; set; } = new();
    [JsonIgnore]
    public string LinkToTokenButtonText { get => Get<string>(); set => Set(value); }
    [JsonIgnore]
    public ICommand TokenSizeChangedCommand { get; set; }
    [JsonIgnore]
    public ICommand PlayerControlCommand { get; set; }
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

    public Bitmap GetBitmap()
    {
        if (_bitmap == null)
        {
            _bitmap = BitmapTools.LoadBitmap(Token.ImagePath);
        }

        return _bitmap;
    }

    public void SetTokenLinker(ITokenLinker tokenLinker)
    {
        _tokenLinker = tokenLinker;
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

    private void TokenSizeChanged(string size)
    {
        Token.Size = Enum.Parse<TokenSize>(size);
    }

    private void PlayerControlToggled()
    {
        Token.PlayerControl = !Token.PlayerControl;
        NotifyConditionsChanged();
    }

    private void ConditionChanged(string conditionString)
    {
        var condition = Enum.Parse<Condition>(conditionString);
        ToggleCondition(condition);

        NotifyConditionsChanged();
        NotifyTokenChanged();
    }

    private void ClearAllConditions()
    {
        if (Conditions.Count > 0)
        {
            Conditions.Clear();
            NotifyConditionsChanged();
            NotifyTokenChanged();
            NotifyPropertyChange(nameof(Conditions));
        }
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
        NotifyTokenChanged();
        NotifyPropertyChange(nameof(Visible));
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
}
