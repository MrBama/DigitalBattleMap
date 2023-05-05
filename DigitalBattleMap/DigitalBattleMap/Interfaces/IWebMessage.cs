using System.Net.Http;

namespace DigitalBattleMap.Interfaces;
public interface IWebMessage
{
    HttpRequestMessage CreateHttpRequestMessage();
}
