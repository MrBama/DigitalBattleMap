using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.DataClasses;

public class HexTileStore
{
    private static string TilesPath => Constants.HexTilesPath;
    private static string IndexPath => Path.Combine(TilesPath, "index.json");

    private readonly Dictionary<string, HexTile> _tiles = new();

    public IReadOnlyList<HexTile> GetAll() => _tiles.Values.ToList();

    public HexTile GetById(string id) => id != null && _tiles.TryGetValue(id, out var t) ? t : null;

    public void Initialize()
    {
        AddPresets();
        LoadCustomTiles();
    }

    public void AddCustomTile(HexTile tile, BitmapSource bitmap)
    {
        Directory.CreateDirectory(TilesPath);
        tile.Bitmap = bitmap;
        _tiles[tile.Id] = tile;

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(TilesPath, $"{tile.Id}.png"));
        encoder.Save(stream);

        SaveIndex();
    }

    public void RemoveCustomTile(string id)
    {
        if (!_tiles.TryGetValue(id, out var tile) || tile.IsPreset) return;
        _tiles.Remove(id);
        var path = Path.Combine(TilesPath, $"{id}.png");
        if (File.Exists(path)) File.Delete(path);
        SaveIndex();
    }

    private void AddPresets()
    {
        var presets = new (string id, string name, Color color)[]
        {
            ("preset_plains",     "Plains",      Color.FromRgb(190, 210, 120)),
            ("preset_forest",     "Forest",      Color.FromRgb( 50, 120,  50)),
            ("preset_mountain",   "Mountain",    Color.FromRgb(140, 130, 120)),
            ("preset_water",      "Water",       Color.FromRgb( 80, 140, 200)),
            ("preset_hills",      "Hills",       Color.FromRgb(160, 175,  90)),
            ("preset_desert",     "Desert",      Color.FromRgb(220, 190,  90)),
            ("preset_swamp",      "Swamp",       Color.FromRgb( 80, 110,  70)),
            ("preset_settlement", "Settlement",  Color.FromRgb(180, 130,  80)),
        };

        foreach (var (id, name, color) in presets)
            _tiles[id] = new HexTile { Id = id, Name = name, IsPreset = true, PresetColor = color };
    }

    private void LoadCustomTiles()
    {
        if (!File.Exists(IndexPath)) return;

        try
        {
            var json = File.ReadAllText(IndexPath);
            var entries = JsonConvert.DeserializeObject<List<TileEntry>>(json) ?? new();
            foreach (var entry in entries)
            {
                var imgPath = Path.Combine(TilesPath, $"{entry.Id}.png");
                if (!File.Exists(imgPath)) continue;

                var bitmap = new BitmapImage(new Uri(imgPath));
                _tiles[entry.Id] = new HexTile { Id = entry.Id, Name = entry.Name, IsPreset = false, Bitmap = bitmap };
            }
        }
        catch { }
    }

    private void SaveIndex()
    {
        var entries = _tiles.Values
            .Where(t => !t.IsPreset)
            .Select(t => new TileEntry { Id = t.Id, Name = t.Name })
            .ToList();
        File.WriteAllText(IndexPath, JsonConvert.SerializeObject(entries, Formatting.Indented));
    }

    private class TileEntry
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
