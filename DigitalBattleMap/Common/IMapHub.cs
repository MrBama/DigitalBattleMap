namespace DigitalBattleMap.Common;

public interface IMapHub
{
    Task UpdateMap(DrawLayer layer);
    Task UpdatePauseStatus(bool isPaused);
}