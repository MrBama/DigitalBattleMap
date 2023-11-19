using DigitalBattleMap.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class ListSelectionWindowViewModel<T> : ViewModelBase
{
    public ListSelectionWindowViewModel()
    {
    }

    public ListSelectionWindowViewModel(IList<T> list)
    {
        List = new ObservableCollection<T>(list);
    }

    protected override void InitializeCommands()
    {
        SelectCommand = new RelayCommand(p => SelectButton());
    }

    public bool Success { get; set; }
    public T SelectedItem { get => Get<T>(); set => Set(value); }
    public ObservableCollection<T> List { get; set; } = new();
    public ICommand SelectCommand { get; set; }


    private void SelectButton()
    {
        Success = true;
    }
}

// This is required because a xaml cannot initiate a generic type class
public class ListSelectionWindowViewModelString : ListSelectionWindowViewModel<string>
{
    public ListSelectionWindowViewModelString()
    {
    }
}
