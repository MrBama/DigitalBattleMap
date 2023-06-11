using DigitalBattleMap.Utilities;
using System;
using System.Windows;

namespace DigitalBattleMap.DataClasses;

public class Statblock : PropertyHandler
{
    public Statblock(string name, string source)
    {
        Name = name;
        Link = $"https://5e.tools/bestiary.html#{Uri.EscapeDataString(Name)}_{source}";
    }

    public string Name { get; set; }
    public string Link { get; set; }
    public Visibility RenderVisibility { get => Get<Visibility>(); set => Set(value); }
}
