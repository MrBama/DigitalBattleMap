using DigitalBattleMap.Common;
using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalBattleMap;

public class ConnectionManager : IWebCommunication
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
    public event EventHandler<DisconnectedEventArgs> OnDisconnect;
    public event EventHandler<MoveTokenEventArgs> OnMoveToken;
    public event EventHandler<ToggleConditionEventArgs> OnToggleCondition;
    public event EventHandler<GetConditionsEventArgs> OnGetConditions;
    public event EventHandler<SetOrientationEventArgs> OnSetOrientation;
    public event EventHandler<SetHeightEventArgs> OnSetHeight;

    private bool _isConnected;
    private Queue<MapUpdate> _mapUpdateQueue = new();
    private ConcurrentQueue<IWebMessage> _messageQueue = new();
    private Thread _thread;
    private static object _lock = new();
    private bool _playerControlAllowed;

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
                OnDisconnect?.Invoke(this, new DisconnectedEventArgs());
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

        SendHttpMessage(new ClearCacheMessage());
        Disconnect(false);
    }

    public void UpdatePlayerControlAllowed(bool isAllowed)
    {
        _playerControlAllowed = isAllowed;
    }

    public Task MoveToken(string character, Direction direction)
    {
        if (_playerControlAllowed)
        {
            OnMoveToken?.Invoke(this, new MoveTokenEventArgs { TokenIdentifier = new TokenIdentifier(character), Direction = direction });
        }

        return Task.CompletedTask;
    }

    public Task ToggleCondition(string character, Condition condition)
    {
        if (_playerControlAllowed)
        {
            OnToggleCondition?.Invoke(this, new ToggleConditionEventArgs { TokenIdentifier = new TokenIdentifier(character), Condition = condition });
        }

        return Task.CompletedTask;
    }

    public Task GetConditions(string character)
    {
        OnGetConditions?.Invoke(this, new GetConditionsEventArgs { TokenIdentifier = new TokenIdentifier(character) });
        return Task.CompletedTask;
    }

    public Task SetOrientation(string player, Orientation orientation)
    {
        OnSetOrientation?.Invoke(this, new SetOrientationEventArgs { Player = player, Orientation = orientation });
        return Task.CompletedTask;
    }

    public Task SetHeight(string character, int height)
    {
        if (_playerControlAllowed)
        {
            OnSetHeight?.Invoke(this, new SetHeightEventArgs { TokenIdentifier = new TokenIdentifier(character), Height = height });
        }

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

        _messageQueue.Enqueue(new ClearMapMessage());
    }

    public void SendMessage(IWebMessage webMessage)
    {
        if (!_isConnected)
            return;

        _messageQueue.Enqueue(webMessage);
    }

    private void Disconnect(bool isConnectionLost)
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
            _thread?.Join();

            OnDisconnect?.Invoke(this, new DisconnectedEventArgs { IsConnectionLost = isConnectionLost });
        });
    }

    private void Configure()
    {
        _webHubConnection.On<string, Direction>(nameof(MoveToken), MoveToken);
        _webHubConnection.On<string, Condition>(nameof(ToggleCondition), ToggleCondition);
        _webHubConnection.On<string>(nameof(GetConditions), GetConditions);
        _webHubConnection.On<string, Orientation>(nameof(SetOrientation), SetOrientation);
        _webHubConnection.On<string, int>(nameof(SetHeight), SetHeight);
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
                if (!SendHttpMessage(mapUpdate))
                    return;
            }

            if (_messageQueue.TryDequeue(out var webMessage))
            {
                if (!SendHttpMessage(webMessage))
                    return;
            }
            else if (!newMapUpdate) // Only sleep when there is no mapUpdate or webMessage
            {
                Thread.Sleep(100);
            }
        }
    }

    private bool SendHttpMessage(IWebMessage webMessage)
    {
        try
        {
            _httpClient.SendAsync(webMessage.CreateHttpRequestMessage()).Wait();
        }
        catch // Connection lost
        {
            Disconnect(true);
            return false;
        }

        return true;
    }
}
