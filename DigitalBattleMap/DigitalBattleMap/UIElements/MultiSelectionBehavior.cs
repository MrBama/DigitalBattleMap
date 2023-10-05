using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Windows.Controls;
using System.Windows;
using System;
using System.Collections.Generic;

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
        if(AssociatedObject.SelectedItem != null)
        {
            Type listType = typeof(List<>);
            Type genericListType = listType.MakeGenericType(AssociatedObject.SelectedItem.GetType());
            var resultList = Activator.CreateInstance(genericListType) as IList;
            
            foreach (var item in AssociatedObject.SelectedItems)
            {
                resultList!.Add(item);
            }
            SelectedItems = resultList;
        }
        else
        {
            SelectedItems = null;
        }
    }
}
