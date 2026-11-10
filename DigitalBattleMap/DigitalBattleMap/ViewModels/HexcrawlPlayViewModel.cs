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

    // When true, any explore action also reveals the place name of those hexes to players.
    public bool RevealNamesOnExplore { get => Get<bool>(); set => Set(value); }

    // When true, left-click+drag on the canvas draws a ping circle instead of exploring.
    public bool IsPingMode { get => Get<bool>(); set => Set(value); }

    public ICommand ExploreCommand      { get; private set; }
    public ICommand CoverCommand        { get; private set; }
    public ICommand ResetFogCommand     { get; private set; }
    public ICommand PlaceTokenCommand   { get; private set; }
    public ICommand LoadTokenImageCommand { get; private set; }
    public ICommand ClearTokenCommand   { get; private set; }
    public ICommand ClearTokenImageCommand { get; private set; }

    // When true, the party is lost: token placement only moves the DM-only actual position,
    // the player token stays frozen and nothing new is revealed. Turning it off moves the
    // token to the actual position and explores it.
    public bool IsPartyLost
    {
        get => Map?.IsPartyLost ?? false;
        set
        {
            if (Map == null || value == Map.IsPartyLost) return;
            if (value)
            {
                Map.BeginPartyLost();
            }
            else
            {
                var pos = Map.EndPartyLost();
                if (pos is (int q, int r))
                    PlaceToken((q, r));
            }
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public void SetMap(HexcrawlMapViewModel map)
    {
        if (Map != null) Map.PropertyChanged -= OnMapPropertyChanged;
        Map = map;
        if (Map != null) Map.PropertyChanged += OnMapPropertyChanged;
        NotifyPropertyChange(nameof(Map));
        NotifyPropertyChange(nameof(IsPartyLost));
    }

    // Keeps the toggle in sync when the lost state changes on the map (e.g. a map is loaded).
    private void OnMapPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HexcrawlMapViewModel.IsPartyLost))
            NotifyPropertyChange(nameof(IsPartyLost));
    }

    protected override void InitializeCommands()
    {
        IncludeNeighbors = true;

        ExploreCommand        = new RelayCommand(p => ExecuteOnCell(p, explore: true));
        CoverCommand          = new RelayCommand(p => ExecuteOnCell(p, explore: false));
        ResetFogCommand       = new RelayCommand(_ => Map?.ResetFog());
        PlaceTokenCommand     = new RelayCommand(p => PlaceToken(p));
        LoadTokenImageCommand = new RelayCommand(_ => LoadTokenImage());
        ClearTokenCommand     = new RelayCommand(_ => { Map?.ClearPartyToken(); CommandManager.InvalidateRequerySuggested(); }, _ => Map?.PartyQ != null);
        ClearTokenImageCommand = new RelayCommand(_ => { Map?.ClearPartyTokenImage(); CommandManager.InvalidateRequerySuggested(); }, _ => Map?.PartyTokenImage != null);
    }

    private void ExecuteOnCell(object param, bool explore)
    {
        if (Map == null || param is not (int q, int r)) return;
        if (explore)
        {
            Map.Explore(q, r, IncludeNeighbors);
            if (RevealNamesOnExplore)
                Map.RevealLabel(q, r, IncludeNeighbors);
        }
        else
        {
            Map.Cover(q, r, IncludeNeighbors);
        }
    }

    private void PlaceToken(object param)
    {
        if (Map == null || param is not (int q, int r)) return;
        if (Map.IsPartyLost)
        {
            // Track the actual position for the DM only; players see nothing change.
            Map.MoveLostParty(q, r);
            return;
        }
        Map.PlacePartyToken(q, r);
        Map.Explore(q, r, IncludeNeighbors);
        if (RevealNamesOnExplore)
            Map.RevealLabel(q, r, IncludeNeighbors);
        CommandManager.InvalidateRequerySuggested();
    }

    private void LoadTokenImage()
    {
        if (_windowService == null) return;
        if (_windowService.ShowOpenFileDialog(out var path, "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"))
            Map?.ImportPartyTokenImage(path);
    }
}
