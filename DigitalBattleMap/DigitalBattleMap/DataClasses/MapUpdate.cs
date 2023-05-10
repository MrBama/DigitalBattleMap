using DigitalBattleMap.Common;
using DigitalBattleMap.Common.Dto;
using DigitalBattleMap.Utilities;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DigitalBattleMap.DataClasses;
public class MapUpdate
{
    public DrawLayer Layer { get; set; }
    public Bitmap Bitmap { get; set; }

    public HttpRequestMessage CreateHttpRequestMessage()
    {
        var dto = new MapUpdateDto { Layer = Layer, Data = Bitmap.ToPng() };
        string json = JsonSerializer.Serialize(dto);

        StringContent content = new(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        HttpRequestMessage message = new(HttpMethod.Post, "/Map/Set") { Content = content };
        return message;
    }
}
