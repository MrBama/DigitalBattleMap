using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class StatblocksViewModel : ViewModelBase
{
    protected override void InitializeCommands()
    {
        ScrollChangedCommand = new RelayCommand(p => ScrollChanged((ScrollChangedEventArgs)p));
    }

    public ObservableCollection<Statblock> Statblocks { get; set; } = new();
    public ICommand ScrollChangedCommand { get; set; }

    public void AddToken(TokenListItem tokenListItem)
    {
        if (tokenListItem.Token.Statblock != null)
        {
            var existingStatblock = Statblocks.SingleOrDefault(s => s.Name == tokenListItem.Token.Name);
            if (existingStatblock == null)
            {
                Statblocks.Add(tokenListItem.Token.Statblock);
            }
        }
    }

    public void RemoveToken(TokenListItem tokenListItem)
    {
        if (tokenListItem.Token.Statblock != null)
        {
            var existingStatblock = Statblocks.SingleOrDefault(s => s.Name == tokenListItem.Token.Name);
            if (existingStatblock != null)
            {
                existingStatblock.Dispose();
                Statblocks.Remove(existingStatblock);
            }
        }
    }

    public void Clear()
    {
        foreach (var statblock in Statblocks)
        {
            statblock.Dispose();
        }
        Statblocks.Clear();
    }

    private void ScrollChanged(ScrollChangedEventArgs eventArgs)
    {
        var horizontalOffset = (int)eventArgs.HorizontalOffset;
        var viewportWidth = (int)eventArgs.ViewportWidth;

        for (int i = 0; i < Statblocks.Count; i++)
        {
            if (i < horizontalOffset || i > (horizontalOffset + viewportWidth - 1))
            {
                if (Statblocks[i].RenderVisibility != Visibility.Hidden)
                {
                    Statblocks[i].RenderVisibility = Visibility.Hidden;
                }
            }
            else
            {
                if (Statblocks[i].RenderVisibility != Visibility.Visible)
                {
                    Statblocks[i].RenderVisibility = Visibility.Visible;
                }
            }
        }
    }
}
