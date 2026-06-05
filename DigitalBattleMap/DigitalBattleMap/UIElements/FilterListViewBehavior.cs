using Microsoft.Xaml.Behaviors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DigitalBattleMap.UIElements;

public class FilterListViewBehavior : Behavior<ListView>
{
    public FilterListViewBehavior()
    {
        FilteredItems = new();
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        // Try to apply the filter immediately
        ApplyFilter();

        // Just in case ItemsSource changes later, listen for dependency property changes
        var dpd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListView));
        dpd?.AddValueChanged(AssociatedObject, (_, _) => ApplyFilter());
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        var dpd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListView));
        dpd?.RemoveValueChanged(AssociatedObject, (_, _) => ApplyFilter());
    }

    public static readonly DependencyProperty FilterKeywordProperty = DependencyProperty.Register(nameof(FilterKeyword), typeof(string), typeof(FilterListViewBehavior), new UIPropertyMetadata(null, OnFilterKeywordPropertyChanged));
    public static readonly DependencyProperty FilterPropertyNameProperty = DependencyProperty.Register(nameof(FilterPropertyName), typeof(string), typeof(FilterListViewBehavior));
    public static readonly DependencyProperty KeepSelectionProperty = DependencyProperty.Register(nameof(KeepSelection), typeof(bool), typeof(FilterListViewBehavior));
    public static readonly DependencyProperty FilteredItemsProperty = DependencyProperty.Register(nameof(FilteredItems), typeof(List<object>), typeof(FilterListViewBehavior));

    public string FilterKeyword
    {
        get { return (string)GetValue(FilterKeywordProperty); }
        set { SetValue(FilterKeywordProperty, value); }
    }

    public string FilterPropertyName
    {
        get { return (string)GetValue(FilterPropertyNameProperty); }
        set { SetValue(FilterPropertyNameProperty, value); }
    }

    public bool KeepSelection
    {
        get { return (bool)GetValue(KeepSelectionProperty); }
        set { SetValue(KeepSelectionProperty, value); }
    }

    public List<object> FilteredItems
    {
        get { return (List<object>)GetValue(FilteredItemsProperty); }
        set { SetValue(FilteredItemsProperty, value); }
    }

    public void ApplyFilter()
    {
        if (AssociatedObject?.ItemsSource == null || FilterKeyword == null)
        {
            return;
        }

        var listCollectionView = CollectionViewSource.GetDefaultView(AssociatedObject.ItemsSource) as ListCollectionView;
        if (listCollectionView == null)
        {
            return;
        }

        // Save a copy of the selected items
        var savedSelections = new List<object>();
        if (KeepSelection)
        {
            foreach (var selectedItem in AssociatedObject.SelectedItems)
            {
                savedSelections.Add(selectedItem);
            }
        }

        // Set filter and custom sort, then refresh the view
        listCollectionView.Filter = (item) => FilterRule(item, savedSelections);
        listCollectionView.CustomSort = new FilterPropertyComparer(FilterKeyword, FilterPropertyName);
        listCollectionView.Refresh();

        // Select first item or previous selected items
        AssociatedObject.SelectedItems.Clear();
        if(KeepSelection)
        {
            foreach (var item in savedSelections)
            {
                AssociatedObject.SelectedItems.Add(item);
            }
        }
        else
        {
            if(AssociatedObject.Items.Count > 0)
            {
                AssociatedObject.SelectedItems.Add(AssociatedObject.Items[0]);
            }
        }

        // Set filtered items
        FilteredItems.Clear();
        foreach (var item in AssociatedObject.Items)
        {
            FilteredItems.Add(item);
        }
    }

    private bool FilterRule(object item, IList selectedItems)
    {
        var itemProperty = item.GetType().GetProperty(FilterPropertyName);
        if (itemProperty == null)
        {
            throw new ArgumentException($"No property found with name: {FilterPropertyName}");
        }

        var itemValue = itemProperty.GetValue(item, null) as string;
        if (itemValue == null)
        {
            throw new ArgumentException($"The value of the property \"{FilterPropertyName}\" is not equal to type string");
        }

        // Filter rules:
        // 1. item (string) contains keyword
        // 2. Current list of selected items contains item

        return itemValue.ToLower().Contains(FilterKeyword.ToLower()) || selectedItems.Contains(item);
    }

    private static void OnFilterKeywordPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var filterListViewBehavior = (FilterListViewBehavior)dependencyObject;
        if (filterListViewBehavior.AssociatedObject != null)
        {
            filterListViewBehavior.ApplyFilter();
        }
    }

    private class FilterPropertyComparer : IComparer
    {
        private string _filterPropertyName;
        private string _filterKeyword;

        public FilterPropertyComparer(string filterKeyword, string filterPropertyName)
        {
            _filterKeyword = filterKeyword.ToLower();
            _filterPropertyName = filterPropertyName;
        }

        public int Compare(object x, object y)
        {
            var xProperty = x.GetType().GetProperty(_filterPropertyName);
            var yProperty = x.GetType().GetProperty(_filterPropertyName);

            if (xProperty == null || yProperty == null)
            {
                throw new ArgumentException($"No property found with name: {_filterPropertyName}");
            }

            var xValue = xProperty.GetValue(x, null) as string;
            var yValue = yProperty.GetValue(y, null) as string;

            if(xValue == null || yValue == null)
            {
                throw new ArgumentException($"The value of the property \"{_filterPropertyName}\" is not equal to type string");
            }

            xValue = xValue.ToLower();
            yValue = yValue.ToLower();

            // Filter rules:
            // 1. Full name equals keyword
            // 2. First name equals keyword
            // 3. First name contains keyword
            // 4. Alphabetical

            // Input:
            //
            // A zombie human
            // LaZombiefied human
            // The zombie human
            // Zombie
            // Zombie human

            // Output (keyword = "Zombie"):
            //
            // Zombie
            // Zombie human
            // LaZombiefied human
            // A zombie human
            // The zombie human


            // Rule 1: Full name equals keyword
            if (xValue == _filterKeyword && yValue == _filterKeyword)
            {
                return Result.Equal;
            }
            else if (xValue == _filterKeyword)
            {
                return Result.XPrecedesY;
            }
            else if (yValue == _filterKeyword)
            {
                return Result.XFollowsY;
            }

            // Rule 2: First name equals keyword
            if (xValue.Split(" ")[0] == _filterKeyword && yValue.Split(" ")[0] == _filterKeyword)
            {
                return string.Compare(xValue, yValue, StringComparison.OrdinalIgnoreCase);
            }
            else if (xValue.Split(" ")[0] == _filterKeyword)
            {
                return Result.XPrecedesY;
            }
            else if (yValue.Split(" ")[0] == _filterKeyword)
            {
                return Result.XFollowsY;
            }

            // Rule 3: First name contains keyword
            if (xValue.Split(" ")[0].Contains(_filterKeyword) && yValue.Split(" ")[0].Contains(_filterKeyword))
            {
                return string.Compare(xValue, yValue, StringComparison.OrdinalIgnoreCase);
            }
            else if (xValue.Split(" ")[0].Contains(_filterKeyword))
            {
                return Result.XPrecedesY;
            }
            else if (yValue.Split(" ")[0].Contains(_filterKeyword))
            {
                return Result.XFollowsY;
            }

            // Rule 4: Alphabetical
            return string.Compare(xValue, yValue, StringComparison.OrdinalIgnoreCase);
        }

        private class Result
        {
            public const int Equal = 0;
            public const int XPrecedesY = -1;
            public const int XFollowsY = 1;
        }
    }
}
