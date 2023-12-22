using DigitalBattleMap.Utilities;
using System;

namespace DigitalBattleMap.DataClasses;

public class Token : PropertyHandler, ICloneable
{
    public event EventHandler OnSizeChanged;

    public string Name { get; set; } = "";
    public TokenSize Size { get => Get<TokenSize>(); set => Set(value, NotifySizeChanged); }
    public string ImagePath { get; set; } = "";
    public Statblock? Statblock { get; set; }
    public int? Hp { get; set; }

    public object Clone()
    {
        return new Token
        {
            Name = Name,
            Size = Size,
            ImagePath = ImagePath,
            Statblock = Statblock?.Clone<Statblock>(),
            Hp = Hp
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

    public void SetSizeWithoutNotification(TokenSize size)
    {
        Set(size, nameof(Size));
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