using DigitalBattleMap.Utilities;
using System;
using System.Drawing;

namespace DigitalBattleMap.DataClasses;

public class Token : PropertyHandler, ICloneable
{
    public event EventHandler OnSizeChanged;
    public event EventHandler OnOrientationChanged;
    public event EventHandler OnRequestRedraw;

    public string Name { get; set; } = "";
    public TokenSize Size { get => Get<TokenSize>(); set => Set(value, NotifySizeChanged); }
    public TokenOrientation Orientation { get => Get<TokenOrientation>(); set => Set(value, NotifyOrientationChanged); }
    public string ImagePath { get; set; } = "";
    public Statblock? Statblock { get; set; }
    public int? Hp { get; set; }

    public object Clone()
    {
        return new Token
        {
            Name = Name,
            Size = Size,
            Orientation = Orientation,
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

    public void SetOrientationWithoutNotification(TokenOrientation orientation)
    {
        Set(orientation, nameof(Orientation));
    }

    public override string ToString()
    {
        return Name;
    }

    private void NotifySizeChanged()
    {
        OnSizeChanged?.Invoke(this, new EventArgs());
        OnRequestRedraw?.Invoke(this, new EventArgs());
    }

    private void NotifyOrientationChanged()
    {
        OnOrientationChanged?.Invoke(this, new EventArgs());
        OnRequestRedraw?.Invoke(this, new EventArgs());
    }

    internal RotateFlipType GetOrientation()
    {
        switch (Orientation)
        {
            case TokenOrientation.North:
                return RotateFlipType.RotateNoneFlipNone;
            case TokenOrientation.East:
                return RotateFlipType.Rotate270FlipNone;
            case TokenOrientation.South:
                return RotateFlipType.Rotate180FlipNone;
            case TokenOrientation.West:
                return RotateFlipType.Rotate90FlipNone;
            default:
                return RotateFlipType.Rotate270FlipNone;
        }
    }

    internal int GetOrientationAngle()
    {
        switch (Orientation)
        {
            case TokenOrientation.North:
                return 0;
            case TokenOrientation.East:
                return 270;
            case TokenOrientation.South:
                return 180;
            case TokenOrientation.West:
                return 90;
            default:
                return 270;
        }
    }
}