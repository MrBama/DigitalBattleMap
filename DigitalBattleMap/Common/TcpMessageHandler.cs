using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap.Common
{
    public class TcpMessageHandler
    {
        private List<byte[]> _buffers = new List<byte[]>();
        private Func<bool> _isTerminated;

        public TcpMessageHandler(Func<bool> isTerminated)
        {
            _isTerminated = isTerminated;
        }

        public delegate void MoveTokenActionEventHandler(object sender, TcpMessageReceivedEventArgs e);

        public event MoveTokenActionEventHandler MessageReceived;

        public void ReceivedBytes(byte[] receivedBytes)
        {
            _buffers.Add(receivedBytes);
            var buffer = ByteArray.Combine(_buffers);

            var indexOfEndOfMessage = ByteArray.GetIndexOfBytes(buffer, TcpConstants.EndOfMessage);
            if(indexOfEndOfMessage != -1)
            {
                while (indexOfEndOfMessage != -1 && !_isTerminated())
                {
                    var buffer1Lenght = indexOfEndOfMessage + Encoding.UTF8.GetBytes(TcpConstants.EndOfMessage).Length;
                    var buffer2Lenght = buffer.Length - buffer1Lenght;

                    var buffer1 = new byte[buffer1Lenght];
                    var buffer2 = new byte[buffer2Lenght];
                    Buffer.BlockCopy(buffer, 0, buffer1, 0, buffer1Lenght);
                    Buffer.BlockCopy(buffer, buffer1Lenght, buffer2, 0, buffer2Lenght);

                    NotifyMessageReceived(buffer1);
                    _buffers.Clear();

                    buffer = buffer2;
                    indexOfEndOfMessage = ByteArray.GetIndexOfBytes(buffer, TcpConstants.EndOfMessage);
                }

                if (buffer.Length > 0)
                {
                    _buffers.Add(buffer);
                }
            }
        }

        private void NotifyMessageReceived(byte[] bytes)
        {
            var args = new TcpMessageReceivedEventArgs { Bytes = bytes };
            MessageReceived?.Invoke(this, args);
        }
    }
}
