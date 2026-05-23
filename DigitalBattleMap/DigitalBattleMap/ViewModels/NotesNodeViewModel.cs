using DigitalBattleMap.Utilities;
using System.Collections.ObjectModel;

namespace DigitalBattleMap.ViewModels;

public class NotesNodeViewModel : PropertyHandler
{
    public string Name { get => Get<string>(); set => Set(value); }
    public string FullPath { get => Get<string>(); set => Set(value); }
    public bool IsFolder { get => Get<bool>(); set => Set(value); }
    public bool IsExpanded { get => Get<bool>(); set => Set(value); }
    public ObservableCollection<NotesNodeViewModel> Children { get; } = new();
}
