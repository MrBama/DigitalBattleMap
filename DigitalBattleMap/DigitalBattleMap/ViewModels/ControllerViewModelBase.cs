using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;

namespace DigitalBattleMap.ViewModels;
public abstract class ControllerViewModelBase : ViewModelBase
{
    protected IMapSize _mapSize;
    protected bool _pauseBitmapCreation;

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

    public abstract void Move(ArrowDirection arrowDirection, int movementCount);

    public void PauseBitmapCreation()
    {
        _pauseBitmapCreation = true;
    }

    public void ResumeBitmapCreation()
    {
        _pauseBitmapCreation = false;
        CreateBitmap();
    }

    protected abstract void CreateBitmap();
}
