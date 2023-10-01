using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;

namespace DigitalBattleMap.ViewModels;
public abstract class ControllerViewModelBase : ViewModelBase
{
    protected ICanvasSize _canvasSize;
    protected int _gridSize;

    public ControllerViewModelBase()
    {
    }

    public ControllerViewModelBase(ICanvasSize canvasSize, int gridSize)
    {
        _gridSize = gridSize;
        _canvasSize = canvasSize;
    }

    public virtual void UpdateGridSize(int gridSize)
    {
        _gridSize = gridSize;
    }

    public abstract void AddToSaveFile(SaveFile saveFile);

    public abstract void OpenSaveFile(SaveFile saveFile);

    public abstract void Zoom(double zoomFactor);

    public abstract void Move(ArrowDirection arrowDirection, int movementCount);
}
