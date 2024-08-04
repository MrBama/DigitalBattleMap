using DigitalBattleMap.DataClasses;
using System.Windows;
using System.Windows.Controls;

namespace DigitalBattleMap.UIElements;

public class ListSelectionDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate Default { get; set; }
    public DataTemplate TokenListItem { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is TokenListItem)
        {
            return TokenListItem;
        }
        return Default;
    }
}
