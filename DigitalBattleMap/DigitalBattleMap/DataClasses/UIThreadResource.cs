using System.Windows;
using System.Windows.Threading;

namespace DigitalBattleMap.DataClasses;

public class UIThreadResource
{
    private bool isClaimed;

    public void Claim()
    {
        isClaimed = true;
        Application.Current.Dispatcher.InvokeAsync(() => { while (isClaimed) { } }, DispatcherPriority.Normal);
    }

    public void Release()
    {
        isClaimed = false;
    }
}
