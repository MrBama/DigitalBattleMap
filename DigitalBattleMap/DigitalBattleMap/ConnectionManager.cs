using DigitalBattleMap.Common;
using DigitalBattleMap.Common.Dto;
using DigitalBattleMap.DataClasses;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
    public class ConnectionManager : IWebHub, IWebHubClientEvents
    {
        private const string WebHubConnectionEndpoint = "/WebHub";
        private const string MapHubConnectionEndpoint = "/MapHub";

        // HttpClients
        private HttpClient _httpClient;
        
        // Hubs
        private HubConnection _webHubConnection;
        private HubConnection _mapHubConnection;

        // Events
        public event EventHandler<EventArgs> OnConnected;
        public event EventHandler<EventArgs> OnDisconnect;
        public event IWebHubClientEvents.MoveTokenActionEventHandler OnMoveToken;

        private bool _isConnected;

        public void Connect(string address)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(address) };

            _webHubConnection = new HubConnectionBuilder()
                .WithUrl($"{address}{WebHubConnectionEndpoint}")
                .Build();

            _mapHubConnection = new HubConnectionBuilder()
                .WithUrl($"{address}{MapHubConnectionEndpoint}")
                .Build();

            Task.Run(async () =>
            {
                await Task.WhenAll(
                    _webHubConnection.StartAsync(),
                    _mapHubConnection.StartAsync());
            }).Wait();

            Configure();

            _isConnected = true;
            OnConnected?.Invoke(this, EventArgs.Empty);
        }

        public void Disconnect()
        {
            if (!_isConnected)
                return;

            Task.Run(async () =>
            {
                await Task.WhenAll(
                    _webHubConnection.StopAsync(),
                    _webHubConnection.DisposeAsync().AsTask(),

                    _mapHubConnection.StopAsync(),
                    _mapHubConnection.DisposeAsync().AsTask());
            }).Wait();

            _httpClient.Dispose();

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
            HttpRequestMessage message = new (HttpMethod.Post, "/Map/Set") { Content = content };
            _httpClient.Send(message);
        }

        public void ClearMap()
        {
            if (!_isConnected)
                return;

            HttpRequestMessage message = new(HttpMethod.Delete, "/Map/Delete");
            message.Headers.Add("Layer", DrawLayer.All.ToString());
            _httpClient.Send(message);
        }

        private void Configure()
        {
            _webHubConnection.On<string, Direction>(nameof(MoveToken), MoveToken);
        }
    }
}
