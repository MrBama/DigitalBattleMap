using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace DigitalBattleMap.UIElements;

public class ScrollToBottomBehavior : Behavior<ScrollViewer>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ScrollChanged += ScrollChanged;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.ScrollChanged -= ScrollChanged;
    }

    private static void ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Only scroll to bottom when the extent changed. Otherwise you can't scroll up
        if (e.ExtentHeightChange != 0)
        {
            var scrollViewer = sender as ScrollViewer;
            scrollViewer?.ScrollToBottom();
        }
    }
}