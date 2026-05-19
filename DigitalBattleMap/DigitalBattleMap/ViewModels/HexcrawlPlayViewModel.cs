using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class HexcrawlPlayViewModel : ViewModelBase
{
    private readonly IWindowService _windowService;

    public HexcrawlPlayViewModel(IWindowService windowService)
    {
        _windowService = windowService;
    }

    public HexcrawlPlayViewModel() { }

    public HexcrawlMapViewModel Map { get; private set; }

    // When true, left-click on canvas places the party token instead of exploring.
    public bool IsTokenMode { get => Get<bool>(); set => Set(value); }

    // When true, Explore/Cover and token placement also affect the 6 neighboring hexes.
    public bool IncludeNeighbors { get => Get<bool>(); set => Set(value); }

    public ICommand ExploreCommand      { get; private set; }
    public ICommand CoverCommand        { get; private set; }
    public ICommand ResetFogCommand     { get; private set; }
    public ICommand PlaceTokenCommand   { get; private set; }
    public ICommand LoadTokenImageCommand { get; private set; }
    public ICommand ClearTokenCommand   { get; private set; }

    public void SetMap(HexcrawlMapViewModel map)
    {
        Map = map;
        NotifyPropertyChange(nameof(Map));
    }

    protected override void InitializeCommands()
    {
        IncludeNeighbors = true;

        ExploreCommand        = new RelayCommand(p => ExecuteOnCell(p, explore: true));
        CoverCommand          = new RelayCommand(p => ExecuteOnCell(p, explore: false));
        ResetFogCommand       = new RelayCommand(_ => Map?.ResetFog());
        PlaceTokenCommand     = new RelayCommand(p => PlaceToken(p));
        LoadTokenImageCommand = new RelayCommand(_ => LoadTokenImage());
        ClearTokenCommand     = new RelayCommand(_ => Map?.ClearPartyToken(), _ => Map?.PartyQ != null);
    }

    private void ExecuteOnCell(object param, bool explore)
    {
        if (Map == null || param is not (int q, int r)) return;
        if (explore) Map.Explore(q, r, IncludeNeighbors);
        else         Map.Cover(q, r, IncludeNeighbors);
    }

    private void PlaceToken(object param)
    {
        if (Map == null || param is not (int q, int r)) return;
        Map.PlacePartyToken(q, r);
        Map.Explore(q, r, IncludeNeighbors);
    }

    private void LoadTokenImage()
    {
        if (_windowService == null) return;
        if (_windowService.ShowOpenFileDialog(out var path, "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"))
            Map?.LoadPartyTokenImage(path);
    }
}
