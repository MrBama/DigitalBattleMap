using DigitalBattleMap.Common;
using DigitalBattleMap.Common.Dto;
using DigitalBattleMap.Imaging;
using DigitalBattleMap.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DigitalBattleMap.DataClasses;
public class MapUpdate : IWebMessage
{
    public DrawLayer Layer { get; set; }
    public IImage Bitmap { get; set; }

    public HttpRequestMessage CreateHttpRequestMessage()
    {
        var dto = new MapUpdateDto { Layer = Layer, Data = Bitmap.Serialize() };
        string json = JsonSerializer.Serialize(dto);

        StringContent content = new(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        HttpRequestMessage message = new(HttpMethod.Post, "/Map/Set") { Content = content };
        return message;
    }
}
