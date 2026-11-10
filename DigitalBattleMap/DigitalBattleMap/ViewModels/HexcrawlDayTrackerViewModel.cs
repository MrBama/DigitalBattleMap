using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.UIElements;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

// One line of the per-phase reminder checklist. Checks are not saved; they reset on phase change.
public class HexcrawlChecklistItem : PropertyHandler
{
    public HexcrawlChecklistItem(string text) { Text = text; }
    public string Text { get; }
    public bool IsChecked { get => Get<bool>(); set => Set(value); }
}

// DM tracker for the hexcrawl play tab: day/phase/hours and info about the party's hex.
// Reminders only: computes values and shows checklists, never rolls dice.
public class HexcrawlDayTrackerViewModel : ViewModelBase
{
    // Fixed rules (terrain-dependent rules live on the tiles).
    public const double ExploringDayHours = 8;
    public const double SearchHours = 1;
    public const int NavigationDc = 12;
    public const int NavigationDcSameTerrain = 15;
    public const int ForcedMarchBaseDc = 10;
    // Named hexes (a place name is set) are the searchable ones and always take this long to enter.
    public const double NamedHexHours = 1;

    private readonly IWindowService _windowService;
    private HexcrawlMapViewModel _map;

    public HexcrawlDayTrackerViewModel(IWindowService windowService)
    {
        _windowService = windowService;
    }

    public void SetMap(HexcrawlMapViewModel map)
    {
        if (_map != null)
        {
            _map.RenderRequested -= RefreshPartyHex;
            _map.HexInfoChanged  -= RefreshPartyHex;
            _map.PropertyChanged -= OnMapPropertyChanged;
        }
        _map = map;
        if (_map != null)
        {
            _map.RenderRequested += RefreshPartyHex;
            _map.HexInfoChanged  += RefreshPartyHex;
            _map.PropertyChanged += OnMapPropertyChanged;
        }
        RebuildChecklist();
        RefreshDay();
        RefreshPartyHex();
    }

    private HexcrawlDayState State => _map?.DayState;

    // A hex with a place name is searchable; only there the search check happens.
    public bool IsSearchable(int q, int r) => !string.IsNullOrWhiteSpace(_map?.GetPlaceName(q, r));

    // Hours spent entering a hex: 1 for named hexes, otherwise the terrain's hours.
    public double GetEnterHours(int q, int r) =>
        IsSearchable(q, r) ? NamedHexHours : _map?.GetTileAt(q, r)?.TravelHours ?? HexTile.DefaultTravelHours;

    // --- Day ---

    public ObservableCollection<HexcrawlChecklistItem> Checklist { get; } = new();

    public string DayTitle => State == null ? string.Empty : $"Day {State.Day} — {PhaseName(State.Phase)}";

    public bool IsExploring => State?.Phase == HexcrawlDayPhase.Exploring;

    public string HoursText => State == null ? string.Empty
        : $"Exploring hours used: {Format(State.ExploringHoursUsed)} / {Format(ExploringDayHours)}";

    public bool IsForcedMarch => State != null && State.ExploringHoursUsed > ExploringDayHours;

    public string ForcedMarchText => IsForcedMarch
        ? $"Forced march — each hex entered or searched: Con save DC {ForcedMarchDc}, failure = 1 level of exhaustion"
        : string.Empty;

    // DC 10 + 1 for each (started) hour past the normal exploring day.
    private int ForcedMarchDc => ForcedMarchBaseDc + (int)Math.Ceiling(State.ExploringHoursUsed - ExploringDayHours);

    public ICommand NextPhaseCommand  { get; private set; }
    public ICommand NewDayCommand     { get; private set; }
    public ICommand PreviousDayCommand { get; private set; }
    public ICommand AddHourCommand    { get; private set; }
    public ICommand RemoveHourCommand { get; private set; }
    public ICommand SearchCommand     { get; private set; }

    protected override void InitializeCommands()
    {
        NextPhaseCommand  = new RelayCommand(_ => NextPhase(), _ => State != null);
        NewDayCommand     = new RelayCommand(_ => NewDay(), _ => State != null);
        PreviousDayCommand = new RelayCommand(_ => PreviousDay(), _ => State?.History?.Count > 0);
        AddHourCommand    = new RelayCommand(_ => AddHours(1), _ => State != null);
        RemoveHourCommand = new RelayCommand(_ => AddHours(-1), _ => State != null && State.ExploringHoursUsed > 0);
        SearchCommand     = new RelayCommand(_ => Search(), _ => GetPartyHex() is (int q, int r) && IsSearchable(q, r));
    }

    // Called when the party moves from one hex into another (normal token or Party lost marker):
    // records the route and spends the hours to enter the new hex.
    public void OnPartyEnteredHex(int fromQ, int fromR, int q, int r)
    {
        if (State == null) return;
        State.Path ??= new();
        if (State.Path.Count == 0)
            State.Path.Add(new HexCoord { Q = fromQ, R = fromR });
        State.Path.Add(new HexCoord { Q = q, R = r });
        if (State.Phase == HexcrawlDayPhase.Morning)
        {
            // Moving means the exploring phase has started.
            State.Phase = HexcrawlDayPhase.Exploring;
            RebuildChecklist();
        }
        AddHours(GetEnterHours(q, r));
        // The route changed, so the trailblazed neighbours may have too.
        RefreshPartyHex();
    }

    // Hexes the party has been in: every completed day's route plus today's.
    // Travelling back into one of these is along a known trail (trailblazed).
    private HashSet<(int, int)> GetVisitedHexes()
    {
        var visited = new HashSet<(int, int)>();
        if (State == null) return visited;
        foreach (var day in State.History ?? new())
            foreach (var c in day.Path ?? new())
                visited.Add((c.Q, c.R));
        foreach (var c in State.Path ?? new())
            visited.Add((c.Q, c.R));
        return visited;
    }

    private void AddHours(double hours)
    {
        if (State == null) return;
        State.ExploringHoursUsed = Math.Max(0, State.ExploringHoursUsed + hours);
        if (State.ExploringHoursUsed > ExploringDayHours)
            State.ForcedMarchToday = true;
        RefreshDay();
        CommandManager.InvalidateRequerySuggested();
    }

    private void Search()
    {
        var hex = GetPartyHex();
        if (hex == null) return;
        var (q, r) = hex.Value;
        if (!IsSearchable(q, r)) return;
        _map.UpdateHexInfo(q, r, i => i.Searched = true);
        AddHours(SearchHours);
    }

    private void NextPhase()
    {
        if (State == null) return;
        if (State.Phase == HexcrawlDayPhase.Rest) { NewDay(); return; }
        State.Phase = State.Phase + 1;
        RebuildChecklist();
        RefreshDay();
    }

    // Stores the finished day in the history and starts the next one in the party's current hex.
    private void NewDay()
    {
        if (State == null) return;
        State.History ??= new();
        State.History.Add(new HexcrawlDayRecord
        {
            Day = State.Day,
            Path = State.Path?.ToList() ?? new(),
            ExploringHoursUsed = State.ExploringHoursUsed,
            ForcedMarch = State.ForcedMarchToday
        });
        State.Day++;
        State.Phase = HexcrawlDayPhase.Morning;
        State.ExploringHoursUsed = 0;
        State.ForcedMarchToday = false;
        State.Path = new();
        if (GetPartyHex() is (int q, int r))
            State.Path.Add(new HexCoord { Q = q, R = r });
        RebuildChecklist();
        RefreshDay();
        CommandManager.InvalidateRequerySuggested();
    }

    // Undoes "New day": restores the last completed day (at its Rest phase).
    // The party token is not moved; only the tracker state returns.
    private void PreviousDay()
    {
        if (State?.History == null || State.History.Count == 0) return;
        var record = State.History[^1];
        bool hasProgress = State.ExploringHoursUsed > 0 || (State.Path?.Count ?? 0) > 1 || State.Phase != HexcrawlDayPhase.Morning;
        if (hasProgress && _windowService != null &&
            !_windowService.ShowConfirmDialog(
                $"Go back to day {record.Day}?\n\nWhat was tracked for day {State.Day} (route and hours) will be lost.",
                "Previous day"))
            return;

        State.History.RemoveAt(State.History.Count - 1);
        State.Day = record.Day;
        State.Phase = HexcrawlDayPhase.Rest;
        State.ExploringHoursUsed = record.ExploringHoursUsed;
        State.ForcedMarchToday = record.ForcedMarch;
        State.Path = record.Path?.ToList() ?? new();
        RebuildChecklist();
        RefreshDay();
        CommandManager.InvalidateRequerySuggested();
    }

    // --- Route / history ---

    public string TodayRouteText => State?.Path == null || State.Path.Count < 2
        ? "Route today: —"
        : "Route today: " + FormatRoute(State.Path);

    // Completed days, newest first.
    public ObservableCollection<string> HistoryLines { get; } = new();

    public bool HasHistory => HistoryLines.Count > 0;

    private void RefreshHistory()
    {
        HistoryLines.Clear();
        if (State?.History != null)
            foreach (var d in Enumerable.Reverse(State.History))
            {
                var route = d.Path == null || d.Path.Count < 2 ? "stayed put" : FormatRoute(d.Path);
                var march = d.ForcedMarch ? ", forced march" : string.Empty;
                HistoryLines.Add($"Day {d.Day} ({Format(d.ExploringHoursUsed)} h{march}): {route}");
            }
        NotifyPropertyChange(nameof(HasHistory));
    }

    private string FormatRoute(System.Collections.Generic.IEnumerable<HexCoord> path) =>
        string.Join(" → ", path.Select(c =>
        {
            var label = HexcrawlMapViewModel.GetHexLabel(c.Q, c.R).ToString("D4");
            var name = _map?.GetPlaceName(c.Q, c.R);
            return string.IsNullOrWhiteSpace(name) ? label : $"{label} {name}";
        }));

    private void RefreshDay()
    {
        NotifyPropertyChange(nameof(DayTitle));
        NotifyPropertyChange(nameof(IsExploring));
        NotifyPropertyChange(nameof(HoursText));
        NotifyPropertyChange(nameof(IsForcedMarch));
        NotifyPropertyChange(nameof(ForcedMarchText));
        NotifyPropertyChange(nameof(TodayRouteText));
        RefreshHistory();
        // New day / Previous day change the routes the trailblazed hexes come from.
        if (GetPartyHex() is (int q, int r))
            TrailblazedText = GetTrailblazedText(q, r);
    }

    private void RebuildChecklist()
    {
        Checklist.Clear();
        if (State == null) return;
        foreach (var line in GetChecklistLines(State))
            Checklist.Add(new HexcrawlChecklistItem(line));
    }

    private string[] GetChecklistLines(HexcrawlDayState state)
    {
        switch (state.Phase)
        {
            case HexcrawlDayPhase.Morning:
                var hex = GetPartyHex();
                var tile = hex != null ? _map.GetTileAt(hex.Value.q, hex.Value.r) : null;
                var encounterOn = tile?.EncounterOn ?? HexTile.DefaultEncounterOn;
                var where = tile != null ? $" (day starts in {tile.Name})" : string.Empty;
                return new[]
                {
                    $"GM: roll d6 — encounter on {encounterOn}–6{where}",
                    "If triggered: Encounter 2d6, Who spots who d4, When d6, Attitude d4 (1 = roleplay)",
                    "Players: lead navigator and marching order",
                    "Players: the day's goal or route",
                };
            case HexcrawlDayPhase.Exploring:
                return new[]
                {
                    "Players name the hex they want to enter",
                    $"Navigation check if the terrain needs it: DC {NavigationDc}, DC {NavigationDcSameTerrain} if all adjacent hexes match, advantage with a landmark in sight",
                    "Failure: secret d10 — 1–4 left, 5–6 intended, 7–10 right (turn on Party lost)",
                    "Describe terrain, adjacent terrain, sights and sounds, special hex",
                    $"Search? Only named hexes ({Format(SearchHours)} h) — special hex reveals its site, otherwise the random table",
                    "Track spill exposure where a site says so",
                    $"Past {Format(ExploringDayHours)} h: Con save for every hex entered or searched",
                };
            case HexcrawlDayPhase.Camping:
                var unused = Math.Max(0, ExploringDayHours - state.ExploringHoursUsed);
                return new[]
                {
                    "Each character eats one ration",
                    $"Downtime: foraging or activities ({Format(unused)} unused exploring hours can be added)",
                    "GM resolves the checks",
                };
            case HexcrawlDayPhase.Rest:
                return new[]
                {
                    "Rest site must be defensible: barricaded room, keyed site or guarded hex (not open tunnel)",
                    "Watch order",
                    "Encounter happens now if its When was during rest",
                    state.ForcedMarchToday
                        ? "Forced march today — no recovery rest"
                        : "Recovery rest: remove one level of forced-march exhaustion; two rations each today",
                };
            default:
                return Array.Empty<string>();
        }
    }

    // --- Party hex card ---

    public bool HasPartyHex => GetPartyHex() != null;
    public bool ShowsLostPosition => _map != null && _map.IsPartyLost && _map.LostQ.HasValue && _map.LostR.HasValue;
    public string PartyHexTitle { get => Get<string>(); private set => Set(value); }
    public string TerrainText   { get => Get<string>(); private set => Set(value); }
    public string NavigationText { get => Get<string>(); private set => Set(value); }
    public string TrailblazedText { get => Get<string>(); private set => Set(value); }
    public string AdjacentText  { get => Get<string>(); private set => Set(value); }
    public string SpecialText   { get => Get<string>(); private set => Set(value); }
    public string NotesText     { get => Get<string>(); private set => Set(value); }
    public bool HasNotes => !string.IsNullOrWhiteSpace(NotesText);

    // The party's hex has a name and the players know where they are (not lost):
    // only then can its name be revealed or hidden from here.
    public bool CanRevealName =>
        _map != null && !_map.IsPartyLost && GetPartyHex() is (int q, int r) && IsSearchable(q, r);

    // Whether the players see the name of the party's hex.
    public bool IsPartyHexNameRevealed
    {
        get => GetPartyHex() is (int q, int r) && _map.IsLabelRevealed(q, r);
        set
        {
            if (!CanRevealName || GetPartyHex() is not (int q, int r)) return;
            if (value) _map.RevealLabel(q, r, includeNeighbors: false);
            else _map.HideLabel(q, r);
        }
    }

    // The party's actual hex: the lost marker while lost, otherwise the token.
    private (int q, int r)? GetPartyHex()
    {
        if (_map == null) return null;
        if (_map.IsPartyLost && _map.LostQ.HasValue && _map.LostR.HasValue)
            return (_map.LostQ.Value, _map.LostR.Value);
        if (_map.PartyQ.HasValue && _map.PartyR.HasValue)
            return (_map.PartyQ.Value, _map.PartyR.Value);
        return null;
    }

    private void RefreshPartyHex()
    {
        var hex = GetPartyHex();
        if (hex == null)
        {
            PartyHexTitle = "Party token not placed";
            TerrainText = NavigationText = TrailblazedText = AdjacentText = SpecialText = NotesText = string.Empty;
        }
        else
        {
            var (q, r) = hex.Value;
            var tile = _map.GetTileAt(q, r);
            var info = _map.GetHexInfo(q, r);
            var name = _map.GetPlaceName(q, r);

            PartyHexTitle = HexcrawlMapViewModel.GetHexLabel(q, r).ToString("D4")
                + (string.IsNullOrEmpty(name) ? string.Empty : $" — {name}");
            bool searchable = IsSearchable(q, r);
            TerrainText = (tile != null ? tile.Name : "No terrain painted")
                + $" · {Format(GetEnterHours(q, r))} h to enter"
                + (searchable ? " (named hex)" : string.Empty);
            NavigationText = GetNavigationText(q, r, tile);
            TrailblazedText = GetTrailblazedText(q, r);
            AdjacentText = GetAdjacentText(q, r);
            SpecialText = (info?.Special switch
            {
                HexSpecial.PointOfInterest => "Point of interest",
                HexSpecial.Dungeon => "Dungeon",
                _ => "Normal hex",
            }) + (!searchable ? " · not searchable"
                 : info?.Searched == true ? " · searched" : " · searchable, not searched");
            NotesText = info?.Notes ?? string.Empty;
        }
        NotifyPropertyChange(nameof(HasPartyHex));
        NotifyPropertyChange(nameof(ShowsLostPosition));
        NotifyPropertyChange(nameof(HasNotes));
        NotifyPropertyChange(nameof(CanRevealName));
        NotifyPropertyChange(nameof(IsPartyHexNameRevealed));
    }

    // Adjacent hexes the party has already visited: moving there is automatic navigation success.
    private string GetTrailblazedText(int q, int r)
    {
        var visited = GetVisitedHexes();
        var trails = HexGeometry.GetNeighbors(q, r, _map.GridWidth, _map.GridHeight)
            .Where(n => visited.Contains((n.q, n.r)))
            .Select(n =>
            {
                var label = HexcrawlMapViewModel.GetHexLabel(n.q, n.r).ToString("D4");
                var name = _map.GetPlaceName(n.q, n.r);
                return string.IsNullOrWhiteSpace(name) ? label : $"{label} {name}";
            })
            .ToList();
        return trails.Count == 0
            ? "Trailblazed: no adjacent hex visited yet"
            : "Trailblazed (auto success): " + string.Join(", ", trails);
    }

    private string GetNavigationText(int q, int r, HexTile tile)
    {
        if (tile == null) return "Navigation: no terrain painted";
        if (!tile.RequiresNavigation) return "Navigation: no check needed";

        var neighborTiles = HexGeometry.GetNeighbors(q, r, _map.GridWidth, _map.GridHeight)
            .Select(n => _map.GetCell(n.q, n.r)?.TileId)
            .ToList();
        bool allSame = neighborTiles.Count > 0 && neighborTiles.All(id => id != null && id == neighborTiles[0]);
        if (allSame)
        {
            var sameName = _map.GetTile(neighborTiles[0])?.Name ?? "the same terrain";
            return $"Navigation: Survival DC {NavigationDcSameTerrain} (all adjacent hexes are {sameName})";
        }
        return $"Navigation: Survival DC {NavigationDc}";
    }

    private string GetAdjacentText(int q, int r)
    {
        // Named hexes are listed by name (always 1 h); other neighbours are grouped by terrain with their hours.
        var neighbors = HexGeometry.GetNeighbors(q, r, _map.GridWidth, _map.GridHeight).ToList();
        var named = neighbors
            .Where(n => IsSearchable(n.q, n.r))
            .Select(n => $"{_map.GetPlaceName(n.q, n.r)} ({Format(NamedHexHours)} h)");
        var terrain = neighbors
            .Where(n => !IsSearchable(n.q, n.r))
            .Select(n => _map.GetTileAt(n.q, n.r))
            .GroupBy(t => t?.Id)
            .OrderByDescending(g => g.Count())
            .Select(g =>
            {
                var t = g.First();
                var name = t?.Name ?? "unpainted";
                var hours = t != null ? $" ({Format(t.TravelHours)} h)" : string.Empty;
                return (g.Count() > 1 ? $"{name} ×{g.Count()}" : name) + hours;
            });
        var parts = terrain.Concat(named).ToList();
        return parts.Count == 0 ? string.Empty : "Adjacent: " + string.Join(", ", parts);
    }

    private void OnMapPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HexcrawlMapViewModel.DayState))
        {
            RebuildChecklist();
            RefreshDay();
        }
        else if (e.PropertyName == nameof(HexcrawlMapViewModel.IsPartyLost))
        {
            RefreshPartyHex();
        }
    }

    private static string PhaseName(HexcrawlDayPhase phase) => phase switch
    {
        HexcrawlDayPhase.Morning   => "Morning",
        HexcrawlDayPhase.Exploring => "Exploring (8 h)",
        HexcrawlDayPhase.Camping   => "Camping (4 h)",
        HexcrawlDayPhase.Rest      => "Rest (8 h)",
        _ => phase.ToString(),
    };

    private static string Format(double hours) => hours.ToString("0.#");
}
