using DigitalBattleMap.Utilities;
using System;

namespace DigitalBattleMap.DataClasses;

public class Token : PropertyHandler, ICloneable
{
    public string Name { get; set; } = "";
    public TokenSize Size { get => Get<TokenSize>(); set => Set(value); }
    public TokenOrientation Orientation { get => Get<TokenOrientation>(); set => Set(value); }
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

    public override string ToString()
    {
        return Name;
    }

    internal BitmapRotation GetBitmapRotation()
    {
        return Orientation switch
        {
            TokenOrientation.North => BitmapRotation.Rotate0,
            TokenOrientation.East => BitmapRotation.Rotate90,
            TokenOrientation.South => BitmapRotation.Rotate180,
            TokenOrientation.West => BitmapRotation.Rotate270,
            _ => throw new NotSupportedException()
        };
    }

    internal int GetOrientationAngle()
    {
        switch (Orientation)
        {
            case TokenOrientation.North:
                return 0;
            case TokenOrientation.East:
                return 90;
            case TokenOrientation.South:
                return 180;
            case TokenOrientation.West:
                return 270;
            default:
                return 270;
        }
    }
}