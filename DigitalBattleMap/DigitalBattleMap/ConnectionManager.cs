using DigitalBattleMap.Common;
using DigitalBattleMap.Common.Dto;
using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalBattleMap;

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
    public event IWebHubClientEvents.ToggleConditionActionEventHandler OnToggleCondition;

    private bool _isConnected;
    private Queue<MapUpdate> _mapUpdateQueue = new Queue<MapUpdate>();
    private Thread _thread;
    private object _lock = "";

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
            try
            {
                await Task.WhenAll(
                    _webHubConnection.StartAsync(),
                    _mapHubConnection.StartAsync());
            }
            catch
            {
                // Failed to make a connection
                _httpClient.Dispose();
                OnDisconnect?.Invoke(this, EventArgs.Empty);
                throw;
            }

            Configure();

            _isConnected = true;
            _thread = new Thread(SendMessages);
            _thread.Start();
            OnConnected?.Invoke(this, EventArgs.Empty);
        });
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

            _httpClient.Dispose();

            _isConnected = false;
            if (_thread != null)
            {
                _thread.Join();
            }

            OnDisconnect?.Invoke(this, EventArgs.Empty);
        });
    }

    public Task MoveToken(string character, Direction direction)
    {
        OnMoveToken?.Invoke(this, new MoveTokenActionEventArgs() { Name = character, Direction = direction });

        // TODO: Is this clean, can we do without Task?
        return Task.CompletedTask;
    }

    public Task ToggleCondition(string character, Condition condition)
    {
        OnToggleCondition?.Invoke(this, new ToggleConditionActionEventArgs() { Name = character, Condition = condition });

        // TODO: Is this clean, can we do without Task?
        return Task.CompletedTask;
    }

    public void SendMapUpdate(MapUpdate mapUpdate)
    {
        if (!_isConnected)
            return;

        lock (_lock)
        {
            var existingUpdateCount = _mapUpdateQueue.Count(u => u.Layer != mapUpdate.Layer);
            if (existingUpdateCount > 0)
            {
                _mapUpdateQueue = new Queue<MapUpdate>(_mapUpdateQueue.Where(u => u.Layer != mapUpdate.Layer));
            }
            _mapUpdateQueue.Enqueue(mapUpdate);
        }
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
        _webHubConnection.On<string, Condition>(nameof(ToggleCondition), ToggleCondition);
    }

    private void SendMessages()
    {
        while (_isConnected)
        {
            MapUpdate? mapUpdate;
            var newMapUpdate = false;
            lock (_lock)
            {
                newMapUpdate = _mapUpdateQueue.TryDequeue(out mapUpdate);
            }

            if (newMapUpdate)
            {
                var dto = new MapUpdateDto { Layer = mapUpdate!.Layer, Data = mapUpdate!.Bitmap.ToPng() };
                string json = JsonSerializer.Serialize(dto);

                StringContent content = new(json);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpRequestMessage message = new(HttpMethod.Post, "/Map/Set") { Content = content };

                _httpClient.SendAsync(message).Wait();
            }

            Thread.Sleep(100);
        }

    }
}
