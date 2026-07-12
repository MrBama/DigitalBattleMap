using DigitalBattleMap.Common;
using DigitalBattleMapServer.Properties;
using System.ComponentModel.DataAnnotations;

namespace DigitalBattleMapServer.Models;

public class SettingsViewModel
{
    [Display(Name = "Name", ResourceType = typeof(Resources))]
    public string Name { get; set; }

    [Display(Name = "Orientation", ResourceType = typeof(Resources))]
    public Orientation Orientation { get; set; }

    [Display(Name = "ConditionVersion", ResourceType = typeof(Resources))]
    public string ConditionVersion { get; set; } = "5.5e";
}