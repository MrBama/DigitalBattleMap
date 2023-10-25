using DigitalBattleMap.Utilities;
using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public class TokenGroup : PropertyHandler
{
    public TokenGroup()
    {
        Name = "";
    }

    public string Name { get => Get<string>(); set => Set(value); }

    public List<string> TokenNames { get; set; } = new List<string>();
}
