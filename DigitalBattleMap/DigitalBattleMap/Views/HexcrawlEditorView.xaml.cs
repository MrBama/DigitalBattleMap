using System.Windows.Controls;
using System.Windows.Input;

namespace DigitalBattleMap.Views;

public partial class HexcrawlEditorView : UserControl
{
    public HexcrawlEditorView()
    {
        InitializeComponent();
    }

    // Clicking the already selected tile or edge type deselects it.
    // With nothing selected, left-click on the map erases painted tiles.
    private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListViewItem { IsSelected: true } item)
        {
            item.IsSelected = false;
            e.Handled = true;
        }
    }
}
