using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DigitalBattleMap
{
    public class ConnectionManager
    {
        private Thread _thread;
        private bool _isTerminated = false;
        private bool _isConnected = false;

        public delegate void MoveTokenActionEventHandler(object sender, MoveTokenActionEventArgs e);

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event MoveTokenActionEventHandler MoveTokenAction;

        public void Connect(string ipAddress, int port)
        {
            _isTerminated = false;
            _thread = new Thread(() => InitiateConnection(ipAddress, port));
            _thread.Start();
        }

        public void Disconnect()
        {
            _isTerminated = true;
            if (_thread != null)
            {
                _thread.Join();
            }
        }

        public void InitiateConnection(string ipAddress, int port)
        {
            var ipEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            var client = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                StartClient(client, ipEndPoint);
            }
            catch (TerminateException)
            {
                if (_isConnected)
                {
                    var sendTask = client.SendAsync(Encoding.UTF8.GetBytes("<Terminate>"), SocketFlags.None);
                    try
                    {
                        WaitOnTaskCompletion(sendTask, 2000);
                    }
                    catch
                    {
                    }
                }
                NotifyDisconnected();
            }
        }

        public void StartClient(Socket client, IPEndPoint ipEndPoint)
        {
            _isConnected = false;
            var connectTask = client.ConnectAsync(ipEndPoint);
            WaitOnTaskCompletion(connectTask, 3000);
            if (!connectTask.IsCompletedSuccessfully)
            {
                throw new TerminateException();
            }

            _isConnected = true;
            NotifyConnected();
            var buffers = new List<byte[]>();

            while (!_isTerminated)
            {
                if (client.Available > 0)
                {
                    var buffer = new byte[client.Available];
                    var receiveTask = client.ReceiveAsync(buffer, SocketFlags.None);
                    WaitOnTaskCompletion(receiveTask);

                    var indexOfEndOfMessage = SearchBytes(buffer, Encoding.UTF8.GetBytes("<EOM>"));
                    while (indexOfEndOfMessage != -1 && !_isTerminated)
                    {
                        var buffer1Lenght = indexOfEndOfMessage + "<EOM>".Length;
                        var buffer2Lenght = buffer.Length - buffer1Lenght;

                        var buffer1 = new byte[buffer1Lenght];
                        var buffer2 = new byte[buffer2Lenght];
                        Buffer.BlockCopy(buffer, 0, buffer1, 0, buffer1Lenght);
                        Buffer.BlockCopy(buffer, buffer1Lenght, buffer2, 0, buffer2Lenght);

                        buffers.Add(buffer1);
                        MessageReceived(buffers);
                        buffers.Clear();

                        buffer = buffer2;
                        indexOfEndOfMessage = SearchBytes(buffer, Encoding.UTF8.GetBytes("<EOM>"));
                    }
                }
            }

            throw new TerminateException();
        }

        private void MessageReceived(List<byte[]> buffers)
        {
            var combinedBuffer = Combine(buffers);
            if (TcpMessage.TryParse(combinedBuffer, out var tcpMessage))
            {
                switch (tcpMessage.Action)
                {
                    case "MoveToken":
                        MoveToken(tcpMessage);
                        break;
                    default:
                        break;
                }
            }
        }

        private void MoveToken(TcpMessage tcpMessage)
        {
            var direction = Enum.Parse<TokenDirection>(tcpMessage.Arguments[1]);

            var rawName = tcpMessage.Arguments[0];
            var indexOf_ = rawName.LastIndexOf("_");
            var name = rawName;
            var id = 1;

            if (indexOf_ != -1 && int.TryParse(rawName.Substring(indexOf_ + 1), out var parsedId))
            {
                name = rawName.Substring(0, indexOf_);
                id = parsedId;
            }

            NotifyMoveTokenAction(name, id, direction);
        }

        private void WaitOnTaskCompletion(Task task)
        {
            while (!task.IsCompleted && !_isTerminated)
            {
                Thread.Sleep(10);
            }

            if (_isTerminated)
            {
                throw new TerminateException();
            }
        }

        private void WaitOnTaskCompletion(Task task, int timeoutInMilliseconds)
        {
            var startTime = DateTime.Now;
            var endTime = DateTime.Now;
            while (!task.IsCompleted && !_isTerminated && (endTime - startTime).TotalMilliseconds <= timeoutInMilliseconds)
            {
                Thread.Sleep(10);
                endTime = DateTime.Now;
            }

            if (_isTerminated)
            {
                throw new TerminateException();
            }

            if ((endTime - startTime).TotalMilliseconds > timeoutInMilliseconds)
            {
                throw new TerminateException();
            }
        }

        private int SearchBytes(byte[] haystack, byte[] needle)
        {
            var lenght = needle.Length;
            var limit = haystack.Length - lenght;
            for (var i = 0; i <= limit; i++)
            {
                var k = 0;
                for (; k < lenght; k++)
                {
                    if (needle[k] != haystack[i + k]) break;
                }
                if (k == lenght) return i;
            }
            return -1;
        }

        private byte[] Combine(List<byte[]> buffers)
        {
            byte[] resultBuffer = new byte[buffers.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] buffer in buffers)
            {
                Buffer.BlockCopy(buffer, 0, resultBuffer, offset, buffer.Length);
                offset += buffer.Length;
            }
            return resultBuffer;
        }

        private void NotifyConnected()
        {
            Connected?.Invoke(this, new EventArgs());
        }

        private void NotifyDisconnected()
        {
            Disconnected?.Invoke(this, new EventArgs());
        }

        private void NotifyMoveTokenAction(string name, int id, TokenDirection direction)
        {
            var args = new MoveTokenActionEventArgs { Name = name, Id = id, Direction = direction };
            MoveTokenAction?.Invoke(this, args);
        }

        public class TerminateException : Exception
        {
        }
    }
}
