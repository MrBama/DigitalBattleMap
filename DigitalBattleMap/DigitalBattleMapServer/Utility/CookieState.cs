using DigitalBattleMapServer.Handlers;

namespace DigitalBattleMapServer.Utility;

public class CookieState<T> : IState<T>
{
    private readonly ICookieHandler _cookieHandler;
    private readonly string _name;

    public CookieState(ICookieHandler cookieHandler)
    {
        _name = $"_{typeof(T).Name.ToLower()}";
        _cookieHandler = cookieHandler;
    }

    public T Get()
    {
        return _cookieHandler.Get<T>(_name);
    }

    public void Set(T value)
    {
        _cookieHandler.Set(_name, value);
    }
}