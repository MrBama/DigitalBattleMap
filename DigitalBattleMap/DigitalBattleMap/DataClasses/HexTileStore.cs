using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    private readonly HashSet<string> _removedPresets = new();
    private readonly Dictionary<string, string> _presetNameOverrides = new();

    public IReadOnlyList<HexTile> GetAll() => _tiles.Values.ToList();

    public HexTile GetById(string id) => id != null && _tiles.TryGetValue(id, out var t) ? t : null;

    public void Initialize()
    {
        LoadCustomTiles(); // must load removedPresets / overrides before AddPresets
        AddPresets();
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

    public void RemoveTile(string id)
    {
        if (!_tiles.TryGetValue(id, out var tile)) return;
        _tiles.Remove(id);

        if (tile.IsPreset)
        {
            _removedPresets.Add(id);
            _presetNameOverrides.Remove(id);
        }
        else
        {
            var path = Path.Combine(TilesPath, $"{id}.png");
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        SaveIndex();
    }

    public void RenameTile(string id, string newName)
    {
        if (!_tiles.TryGetValue(id, out var tile)) return;
        tile.Name = newName;
        if (tile.IsPreset)
            _presetNameOverrides[id] = newName;
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
        {
            if (_removedPresets.Contains(id)) continue;
            var displayName = _presetNameOverrides.TryGetValue(id, out var n) ? n : name;
            _tiles[id] = new HexTile { Id = id, Name = displayName, IsPreset = true, PresetColor = color };
        }
    }

    private void LoadCustomTiles()
    {
        if (!File.Exists(IndexPath)) return;

        try
        {
            var json = File.ReadAllText(IndexPath);
            var token = JToken.Parse(json);

            List<TileEntry> tileEntries;
            if (token is JObject)
            {
                var index = token.ToObject<TileIndex>() ?? new TileIndex();
                foreach (var id in index.RemovedPresets ?? new())
                    _removedPresets.Add(id);
                foreach (var kv in index.PresetNameOverrides ?? new())
                    _presetNameOverrides[kv.Key] = kv.Value;
                tileEntries = index.Tiles;
            }
            else
            {
                // backward-compat: old format was a bare array
                tileEntries = token.ToObject<List<TileEntry>>() ?? new();
            }

            foreach (var entry in tileEntries ?? new())
            {
                var imgPath = Path.Combine(TilesPath, $"{entry.Id}.png");
                if (!File.Exists(imgPath)) continue;
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imgPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                _tiles[entry.Id] = new HexTile { Id = entry.Id, Name = entry.Name, IsPreset = false, Bitmap = bitmap };
            }
        }
        catch { }
    }

    private void SaveIndex()
    {
        try
        {
            Directory.CreateDirectory(TilesPath);
            var index = new TileIndex
            {
                RemovedPresets = _removedPresets.ToList(),
                PresetNameOverrides = new Dictionary<string, string>(_presetNameOverrides),
                Tiles = _tiles.Values
                    .Where(t => !t.IsPreset)
                    .Select(t => new TileEntry { Id = t.Id, Name = t.Name })
                    .ToList()
            };
            File.WriteAllText(IndexPath, JsonConvert.SerializeObject(index, Formatting.Indented));
        }
        catch { }
    }

    private class TileIndex
    {
        public List<string> RemovedPresets { get; set; } = new();
        public Dictionary<string, string> PresetNameOverrides { get; set; } = new();
        public List<TileEntry> Tiles { get; set; } = new();
    }

    private class TileEntry
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
