using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
    public class TcpMessage
    {
        public string Action { get; set; } = "";
        public List<string> Arguments { get; set; } = new List<string>();

        public TcpMessage()
        {
        }

        public TcpMessage(string action)
        {
            Action = action;
        }

        public byte[] GetBytes()
        {
            // Format: "action<:>arg1<,>arg2<,><EOM>";

            var rawString = Action;
            rawString += "<:>";

            foreach (var argument in Arguments)
            {
                rawString += argument;
                rawString += "<,>";
            }
            rawString += "<EOM>";

            return Encoding.UTF8.GetBytes(rawString);
        }

        public static TcpMessage Parse(byte[] bytes)
        {
            var rawString = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            var action = rawString.Substring(0, rawString.IndexOf("<:>"));
            rawString = rawString.Substring(action.Length + 3);

            var tcpMessage = new TcpMessage();
            tcpMessage.Action = action;

            var indexOfArgument = rawString.IndexOf("<,>");
            while (indexOfArgument != -1)
            {
                tcpMessage.Arguments.Add(rawString.Substring(0, indexOfArgument));
                rawString = rawString.Substring(indexOfArgument + 3);
                indexOfArgument = rawString.IndexOf("<,>");
            }

            return tcpMessage;
        }

        public static bool TryParse(byte[] bytes, out TcpMessage tcpMessage)
        {
            try
            {
                tcpMessage = Parse(bytes);
            }
            catch (Exception)
            {
                tcpMessage = new TcpMessage();
                return false;
            }

            return true;
        }
    }
}
