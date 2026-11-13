namespace DigitalBattleMap.Common;

public interface IMapHub
{
    // Server to client
    Task UpdateMap(DrawLayer layer);
    Task UpdatePauseStatus(bool isPaused);
    Task EnablePing(double x, double y);
    Task DisablePing();

    // Client to server
    Task EnablePingFromClient(double x, double y);
}