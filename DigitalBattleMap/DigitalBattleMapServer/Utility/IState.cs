namespace DigitalBattleMapServer.Utility;

public interface IState<T>
{
    T Get();

    void Set(T value);
}