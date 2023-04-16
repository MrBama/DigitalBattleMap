using DigitalBattleMap.Common;
using System.Drawing;

namespace DigitalBattleMap.DataClasses;
public class MapUpdate
{
    public DrawLayer Layer { get; set; }
    public Bitmap Bitmap { get; set; }
}
