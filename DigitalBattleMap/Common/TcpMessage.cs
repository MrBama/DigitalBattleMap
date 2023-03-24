namespace DigitalBattleMap.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    namespace DigitalBattleMap.Common
    {
        public class TcpMessage
        {
            private byte[] _buffer;

            public TcpMessage(byte[] buffer)
            {
                _buffer = buffer;

                var indexOfActionSeparator = ByteArray.GetIndexOfBytes(buffer, TcpConstants.ActionSeparator);
                if (indexOfActionSeparator != -1)
                {
                    Action = Encoding.UTF8.GetString(buffer.Extract(0, indexOfActionSeparator));
                    IsValid = true;
                }
            }

            public bool IsValid { get; set; }
            public string Action { get; set; }

            public byte[] GetBytes()
            {
                return _buffer;
            }
        }
    }
}