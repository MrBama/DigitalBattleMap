using DigitalBattleMap.Utilities;

namespace DigitalBattleMap.ViewModels;
public abstract class ViewModelBase : PropertyHandler
{
    public ViewModelBase()
    {
        InitializeCommands();
    }

    protected abstract void InitializeCommands();
}
