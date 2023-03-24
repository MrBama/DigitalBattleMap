using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap.Common
{
    public static class TcpConstants
    {
        public const string ActionSeparator = "<:>";
        public const string ArgumentSeparator = "<,>";
        public const string EndOfMessage = "<EOM>";
        public const string UpdateMapAction = "UpdateMap";
        public const string MoveTokenAction = "MoveToken";
        public const string TerminateAction = "Terminate";
    }
}
