using DigitalBattleMap.UIElements;
using DigitalBattleMap.Utilities;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class HexcrawlPlayViewModel : ViewModelBase
{
    public HexcrawlMapViewModel Map { get; private set; }

    // When true, Explore/Cover also affect the 6 neighboring hexes.
    public bool IncludeNeighbors { get => Get<bool>(); set => Set(value); }

    public ICommand ExploreCommand  { get; private set; }
    public ICommand CoverCommand    { get; private set; }
    public ICommand ResetFogCommand { get; private set; }

    public void SetMap(HexcrawlMapViewModel map)
    {
        Map = map;
        NotifyPropertyChange(nameof(Map));
    }

    protected override void InitializeCommands()
    {
        IncludeNeighbors = true;
        ExploreCommand  = new RelayCommand(p => ExecuteOnCell(p, explore: true));
        CoverCommand    = new RelayCommand(p => ExecuteOnCell(p, explore: false));
        ResetFogCommand = new RelayCommand(_ => Map?.ResetFog());
    }

    // Called from the canvas code-behind with the (q, r) of the clicked hex.
    private void ExecuteOnCell(object param, bool explore)
    {
        if (Map == null || param is not (int q, int r)) return;
        if (explore)
            Map.Explore(q, r, IncludeNeighbors);
        else
            Map.Cover(q, r, IncludeNeighbors);
    }
}
