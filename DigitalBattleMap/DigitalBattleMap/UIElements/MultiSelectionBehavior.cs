using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Windows.Controls;
using System.Windows;

namespace DigitalBattleMap.UIElements;

public class MultiSelectionBehavior : Behavior<ListView>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.SelectionChanged += ListViewSelectionChanged;
    }

    public IList SelectedItems
    {
        get { return (IList)GetValue(SelectedItemsProperty); }
        set { SetValue(SelectedItemsProperty, value); }
    }

    public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register("SelectedItems", typeof(IList), typeof(MultiSelectionBehavior), new UIPropertyMetadata(null));

    private void ListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedItems = AssociatedObject.SelectedItems;
    }
}
