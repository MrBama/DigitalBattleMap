namespace DigitalBattleMapServer.Handlers;

public interface ICookieHandler
{
    T Get<T>(string key);
    
    void Set<T>(string key, T value);
    
    void Delete(string key);
}