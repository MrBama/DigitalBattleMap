using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
    public class MoveTokenActionEventArgs : EventArgs
    {
        public string Name { get; set; } = "";
        public int Id { get; set; } = 1;
        public TokenDirection Direction { get; set; }
    }

    public class ZLevelChangedEventArgs : EventArgs
    {
        public ZLevelDirection ZLevelDirection { get; set; }
    }
}
