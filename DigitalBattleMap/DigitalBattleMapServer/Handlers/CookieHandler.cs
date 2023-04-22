using System.Text.Json;

namespace DigitalBattleMapServer.Handlers;

public class CookieHandler : ICookieHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private HttpRequest Request => _httpContextAccessor.HttpContext.Request;
    private HttpResponse Response => _httpContextAccessor.HttpContext.Response;
    
    public CookieHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public T Get<T>(string key)
    {
        if (Request.Cookies.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value))
            return JsonSerializer.Deserialize<T>(value);

        return default;
    }

    public void Set<T>(string key, T value)
    {
        string json = string.Empty;
        if (!EqualityComparer<T>.Default.Equals(value, default))
            json = JsonSerializer.Serialize(value);
        
        if (HasKey(key))
            Delete(key);

        var options = new CookieOptions
        {
            Expires = DateTime.MaxValue
        };
        Response.Cookies.Append(key, json, options);
    }

    public void Delete(string key)
    {
        Response.Cookies.Delete(key);
    }

    private bool HasKey(string key)
    {
        return Request.Cookies.ContainsKey(key);
    }
}