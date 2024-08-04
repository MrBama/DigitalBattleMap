using DigitalBattleMap.Common;
using DigitalBattleMap.Common.Dto;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DigitalBattleMap.DataClasses;
class ClearMapMessage : IWebMessage
{
    public HttpRequestMessage CreateHttpRequestMessage()
    {
        HttpRequestMessage message = new(HttpMethod.Delete, "/Map/Delete");
        message.Headers.Add("Layer", DrawLayer.All.ToString());
        return message;
    }
}

class ConditionsMessage : IWebMessage
{
    public TokenIdentifier TokenIdentifier { get; set; } = new();
    public List<Condition> Conditions { get; set; } = new();

    public HttpRequestMessage CreateHttpRequestMessage()
    {
        var dto = new ConditionsDto { Character = TokenIdentifier.GetCombinedString(), Conditions = Conditions };
        string json = JsonSerializer.Serialize(dto);

        StringContent content = new(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        HttpRequestMessage message = new(HttpMethod.Post, "/Navigation/SetConditions") { Content = content };
        return message;
    }
}

class TokensMessage : IWebMessage
{
    public string Player { get; set; } = "";
    public List<string> Tokens { get; set; } = new();

    public HttpRequestMessage CreateHttpRequestMessage()
    {
        var dto = new TokensDto { Player = Player, Tokens = Tokens };
        string json = JsonSerializer.Serialize(dto);

        StringContent content = new(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        HttpRequestMessage message = new(HttpMethod.Post, "/Navigation/SetTokens") { Content = content };
        return message;
    }
}

class CampaignMessage : IWebMessage
{
    public List<Player> Players { get; set; } = new();

    public HttpRequestMessage CreateHttpRequestMessage()
    {
        var dto = new CampaignDto();
        foreach (var player in Players)
        {
            dto.Players[player.Name] = player.TokenIdentifiers.ToStringList();
        }

        string json = JsonSerializer.Serialize(dto);

        StringContent content = new(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        HttpRequestMessage message = new(HttpMethod.Post, "/Navigation/SetCampaign") { Content = content };
        return message;
    }
}