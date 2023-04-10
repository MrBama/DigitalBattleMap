using DigitalBattleMapServer.Application;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DigitalBattleMapServer.Models;

public class NavigationComponentViewModel
{
    public Orientation Orientation { get; set; }
    
    public string Character { get; set; }
    public IEnumerable<SelectListItem> Characters { get; set; }
}