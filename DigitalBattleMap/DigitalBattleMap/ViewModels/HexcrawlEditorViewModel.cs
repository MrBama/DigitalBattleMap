using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class HexcrawlEditorViewModel : ViewModelBase
{
    private readonly HexTileStore _tileStore;
    private readonly IWindowService _windowService;

    public HexcrawlEditorViewModel(IWindowService windowService)
    {
        _windowService = windowService;
        _tileStore = new HexTileStore();
        _tileStore.Initialize();
        Map = new HexcrawlMapViewModel(_tileStore);
        ReloadTileList();
    }

    public HexcrawlEditorViewModel()
    {
        _tileStore = new HexTileStore();
        Map = new HexcrawlMapViewModel(_tileStore);
    }

    protected override void InitializeCommands()
    {
        AddTileCommand    = new RelayCommand(_ => AddTile());
        RemoveTileCommand = new RelayCommand(_ => RemoveTile(), _ => SelectedTile?.IsPreset == false);
        UndoCommand       = new RelayCommand(_ => Map?.Undo(), _ => Map?.PaintHistory.IsPreviousCommandAvailable == true);
        RedoCommand       = new RelayCommand(_ => Map?.Redo(), _ => Map?.PaintHistory.IsNextCommandAvailable == true);
    }

    public HexcrawlMapViewModel Map { get; private set; }

    public ObservableCollection<HexcrawlTileViewModel> TileList { get; } = new();

    public HexcrawlTileViewModel SelectedTile
    {
        get => Get<HexcrawlTileViewModel>();
        set
        {
            Set(value);
            if (Map != null)
                Map.ActiveTileId = value?.Id;
        }
    }

    public ICommand AddTileCommand    { get; private set; }
    public ICommand RemoveTileCommand { get; private set; }
    public ICommand UndoCommand       { get; private set; }
    public ICommand RedoCommand       { get; private set; }

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
        if (SelectedTile == null || SelectedTile.IsPreset) return;
        _tileStore.RemoveCustomTile(SelectedTile.Id);
        TileList.Remove(SelectedTile);
        SelectedTile = TileList.FirstOrDefault();
    }
}
