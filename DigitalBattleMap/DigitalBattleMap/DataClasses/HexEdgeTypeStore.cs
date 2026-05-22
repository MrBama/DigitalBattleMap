using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DigitalBattleMap.DataClasses;

public class HexEdgeTypeStore
{
    private static string IndexPath => Path.Combine(Constants.HexEdgeTypesPath, "index.json");

    private readonly Dictionary<string, HexEdgeType> _types = new();

    public IReadOnlyList<HexEdgeType> GetAll() => _types.Values.ToList();

    public HexEdgeType GetById(string id) =>
        id != null && _types.TryGetValue(id, out var t) ? t : null;

    public void Initialize()
    {
        if (!File.Exists(IndexPath)) return;
        try
        {
            var list = JsonConvert.DeserializeObject<List<HexEdgeType>>(File.ReadAllText(IndexPath)) ?? new();
            foreach (var t in list)
                _types[t.Id] = t;
        }
        catch { }
    }

    public void Add(HexEdgeType type)
    {
        _types[type.Id] = type;
        Save();
    }

    public void Remove(string id)
    {
        _types.Remove(id);
        Save();
    }

    public void Rename(string id, string newName)
    {
        if (!_types.TryGetValue(id, out var t)) return;
        t.Name = newName;
        Save();
    }

    public void UpdateColor(string id, string color)
    {
        if (!_types.TryGetValue(id, out var t)) return;
        t.Color = color;
        Save();
    }

    public void UpdateCurved(string id, bool isCurved)
    {
        if (!_types.TryGetValue(id, out var t)) return;
        t.IsCurved = isCurved;
        Save();
    }

    public void UpdateEndTiles(string id, List<string> endTileIds)
    {
        if (!_types.TryGetValue(id, out var t)) return;
        t.EndTileIds = endTileIds ?? new();
        Save();
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Constants.HexEdgeTypesPath);
            File.WriteAllText(IndexPath,
                JsonConvert.SerializeObject(_types.Values.ToList(), Formatting.Indented));
        }
        catch { }
    }
}
