using DigitalBattleMap.DataClasses;

namespace DigitalBattleMap.ViewModels;
public abstract class ControllerViewModelBase : ViewModelBase
{
    protected Size<int> _bitmapSize = BitmapTools.GetBitmapSize();
    protected Size<double> _canvasSize;
    protected int _gridSize;

    public ControllerViewModelBase(int gridSize)
    {
        _gridSize = gridSize;
    }

    public void UpdateGridSize(int gridSize)
    {
        _gridSize = gridSize;
    }

    public void SetCanvasSize(Size<double> canvasSize)
    {
        _canvasSize = canvasSize;
    }

    public abstract void AddToSaveFile(SaveFile saveFile);

    public abstract void OpenSaveFile(SaveFile saveFile);
}
