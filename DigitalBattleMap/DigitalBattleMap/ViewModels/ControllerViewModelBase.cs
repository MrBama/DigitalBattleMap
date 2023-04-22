using DigitalBattleMap.DataClasses;

namespace DigitalBattleMap.ViewModels;
public abstract class ControllerViewModelBase : ViewModelBase
{
    protected Size<double> _canvasSize;
    protected int _gridSize;

    public ControllerViewModelBase(int gridSize)
    {
        _gridSize = gridSize;
    }

    public virtual void UpdateGridSize(int gridSize)
    {
        _gridSize = gridSize;
    }

    public virtual void SetCanvasSize(Size<double> canvasSize)
    {
        _canvasSize = canvasSize;
    }

    public abstract void AddToSaveFile(SaveFile saveFile);

    public abstract void OpenSaveFile(SaveFile saveFile);

    public abstract void Zoom(double zoomFactor);

    public abstract void Move(ArrowDirection arrowDirection);
}
