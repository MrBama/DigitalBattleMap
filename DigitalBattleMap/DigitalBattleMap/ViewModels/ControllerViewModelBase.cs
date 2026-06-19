using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using System.Collections.Generic;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public abstract class ControllerViewModelBase : ViewModelBase
{
    protected IMapSize _mapSize;
    protected bool _pauseBitmapCreation;
    protected HashSet<Key> _pressedKeys = new();

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

    public virtual void KeyDown(KeyEventArgs keyEventArgs)
    {
        _pressedKeys.Add(keyEventArgs.Key);
    }

    public virtual void KeyUp(KeyEventArgs keyEventArgs)
    {
        _pressedKeys.Remove(keyEventArgs.Key);
    }

    protected abstract void CreateBitmap();
}
