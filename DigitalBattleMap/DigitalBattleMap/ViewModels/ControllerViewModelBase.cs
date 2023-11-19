using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;

namespace DigitalBattleMap.ViewModels;
public abstract class ControllerViewModelBase : ViewModelBase
{
    protected ICanvasSize _canvasSize;


    public ControllerViewModelBase()
    {
    }

    public ControllerViewModelBase(ICanvasSize canvasSize)
    {
        _canvasSize = canvasSize;
    }

    public abstract void AddToSaveFile(SaveFile saveFile);

    public abstract void OpenSaveFile(SaveFile saveFile);

    public abstract void Zoom(double zoomFactor);

    public abstract void Move(ArrowDirection arrowDirection, int movementCount);
}
