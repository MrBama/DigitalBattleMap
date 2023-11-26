using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Windows.Controls;
using System.Windows;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace DigitalBattleMap.UIElements;

public class MultiSelectionBehavior : Behavior<ListView>
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

    public bool InternalSet { get; set; }

    public IList SelectedItems
    {
        get { return (IList)GetValue(SelectedItemsProperty); }
        set { SetValue(SelectedItemsProperty, value); }
    }

    public ICommand SelectedItemsChangedCommand
    {
        get { return (ICommand)GetValue(SelectedItemsChangedCommandProperty); }
        set { SetValue(SelectedItemsChangedCommandProperty, value); }
    }

    public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(nameof(SelectedItems), typeof(IList), typeof(MultiSelectionBehavior), new UIPropertyMetadata(null, OnSelectedItemsPropertyChanged));
    public static readonly DependencyProperty SelectedItemsChangedCommandProperty = DependencyProperty.Register(nameof(SelectedItemsChangedCommand), typeof(ICommand), typeof(MultiSelectionBehavior), new UIPropertyMetadata(null));

    private void ListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        InternalSet = true;

        if (AssociatedObject.SelectedItem != null)
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

        InternalSet = false;
        SelectedItemsChangedCommand?.Execute(e);
    }

    private static void OnSelectedItemsPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var multiSelectionBehavior = (MultiSelectionBehavior)dependencyObject;
        if (!multiSelectionBehavior.InternalSet)
        {
            multiSelectionBehavior.AssociatedObject.SelectedItems.Clear();
            foreach (var item in (IList)eventArgs.NewValue)
            {
                multiSelectionBehavior.AssociatedObject.SelectedItems.Add(item);
            }
        }
    }
}
