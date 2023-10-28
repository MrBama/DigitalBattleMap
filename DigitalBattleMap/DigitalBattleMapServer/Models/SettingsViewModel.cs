using DigitalBattleMapServer.Application;
using DigitalBattleMapServer.Properties;
using System.ComponentModel.DataAnnotations;

namespace DigitalBattleMapServer.Models;

public class SettingsViewModel
{
    [Display(Name = "Name", ResourceType = typeof(Resources))]
    public string Name { get; set; }

    [Display(Name = "Orientation", ResourceType = typeof(Resources))]
    public Orientation Orientation { get; set; }
}