using DigitalBattleMap.Common;
using DigitalBattleMap.Utilities;
using Markdig.Parsers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Reflection;

namespace DigitalBattleMap.DataClasses;

public class ConditionIcons
{
    private const string _iconResourcePath = "DigitalBattleMap.Resources.ConditionIcons";
    private const string _digitResourcePath = "DigitalBattleMap.Resources.Digits";

    private Bitmap _baned;
    private Bitmap _blessed;
    private Bitmap _blinded;
    private Bitmap _charmed;
    private Bitmap _concentration;
    private Bitmap _deafened;
    private Bitmap _death;
    private Bitmap _exhausted;
    private Bitmap _flying;
    private Bitmap _frightened;
    private Bitmap _grappled;
    private Bitmap _hasted;
    private Bitmap _hex;
    private Bitmap _highlighted;
    private Bitmap _incapacitated;
    private Bitmap _invisible;
    private Bitmap _mark;
    private Bitmap _paralyzed;
    private Bitmap _petrified;
    private Bitmap _poisoned;
    private Bitmap _prone;
    private Bitmap _restrained;
    private Bitmap _stabilized;
    private Bitmap _stunned;
    private Bitmap _unconcious;

    private Bitmap _0;
    private Bitmap _1;
    private Bitmap _2;
    private Bitmap _3;
    private Bitmap _4;
    private Bitmap _5;
    private Bitmap _6;
    private Bitmap _7;
    private Bitmap _8;
    private Bitmap _9;

    public ConditionIcons()
    {
        _baned = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Baned.png"));
        _blessed = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Blessed.png"));
        _blinded = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Blinded.png"));
        _charmed = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Charmed.png"));
        _concentration = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Concentration.png"));
        _deafened = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Deafened.png"));
        _death = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Death.png"));
        _exhausted = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Exhausted.png"));
        _flying = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Flying.png"));
        _frightened = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Frightened.png"));
        _grappled = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Grappled.png"));
        _hasted = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Hasted.png"));
        _hex = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Hex.png"));
        _highlighted = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Highlighted.png"));
        _incapacitated = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Incapacitated.png"));
        _invisible = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Invisible.png"));
        _mark = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Mark.png"));
        _paralyzed = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Paralyzed.png"));
        _petrified = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Petrified.png"));
        _poisoned = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Poisoned.png"));
        _prone = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Prone.png"));
        _restrained = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Restrained.png"));
        _stabilized = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Stabilized.png"));
        _stunned = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Stunned.png"));
        _unconcious = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Unconcious.png"));

        _0 = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_digitResourcePath}.0.png"));
        _1 = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_digitResourcePath}.1.png"));
        _2 = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_digitResourcePath}.2.png"));
        _3 = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_digitResourcePath}.3.png"));
        _4 = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_digitResourcePath}.4.png"));
        _5 = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_digitResourcePath}.5.png"));
        _6 = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_digitResourcePath}.6.png"));
        _7 = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_digitResourcePath}.7.png"));
        _8 = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_digitResourcePath}.8.png"));
        _9 = IO.File.LoadBitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_digitResourcePath}.9.png"));
    }

    public Bitmap GetConditionIcon(Condition condition)
    {
        switch (condition)
        {
            case Condition.Baned:
                return _baned;
            case Condition.Blessed:
                return _blessed;
            case Condition.Blinded:
                return _blinded;
            case Condition.Charmed:
                return _charmed;
            case Condition.Concentration:
                return _concentration;
            case Condition.Deafened:
                return _deafened;
            case Condition.Death:
                return _death;
            case Condition.Exhausted:
                return _exhausted;
            case Condition.Flying:
                return _flying;
            case Condition.Frightened:
                return _frightened;
            case Condition.Grappled:
                return _grappled;
            case Condition.Hasted:
                return _hasted;
            case Condition.Hex:
                return _hex;
            case Condition.Highlighted:
                return _highlighted;
            case Condition.Incapacitated:
                return _incapacitated;
            case Condition.Invisible:
                return _invisible;
            case Condition.Mark:
                return _mark;
            case Condition.Paralyzed:
                return _paralyzed;
            case Condition.Petrified:
                return _petrified;
            case Condition.Poisoned:
                return _poisoned;
            case Condition.Prone:
                return _prone;
            case Condition.Restrained:
                return _restrained;
            case Condition.Stabilized:
                return _stabilized;
            case Condition.Stunned:
                return _stunned;
            case Condition.Unconcious:
                return _unconcious;
            default:
                return new(100, 100);
        }
    }

    public Bitmap GetDigitIcon(char digit)
    {
        switch (digit)
        {
            case '0':
                return _0;
            case '1':
                return _1;
            case '2':
                return _2;
            case '3':
                return _3;
            case '4':
                return _4;
            case '5':
                return _5;
            case '6':
                return _6;
            case '7':
                return _7;
            case '8':
                return _8;
            case '9':
                return _9;
            default:
                return new(100, 100);
        }
    }

    public Bitmap GetDigitIcon(string heightString)
    {
        var bitmaps = new List<Bitmap>();
        char[] digits = heightString.ToCharArray();
        for (int i = 0; i < heightString.Length; i++)
        {
            bitmaps.Add(GetDigitIcon(digits[i]));
        }
        return BitmapTools.ConcateBitmaps(bitmaps);
    }

    public Bitmap GetDigitIcon(int digit)
    {
        var heightString = digit.ToString();
        return GetDigitIcon(heightString);
    }
}
