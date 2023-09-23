using DigitalBattleMap.Utilities;
using System;
using System.IO;

namespace DigitalBattleMap.DataClasses;

public class Token : PropertyHandler
{
    private string _statblockMarkdown;

    public event EventHandler OnSizeChanged;

    public string Name { get; set; } = "";
    public TokenSize Size { get => Get<TokenSize>(); set => Set(value, NotifySizeChanged); }
    public string ImagePath { get; set; } = "";
    public string StatBlockMarkdownPath { get; set; } = "";
    public bool PlayerControl { get => Get<bool>(); set => Set(value); }
    public string? Source { get; set; }
    public int? Hp { get; set; }

    public Token Copy()
    {
        return new Token
        {
            Name = Name,
            Size = Size,
            ImagePath = ImagePath,
            StatBlockMarkdownPath = StatBlockMarkdownPath,
            PlayerControl = PlayerControl,
            Source = Source,
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

    public bool TryGetStatBlockMarkdown(out string markdown)
    {
        markdown = default;

        if (Source == Constants.MarkdownSource)
        {
            if (_statblockMarkdown == null)
            {
                if (IO.File.Exists(StatBlockMarkdownPath))
                {
                    _statblockMarkdown = IO.File.ReadAllText(StatBlockMarkdownPath);
                }
                else
                {
                    return false;
                }
            }

            markdown = _statblockMarkdown;
            return true;
        }
        else
        {
            return false;
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