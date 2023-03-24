using DigitalBattleMap.Common.DigitalBattleMap.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap.Common
{
    public class TcpCommandMessage
    {
        public string Action { get; set; } = "";
        public List<string> Arguments { get; set; } = new List<string>();

        public TcpCommandMessage(string action)
        {
            Action = action;
        }

        public byte[] GetBytes()
        {
            // Format: "action<:>arg1<,>arg2<,><EOM>";
            var rawString = Action;
            rawString += TcpConstants.ActionSeparator;

            foreach (var argument in Arguments)
            {
                rawString += argument;
                rawString += TcpConstants.ArgumentSeparator;
            }
            rawString += TcpConstants.EndOfMessage;

            return Encoding.UTF8.GetBytes(rawString);
        }

        public static TcpCommandMessage Parse(TcpMessage tcpMessage)
        {
            var bytes = tcpMessage.GetBytes();
            var rawString = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            var action = rawString.Substring(0, rawString.IndexOf(TcpConstants.ActionSeparator));
            rawString = rawString.Substring(action.Length + 3);

            var tcpImageMessage = new TcpCommandMessage(action);

            var indexOfArgument = rawString.IndexOf(TcpConstants.ArgumentSeparator);
            while (indexOfArgument != -1)
            {
                tcpImageMessage.Arguments.Add(rawString.Substring(0, indexOfArgument));
                rawString = rawString.Substring(indexOfArgument + 3);
                indexOfArgument = rawString.IndexOf(TcpConstants.ArgumentSeparator);
            }

            return tcpImageMessage;
        }

        public static bool TryParse(TcpMessage tcpMessage, out TcpCommandMessage tcpImageMessage)
        {
            try
            {
                tcpImageMessage = Parse(tcpMessage);
            }
            catch (Exception)
            {
                tcpImageMessage = null;
                return false;
            }

            return true;
        }
    }
}
