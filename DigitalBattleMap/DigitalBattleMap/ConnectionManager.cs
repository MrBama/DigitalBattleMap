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
using System.Collections.Concurrent;
using System.Windows.Shapes;
using DigitalBattleMap.Common;
using DigitalBattleMap.Common.DigitalBattleMap.Common;

namespace DigitalBattleMap
{
    public class ConnectionManager
    {
        private Thread _thread;
        private bool _isTerminated = false;
        private bool _isConnected = false;
        private ConcurrentQueue<MapUpdate> _messageQueue = new ConcurrentQueue<MapUpdate>();
        private TcpMessageHandler _tcpMessageHandler;

        public ConnectionManager()
        {
            _tcpMessageHandler = new TcpMessageHandler(() => _isTerminated);
            _tcpMessageHandler.MessageReceived += OnMessageReceived;
        }

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

        public void SendMapUpdate(MapUpdate mapUpdate)
        {
            if(_isConnected)
            {
                _messageQueue.Enqueue(mapUpdate);
            }
        }

        private void InitiateConnection(string ipAddress, int port)
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
                    var tcpCommandMessage = new TcpCommandMessage(TcpConstants.TerminateAction);
                    var sendTask = client.SendAsync(tcpCommandMessage.GetBytes(), SocketFlags.None);
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

            while (!_isTerminated)
            {
                if (client.Available > 0)
                {
                    var receivedBuffer = new byte[client.Available];
                    var receiveTask = client.ReceiveAsync(receivedBuffer, SocketFlags.None);
                    WaitOnTaskCompletion(receiveTask);
                    _tcpMessageHandler.ReceivedBytes(receivedBuffer);
                }

                if (_messageQueue.TryDequeue(out var mapUpdate))
                {
                    var bitmaps = new List<Bitmap> { mapUpdate.BackgroundBitmap, mapUpdate.GridAndDrawingBitmap, mapUpdate.TokenBitmap };
                    var tcpImageMessage = new TcpImageMessage(BitmapTools.MergeBitmaps(bitmaps));
                    var sendTask = client.SendAsync(tcpImageMessage.GetBytes(), SocketFlags.None);
                    WaitOnTaskCompletion(sendTask);
                }

                Thread.Sleep(10);
            }

            throw new TerminateException();
        }

        private void OnMessageReceived(object sender, TcpMessageReceivedEventArgs e)
        {
            var tcpMessage = new TcpMessage(e.Bytes);
            if (tcpMessage.IsValid)
            {
                switch (tcpMessage.Action)
                {
                    case TcpConstants.MoveTokenAction:
                        MoveToken(tcpMessage);
                        break;
                    default:
                        break;
                }
            }
        }

        private void MoveToken(TcpMessage tcpMessage)
        {
            if (TcpCommandMessage.TryParse(tcpMessage, out var tcpImageMessage))
            {
                var direction = Enum.Parse<TokenDirection>(tcpImageMessage.Arguments[1]);

                var rawName = tcpImageMessage.Arguments[0];
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
