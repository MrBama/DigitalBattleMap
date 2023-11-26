using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;

namespace DigitalBattleMap.UIElements;

public class ScrollIntoViewBehavior : Behavior<ListView>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.SelectionChanged += ListViewSelectionChanged;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.SelectionChanged -= ListViewSelectionChanged;

    }

    void ListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listview = (ListView)sender;
        if (listview.SelectedItem != null)
        {
            listview.Dispatcher.BeginInvoke(() =>
            {
                listview.UpdateLayout();
                if (listview.SelectedItem != null)
                {
                    listview.ScrollIntoView(listview.SelectedItem);
                }
            });
        }
    }
}
