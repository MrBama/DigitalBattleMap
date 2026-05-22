using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace DigitalBattleMap.ViewModels;

public class HexcrawlEditorViewModel : ViewModelBase
{
    private readonly HexTileStore _tileStore;
    private readonly HexEdgeTypeStore _edgeStore;
    private readonly IWindowService _windowService;

    public HexcrawlEditorViewModel(IWindowService windowService)
    {
        _windowService = windowService;
        _tileStore = new HexTileStore();
        _tileStore.Initialize();
        _edgeStore = new HexEdgeTypeStore();
        _edgeStore.Initialize();
        Map = new HexcrawlMapViewModel(_tileStore, _edgeStore);
        ReloadTileList();
        ReloadEdgeTypeList();
    }

    public HexcrawlEditorViewModel()
    {
        _tileStore = new HexTileStore();
        _edgeStore = new HexEdgeTypeStore();
        Map = new HexcrawlMapViewModel(_tileStore, _edgeStore);
    }

    protected override void InitializeCommands()
    {
        AddTileCommand          = new RelayCommand(_ => AddTile());
        RemoveTileCommand       = new RelayCommand(_ => RemoveTile(), _ => SelectedTile != null);
        RenameTileCommand       = new RelayCommand(_ => RenameTile(), _ => SelectedTile != null && !string.IsNullOrWhiteSpace(EditingName));
        AddEdgeTypeCommand      = new RelayCommand(_ => AddEdgeType());
        RemoveEdgeTypeCommand   = new RelayCommand(_ => RemoveEdgeType(), _ => SelectedEdgeType != null);
        RenameEdgeTypeCommand   = new RelayCommand(_ => RenameEdgeType(), _ => SelectedEdgeType != null && !string.IsNullOrWhiteSpace(EditingEdgeName));
        ApplyEdgeColorCommand   = new RelayCommand(_ => ApplyEdgeColor(), _ => SelectedEdgeType != null && !string.IsNullOrWhiteSpace(EditingEdgeColor));
        UndoCommand             = new RelayCommand(_ => Map?.Undo(), _ => Map?.PaintHistory.IsPreviousCommandAvailable == true);
        RedoCommand             = new RelayCommand(_ => Map?.Redo(), _ => Map?.PaintHistory.IsNextCommandAvailable == true);
    }

    public HexcrawlMapViewModel Map { get; private set; }

    // --- Tiles ---
    public ObservableCollection<HexcrawlTileViewModel> TileList { get; } = new();

    public HexcrawlTileViewModel SelectedTile
    {
        get => Get<HexcrawlTileViewModel>();
        set
        {
            Set(value);
            if (Map != null)
            {
                Map.ActiveTileId = value?.Id;
                if (value != null) Map.ActiveEdgeTypeId = null;
            }
            EditingName = value?.Name ?? string.Empty;
            // Deselect edge type so re-clicking it always re-enters edge mode
            if (value != null) Set<HexcrawlEdgeTypeViewModel>(null, nameof(SelectedEdgeType));
        }
    }

    public string EditingName { get => Get<string>(); set => Set(value); }

    public ICommand AddTileCommand    { get; private set; }
    public ICommand RemoveTileCommand { get; private set; }
    public ICommand RenameTileCommand { get; private set; }
    public ICommand UndoCommand       { get; private set; }
    public ICommand RedoCommand       { get; private set; }

    // --- Edge types ---
    public ObservableCollection<HexcrawlEdgeTypeViewModel> EdgeTypeList { get; } = new();

    public HexcrawlEdgeTypeViewModel SelectedEdgeType
    {
        get => Get<HexcrawlEdgeTypeViewModel>();
        set
        {
            Set(value);
            if (Map != null)
            {
                Map.ActiveEdgeTypeId = value?.Id;
                if (value != null) Map.ActiveTileId = null;
            }
            EditingEdgeName  = value?.Name ?? string.Empty;
            NotifyPropertyChange(nameof(EditingEdgeCurved));
            // Deselect tile so re-clicking it always re-enters tile mode
            if (value != null) Set<HexcrawlTileViewModel>(null, nameof(SelectedTile));
        }
    }

    public string EditingEdgeName  { get => Get<string>(); set => Set(value); }
    public string EditingEdgeColor { get => Get<string>(); set => Set(value); }

    public bool EditingEdgeCurved
    {
        get => SelectedEdgeType != null && (_edgeStore.GetById(SelectedEdgeType.Id)?.IsCurved ?? false);
        set
        {
            if (SelectedEdgeType == null) return;
            _edgeStore.UpdateCurved(SelectedEdgeType.Id, value);
            NotifyPropertyChange(nameof(EditingEdgeCurved));
            Map?.RaiseRenderRequested();
        }
    }

    public Brush SelectedEdgePreviewBrush =>
        SelectedEdgeType?.PreviewBrush ?? Brushes.Transparent;

    public ICommand AddEdgeTypeCommand    { get; private set; }
    public ICommand RemoveEdgeTypeCommand { get; private set; }
    public ICommand RenameEdgeTypeCommand { get; private set; }
    public ICommand ApplyEdgeColorCommand { get; private set; }

    public void LoadFromData(HexcrawlGridData data)
    {
        Map?.LoadFromData(data);
    }

    public HexcrawlGridData GetGridData() => Map?.GetGridData() ?? new HexcrawlGridData();

    private void ReloadTileList()
    {
        TileList.Clear();
        foreach (var tile in _tileStore.GetAll())
            TileList.Add(new HexcrawlTileViewModel(tile));
        SelectedTile = TileList.FirstOrDefault();
    }

    private void AddTile()
    {
        if (!_windowService.ShowOpenFileDialog(out var path,
            "Image files|*.png;*.jpg;*.jpeg;*.bmp"))
            return;

        var cutoutVm = new HexCutoutWindowViewModel(path);
        _windowService.ShowWindowDialog<HexCutoutWindow>(cutoutVm);

        if (cutoutVm.Result == null) return;

        var tile = new HexTile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = cutoutVm.TileName,
            IsPreset = false
        };
        _tileStore.AddCustomTile(tile, cutoutVm.Result);

        var tileVm = new HexcrawlTileViewModel(tile);
        TileList.Add(tileVm);
        SelectedTile = tileVm;
    }

    private void RemoveTile()
    {
        if (SelectedTile == null) return;
        _tileStore.RemoveTile(SelectedTile.Id);
        TileList.Remove(SelectedTile);
        SelectedTile = TileList.FirstOrDefault();
    }

    private void RenameTile()
    {
        if (SelectedTile == null || string.IsNullOrWhiteSpace(EditingName)) return;
        var selectedId = SelectedTile.Id;
        _tileStore.RenameTile(selectedId, EditingName.Trim());
        ReloadTileList();
        SelectedTile = TileList.FirstOrDefault(t => t.Id == selectedId) ?? TileList.FirstOrDefault();
    }

    private void ReloadEdgeTypeList()
    {
        var selectedId = SelectedEdgeType?.Id;
        EdgeTypeList.Clear();
        foreach (var t in _edgeStore.GetAll())
            EdgeTypeList.Add(new HexcrawlEdgeTypeViewModel(t));
        SelectedEdgeType = EdgeTypeList.FirstOrDefault(t => t.Id == selectedId)
                        ?? EdgeTypeList.FirstOrDefault();
    }

    private void AddEdgeType()
    {
        var type = new HexEdgeType
        {
            Id    = Guid.NewGuid().ToString("N"),
            Name  = "New edge",
            Color = "#0070C0"
        };
        _edgeStore.Add(type);
        var vm = new HexcrawlEdgeTypeViewModel(type);
        EdgeTypeList.Add(vm);
        SelectedEdgeType = vm;
        EditingEdgeName  = vm.Name;
        EditingEdgeColor = type.Color;
    }

    private void RemoveEdgeType()
    {
        if (SelectedEdgeType == null) return;
        _edgeStore.Remove(SelectedEdgeType.Id);
        EdgeTypeList.Remove(SelectedEdgeType);
        SelectedEdgeType = EdgeTypeList.FirstOrDefault();
    }

    private void RenameEdgeType()
    {
        if (SelectedEdgeType == null || string.IsNullOrWhiteSpace(EditingEdgeName)) return;
        var selectedId = SelectedEdgeType.Id;
        _edgeStore.Rename(selectedId, EditingEdgeName.Trim());
        ReloadEdgeTypeList();
        SelectedEdgeType = EdgeTypeList.FirstOrDefault(t => t.Id == selectedId)
                        ?? EdgeTypeList.FirstOrDefault();
    }

    private void ApplyEdgeColor()
    {
        if (SelectedEdgeType == null || string.IsNullOrWhiteSpace(EditingEdgeColor)) return;
        var color = EditingEdgeColor.Trim();
        if (!color.StartsWith("#")) color = "#" + color;
        _edgeStore.UpdateColor(SelectedEdgeType.Id, color);
        if (Map != null) Map.InvalidateEdgePenCache();
        ReloadEdgeTypeList();
        Map?.RaiseRenderRequested();
    }
}
