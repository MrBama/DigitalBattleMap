using DigitalBattleMap.DataClasses;
using DigitalBattleMap.UIElements;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace DigitalBattleMap.ViewModels;

public class HexcrawlMapViewModel : ViewModelBase
{
    private readonly HexTileStore _tileStore;
    private readonly Dictionary<(int, int), HexCell> _cells = new();
    private readonly Dictionary<(int, int), string> _gestureOriginals = new();
    private readonly HashSet<(int, int)> _paintedThisGesture = new();
    private bool _isPainting;
    private bool _needsInitialCenter = true;

    public HexcrawlMapViewModel(HexTileStore tileStore)
    {
        _tileStore = tileStore;
        PaintHistory = new CommandHistory<HexPaintCommand>(50);
    }

    public HexcrawlMapViewModel()
    {
        _tileStore = new HexTileStore();
        PaintHistory = new CommandHistory<HexPaintCommand>(50);
    }

    protected override void InitializeCommands()
    {
        Scale = 1.0;
        GridWidth = 30;
        GridHeight = 20;
    }

    public CommandHistory<HexPaintCommand> PaintHistory { get; }
    public double HexSize { get; set; } = 80.0;
    public string ActiveTileId { get; set; }
    public Size CanvasSize { get; private set; }

    public double Scale   { get => Get<double>(); private set => Set(value); }
    public double OffsetX { get => Get<double>(); private set => Set(value); }
    public double OffsetY { get => Get<double>(); private set => Set(value); }

    public int GridWidth
    {
        get => Get<int>();
        set { Set(value); CenterGrid(); RenderRequested?.Invoke(); }
    }

    public int GridHeight
    {
        get => Get<int>();
        set { Set(value); CenterGrid(); RenderRequested?.Invoke(); }
    }

    public event Action RenderRequested;

    public void SetCanvasSize(double w, double h)
    {
        CanvasSize = new Size(w, h);
        if (_needsInitialCenter && w > 0 && h > 0)
        {
            CenterGrid();
            _needsInitialCenter = false;
        }
    }

    private void CenterGrid()
    {
        if (CanvasSize.Width <= 0 || CanvasSize.Height <= 0) return;

        // Visual centre of the rectangular (even-q offset) grid.
        // x: columns span q=0..W-1 with width size*1.5*(W-1)
        // y: even columns top at 0, odd at Sqrt3/2*size; bottom is Sqrt3*H*size (odd cols)
        //    → y-extent from -Sqrt3/2*size to Sqrt3*H*size
        //    → y-centre = Sqrt3*(H-0.5)/2 * size
        double gridCX = HexSize * 1.5 * (GridWidth  - 1) / 2.0;
        double gridCY = HexSize * HexGeometry.Sqrt3 * (GridHeight - 0.5) / 2.0;

        OffsetX = CanvasSize.Width  / 2.0 - gridCX * Scale;
        OffsetY = CanvasSize.Height / 2.0 - gridCY * Scale;
    }

    public HexCell GetCell(int q, int r) =>
        _cells.TryGetValue((q, r), out var c) ? c : null;

    public HexTile GetTile(string id) =>
        _tileStore.GetById(id);

    public void BeginPaint()
    {
        _isPainting = true;
        _gestureOriginals.Clear();
        _paintedThisGesture.Clear();
    }

    public void PaintCell(int q, int r)
    {
        if (!_isPainting) return;
        if (q < 0 || q >= GridWidth) return;
        int rOff = q / 2;
        if (r < -rOff || r >= GridHeight - rOff) return;

        var key = (q, r);
        if (_paintedThisGesture.Contains(key)) return;

        if (!_gestureOriginals.ContainsKey(key))
            _gestureOriginals[key] = _cells.TryGetValue(key, out var ex) ? ex.TileId : null;

        if (ActiveTileId == null)
            _cells.Remove(key);
        else
            _cells[key] = new HexCell(q, r, ActiveTileId);

        _paintedThisGesture.Add(key);
        RenderRequested?.Invoke();
    }

    public void EndPaint()
    {
        if (!_isPainting) return;
        _isPainting = false;

        if (_paintedThisGesture.Count > 0)
        {
            var cmd = new HexPaintCommand();
            foreach (var key in _paintedThisGesture)
            {
                var (q, r) = key;
                _gestureOriginals.TryGetValue(key, out var oldId);
                _cells.TryGetValue(key, out var cell);
                cmd.Changes.Add(new HexPaintCommand.HexCellChange(q, r, oldId, cell?.TileId));
            }
            PaintHistory.Enqueue(cmd);
        }
    }

    public void Undo()
    {
        if (!PaintHistory.TryDequeuePreviousCommand(out var cmd)) return;
        foreach (var c in cmd.Changes)
        {
            if (c.OldTileId == null) _cells.Remove((c.Q, c.R));
            else _cells[(c.Q, c.R)] = new HexCell(c.Q, c.R, c.OldTileId);
        }
        RenderRequested?.Invoke();
    }

    public void Redo()
    {
        if (!PaintHistory.TryDequeueNextCommand(out var cmd)) return;
        foreach (var c in cmd.Changes)
        {
            if (c.NewTileId == null) _cells.Remove((c.Q, c.R));
            else _cells[(c.Q, c.R)] = new HexCell(c.Q, c.R, c.NewTileId);
        }
        RenderRequested?.Invoke();
    }

    public void ZoomIn()
    {
        ApplyZoom(1.1);
    }

    public void ZoomOut()
    {
        ApplyZoom(1.0 / 1.1);
    }

    private void ApplyZoom(double factor)
    {
        double cx = CanvasSize.Width / 2;
        double cy = CanvasSize.Height / 2;
        Scale *= factor;
        OffsetX = cx + (OffsetX - cx) * factor;
        OffsetY = cy + (OffsetY - cy) * factor;
        RenderRequested?.Invoke();
    }

    public void Pan(ArrowDirection direction, int steps)
    {
        double step = HexSize * Scale * 0.5 * steps;
        switch (direction)
        {
            case ArrowDirection.Up:    OffsetY += step; break;
            case ArrowDirection.Down:  OffsetY -= step; break;
            case ArrowDirection.Left:  OffsetX += step; break;
            case ArrowDirection.Right: OffsetX -= step; break;
        }
        RenderRequested?.Invoke();
    }

    public void LoadFromData(HexcrawlGridData data)
    {
        _cells.Clear();
        HexSize = data.HexSize;
        _needsInitialCenter = true;
        // Set without triggering CenterGrid until canvas size is known
        Set(data.GridWidth,  nameof(GridWidth));
        Set(data.GridHeight, nameof(GridHeight));
        foreach (var cell in data.Cells)
            _cells[(cell.Q, cell.R)] = cell;
        PaintHistory.Clear();
        RenderRequested?.Invoke();
    }

    public HexcrawlGridData GetGridData()
    {
        return new HexcrawlGridData
        {
            HexSize    = HexSize,
            GridWidth  = GridWidth,
            GridHeight = GridHeight,
            Cells      = _cells.Values.ToList()
        };
    }
}
