using DigitalBattleMap.Common;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Input;

namespace DigitalBattleMap.DataClasses;

public class Token : PropertyHandler
{
    public event EventHandler OnSizeChanged;

    public string Name { get; set; } = "";
    public TokenSize Size { get => Get<TokenSize>(); set => Set(value, NotifySizeChanged); }
    public string ImagePath { get; set; } = "";
    public bool PlayerControl { get => Get<bool>(); set => Set(value); }

    public Token Copy()
    {
        return new Token
        {
            Name = Name,
            Size = Size,
            ImagePath = ImagePath,
            PlayerControl = PlayerControl
        };
    }

    public double GetSizeFactor()
    {
        switch (Size)
        {
            case TokenSize.Tiny:
                return 0.5;
            case TokenSize.Small:
                return 0.75;
            case TokenSize.Medium:
                return 1;
            case TokenSize.Large:
                return 2;
            case TokenSize.Huge:
                return 3;
            case TokenSize.Gargantuan:
                return 4;
            default:
                return 1;
        }
    }

    public override string ToString()
    {
        return Name;
    }

    private void NotifySizeChanged()
    {
        OnSizeChanged?.Invoke(this, new EventArgs());
    }
}

public class TokenListItem : PropertyHandler, ITokenLink, IDisposable
{
    private Bitmap _bitmap;

    public TokenListItem()
    {
        TokenSizeChangedCommand = new RelayCommand(p => TokenSizeChanged((string)p));
        PlayerControlCommand = new RelayCommand(p => PlayerControlToggled());
        ConditionChangedCommand = new RelayCommand(p => ConditionChanged((string)p));
        ClearAllConditionsCommand = new RelayCommand(p => ClearAllConditions());
        TokenVisibilityCommand = new RelayCommand(p => ToggleTokenVisibility());
        MoveToFrontCommand = new RelayCommand(p => MoveToFront());
        MoveToBackCommand = new RelayCommand(p => MoveToBack());
    }

    public delegate void ZLevelChangedEventHandler(object sender, ZLevelChangedEventArgs e);

    public event EventHandler OnTokenChanged;
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

    public Bitmap GetBitmap()
    {
        if (_bitmap == null)
        {
            _bitmap = BitmapTools.LoadBitmap(Token.ImagePath);
        }

        return _bitmap;
    }

    public void ToggleCondition(Condition condition)
    {
        if (!Conditions.Contains(condition))
        {
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

    public void Dispose()
    {
        foreach (var linkedObject in LinkedObjects)
        {
            linkedObject.DisposeLink();
        }
    }

    private void TokenSizeChanged(string size)
    {
        Token.Size = Enum.Parse<TokenSize>(size);
    }

    private void PlayerControlToggled()
    {
        Token.PlayerControl = !Token.PlayerControl;
    }

    private void ConditionChanged(string conditionString)
    {
        var condition = Enum.Parse<Condition>(conditionString);
        ToggleCondition(condition);
        NotifyTokenChanged();
    }

    private void ClearAllConditions()
    {
        if (Conditions.Count > 0)
        {
            Conditions.Clear();
            NotifyTokenChanged();
            NotifyPropertyChange(nameof(Conditions));
        }
    }

    private void NotifyTokenChanged()
    {
        OnTokenChanged?.Invoke(this, new EventArgs());
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
}