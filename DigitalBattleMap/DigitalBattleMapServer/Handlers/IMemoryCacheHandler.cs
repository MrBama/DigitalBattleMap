namespace DigitalBattleMapServer.Handlers;

public interface IMemoryCacheHandler
{
    byte[] Get(string key);

    void Set(string key, byte[] value);

    void Delete(string key);
}