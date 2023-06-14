using DigitalBattleMap.Common;
using DigitalBattleMap.Common.Dto;
using DigitalBattleMap.Interfaces;
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
    public string Character { get; set; } = "";
    public List<Condition> Conditions { get; set; } = new();

    public HttpRequestMessage CreateHttpRequestMessage()
    {
        var dto = new ConditionsDto { Character = Character, Conditions = Conditions };
        string json = JsonSerializer.Serialize(dto);

        StringContent content = new(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        HttpRequestMessage message = new(HttpMethod.Post, "/Navigation/SetConditions") { Content = content };
        return message;
    }
}
