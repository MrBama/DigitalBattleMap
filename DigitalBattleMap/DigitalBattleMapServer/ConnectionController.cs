using Microsoft.AspNetCore.SignalR;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections.Concurrent;
using DigitalBattleMapServer;
using DigitalBattleMap;
using System;
using System.Drawing;
using System.Diagnostics;
using System.Data;
using DigitalBattleMap.Common;
using DigitalBattleMap.Common.DigitalBattleMap.Common;

namespace WebServer
{
    public class ConnectionController
    {
        private static readonly ConnectionController _instance = new ConnectionController();

        private IHubContext<MapHub> _hubContext;
        private Thread _thread;
        private bool _isTerminated = false;
        private bool _isConnected = false;
        private Dictionary<string, int> _imageNameNumbers = new Dictionary<string, int>();
        private ConcurrentQueue<TcpCommandMessage> _messageQueue = new ConcurrentQueue<TcpCommandMessage>();
        private TcpMessageHandler _tcpMessageHandler;

        public static ConnectionController GetInstance()
        {
            return _instance;
        }

        private ConnectionController()
        {
            _imageNameNumbers[TcpConstants.UpdateMapBackgroundAction] = 0;
            _imageNameNumbers[TcpConstants.UpdateMapGridAndStrokesAction] = 0;
            _imageNameNumbers[TcpConstants.UpdateMapTokensAction] = 0;

            _tcpMessageHandler = new TcpMessageHandler(() => _isTerminated);
            _tcpMessageHandler.MessageReceived += OnMessageReceived;
        }

        public void ButtonPressed(string name, string direction)
        {
            if (_isConnected)
            {
                var tcpMessage = new TcpCommandMessage(TcpConstants.MoveTokenAction);
                tcpMessage.Arguments.Add(name);
                tcpMessage.Arguments.Add(direction);
                _messageQueue.Enqueue(tcpMessage);
            }
        }

        public void Initialize(IHubContext<MapHub> hubContext)
        {
            _hubContext = hubContext;

            _thread = new Thread(StartServer);
            _thread.Start();
        }

        public void Terminate()
        {
            _isTerminated = true;
            _thread?.Join();
        }

        private void StartServer()
        {
            try
            {
                Server();
            }
            catch (TerminateException)
            {
                return;
            }
        }

        private void Server()
        {
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 8001);
            var listener = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(ipEndPoint);
            listener.Listen(100);
            listener.ReceiveBufferSize = 10_000_000; // 10 mb, this makes receiving a lot faster because there is less overhead

            while (!_isTerminated)
            {
                var acceptConnectionTask = listener.AcceptAsync();
                WaitOnTaskCompletion(acceptConnectionTask);
                _isConnected = true;

                var client = acceptConnectionTask.Result;

                while (!_isTerminated && _isConnected)
                {
                    if (client.Available > 0)
                    {
                        var receivedBuffer = new byte[client.Available];
                        var receiveTask = client.ReceiveAsync(receivedBuffer, SocketFlags.None);
                        WaitOnTaskCompletion(receiveTask);
                        _tcpMessageHandler.ReceivedBytes(receivedBuffer);
                    }

                    if (_isConnected && _messageQueue.TryDequeue(out var tcpMessage))
                    {
                        var sendTask = client.SendAsync(tcpMessage.GetBytes(), 0);
                        WaitOnTaskCompletion(sendTask);
                    }

                    Thread.Sleep(10);
                }
            }
        }

        private void OnMessageReceived(object sender, TcpMessageReceivedEventArgs e)
        {
            var tcpMessage = new TcpMessage(e.Bytes);
            if (tcpMessage.IsValid)
            {
                switch (tcpMessage.Action)
                {
                    case TcpConstants.UpdateMapBackgroundAction:
                        UpdateMap(tcpMessage);
                        break;
                    case TcpConstants.UpdateMapGridAndStrokesAction:
                        UpdateMap(tcpMessage);
                        break;
                    case TcpConstants.UpdateMapTokensAction:
                        UpdateMap(tcpMessage);
                        break;
                    case TcpConstants.TerminateAction:
                        _isConnected = false;
                        break;
                    default:
                        break;
                }
            }
        }

        private void UpdateMap(TcpMessage tcpMessage)
        {
            if (TcpImageMessage.TryParse(tcpMessage, out var tcpImageMessage))
            {
                var action = tcpImageMessage.Action;
                var imageName = $"{action}{_imageNameNumbers[action]}.png";
                _imageNameNumbers[action] += 1;
                if(_imageNameNumbers[action] > 9)
                {
                    _imageNameNumbers[action] = 0;
                }

                if (File.Exists($@"wwwroot/{imageName}"))
                {
                    File.Delete($@"wwwroot/{imageName}");
                }

                tcpImageMessage.Bitmap.Save($@"wwwroot/{imageName}");
                tcpImageMessage.Bitmap.Dispose();

                var updateClientsTask = _hubContext.Clients.All.SendAsync("UpdateMap", action, imageName + "?t=" + DateTime.Now.Ticks);
                WaitOnTaskCompletion(updateClientsTask);
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
    }

    public class TerminateException : Exception
    {
    }
}
