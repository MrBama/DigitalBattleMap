using System.ComponentModel.DataAnnotations;
using DigitalBattleMapServer.Application;
using DigitalBattleMapServer.Properties;

namespace DigitalBattleMapServer.Models;

public class SettingsViewModel
{
    [Display(Name = "Name", ResourceType = typeof(Resources))]
    public string Name { get; set; }

    [Display(Name = "Orientation", ResourceType = typeof(Resources))]
    public Orientation Orientation { get; set; }

    [Display(Name = "Characters", ResourceType = typeof(Resources))]
    public string Characters { get; set; }
}