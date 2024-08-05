using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;

namespace DigitalBattleMap.ViewModels;
public abstract class ControllerViewModelBase : ViewModelBase
{
    protected IMapSize _mapSize;


    public ControllerViewModelBase()
    {
    }

    public ControllerViewModelBase(IMapSize mapSize)
    {
        _mapSize = mapSize;
    }

    public abstract void AddToSaveFile(SaveFile saveFile);

    public abstract void OpenSaveFile(SaveFile saveFile);

    public abstract void Zoom(double zoomFactor);

    public abstract void Move(ArrowDirection arrowDirection, int movementCount, bool update = true);
}
