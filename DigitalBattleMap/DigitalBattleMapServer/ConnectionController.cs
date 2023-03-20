using Microsoft.AspNetCore.SignalR;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections.Concurrent;
using DigitalBattleMapServer;
using DigitalBattleMap;

namespace WebServer
{
    public class ConnectionController
    {
        private static readonly ConnectionController _instance = new ConnectionController();

        private IHubContext<WebHub> _hubContext;
        private Thread _thread;
        private bool _isTerminated = false;
        private ConcurrentQueue<TcpMessage> _messageQueue = new ConcurrentQueue<TcpMessage>();

        public static ConnectionController GetInstance()
        {
            return _instance;
        }

        public void ButtonPressed(string name, string direction)
        {
            var tcpMessage = new TcpMessage("MoveToken");
            tcpMessage.Arguments.Add(name);
            tcpMessage.Arguments.Add(direction);
            _messageQueue.Enqueue(tcpMessage);
        }

        public void Initialize(IHubContext<WebHub> hubContext)
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

            while (!_isTerminated)
            {
                var acceptConnectionTask = listener.AcceptAsync();
                WaitOnTaskCompletion(acceptConnectionTask);

                var client = acceptConnectionTask.Result;
                _messageQueue.Clear();

                while (!_isTerminated)
                {
                    if (client.Available > 0)
                    {
                        var buffer = new byte[client.Available];
                        var receiveTask = client.ReceiveAsync(buffer, SocketFlags.None);
                        WaitOnTaskCompletion(receiveTask);

                        var indexOfTerminate = SearchBytes(buffer, Encoding.UTF8.GetBytes("<Terminate>"));
                        if (indexOfTerminate != -1)
                        {
                            break;
                        }
                    }

                    if (_messageQueue.TryDequeue(out var tcpMessage))
                    {
                        var sendTask = client.SendAsync(tcpMessage.GetBytes(), 0);
                        WaitOnTaskCompletion(sendTask);
                    }
                    Thread.Sleep(10);
                }
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

        static int SearchBytes(byte[] haystack, byte[] needle)
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
    }

    public class TerminateException : Exception
    {
    }
}
