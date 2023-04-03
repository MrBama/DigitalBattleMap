using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using DigitalBattleMap.Common;
using DigitalBattleMap.Common.Dto;
using DigitalBattleMap.Events;
using DigitalBattleMap.Utility;
using Microsoft.AspNetCore.SignalR.Client;

namespace DigitalBattleMap
{
    public class ConnectionManager : IWebHub, IWebHubClientEvents
    {
        // TODO: Move to configuration file
        private const string BaseConnectionUrl = "http://localhost:8000";
        //private const string BaseConnectionUrl = "https://digitalbattlemapserver.azurewebsites.net/";
        private const string WebHubConnectionUrl = BaseConnectionUrl + "/WebHub";
        private const string MapHubConnectionUrl = BaseConnectionUrl + "/MapHub";

        // HttpClients
        private readonly HttpClient _httpClient;
        
        // Hubs
        private readonly HubConnection _webHubConnection;
        private readonly HubConnection _mapHubConnection;

        // Events
        public event EventHandler<EventArgs> OnConnected;
        public event EventHandler<EventArgs> OnDisconnect;
        public event IWebHubClientEvents.MoveTokenActionEventHandler OnMoveToken;

        private bool _isConnected;

        public ConnectionManager()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseConnectionUrl);

            _webHubConnection = new HubConnectionBuilder()
                .WithUrl(WebHubConnectionUrl)
                .Build();

            _mapHubConnection = new HubConnectionBuilder()
                .WithUrl(MapHubConnectionUrl)
                .Build();

            Configure();
        }
        
        public void Connect(string ipAddress, int port)
        {
            Task.Run(async () =>
            {
                await Task.WhenAll(
                    _webHubConnection.StartAsync(),
                    _mapHubConnection.StartAsync());
            }).Wait();

            _isConnected = true;
            OnConnected?.Invoke(this, EventArgs.Empty);
        }

        public void Disconnect()
        {
            Task.Run(async () =>
            {
                await Task.WhenAll(
                    _webHubConnection.StopAsync(),
                    _mapHubConnection.StopAsync());
            }).Wait();

            _isConnected = false;

            OnDisconnect?.Invoke(this, EventArgs.Empty);
        }

        public Task MoveToken(string character, Direction direction)
        {
            OnMoveToken?.Invoke(this, new MoveTokenActionEventArgs() { Name = character, Direction = direction });
            
            // TODO: Is this clean, can we do without Task?
            return Task.FromResult<object>(null);
        }
        
        public void SendMapUpdate(MapUpdateDto mapUpdate)
        {
            if (!_isConnected)
                return;

            string json = JsonSerializer.Serialize(mapUpdate);

            StringContent content = new(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpRequestMessage message = new (HttpMethod.Post, $"{BaseConnectionUrl}/Map/Set") { Content = content };
            _httpClient.Send(message);
        }

        public void ClearMap()
        {
            if (!_isConnected)
                return;

            HttpRequestMessage message = new(HttpMethod.Delete, $"{BaseConnectionUrl}/Map/Delete");
            message.Headers.Add("Layer", DrawLayer.All.ToString());
            _httpClient.Send(message);
        }

        private void Configure()
        {
            _webHubConnection.On<string, Direction>(nameof(MoveToken), MoveToken);
        }
    }
}
