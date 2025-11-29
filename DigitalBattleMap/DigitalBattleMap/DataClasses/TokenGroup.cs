using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBattleMap.DataClasses;

public class TokenGroup : PropertyHandler, ICloneable
{
    public TokenGroup()
    {
        Name = "";
    }

    public string Name { get => Get<string>(); set => Set(value); }

    public List<string> TokenNames { get; set; } = new List<string>();

    public object Clone()
    {
        return new TokenGroup
        {
            Name = Name,
            TokenNames = TokenNames.ToList()
        };
    }
}
