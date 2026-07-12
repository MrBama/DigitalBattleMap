using DigitalBattleMap.DataClasses;
using DigitalBattleMap.UIElements;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DigitalBattleMap.ViewModels;

public class HexcrawlMapViewModel : ViewModelBase
{
    private readonly HexTileStore _tileStore;
    private readonly HexEdgeTypeStore _edgeStore;
    private readonly Dictionary<(int, int), HexCell> _cells = new();
    private readonly Dictionary<(int, int), string> _gestureOriginals = new();
    private readonly HashSet<(int, int)> _paintedThisGesture = new();
    private readonly HashSet<(int, int)> _exploredHexes = new();
    private bool _isPainting;

    // Canonical edge storage: key = (q, r, dir) from CanonicalizeEdge, value = typeId
    private readonly Dictionary<(int, int, int), string> _edges = new();
    // Edge paint gesture state
    private bool _isEdgePainting;
    private readonly Dictionary<(int, int, int), string> _edgeGestureOriginals = new();
    private readonly HashSet<(int, int, int)> _edgePaintedThisGesture = new();
    private bool _needsInitialCenter = true;
    private int? _partyQ;
    private int? _partyR;
    private string _partyTokenImagePath;

    // --- Ping ---
    private double _pingCenterX;
    private double _pingCenterY;
    private double _pingRadiusWorld;
    private bool _pingVisible;
    private DispatcherTimer _pingTimer;

    // --- Edge pen cache (keyed by typeId, invalidated on hexSize change) ---
    private readonly Dictionary<string, Pen> _edgePenCache = new();
    private double _edgePenCacheHexSize = -1;

    // --- Place names ---
    private readonly Dictionary<(int, int), string> _cellLabels = new();
    private readonly HashSet<(int, int)> _revealedLabels = new();
    private readonly Dictionary<string, Geometry> _placeNameCache = new();
    private double _placeNameCacheHexSize = -1;
    private int? _selectedQ;
    private int? _selectedR;

    // --- Zoom revert ---
    private double? _prevScale;
    private double? _prevOffsetX;
    private double? _prevOffsetY;

    // --- Label cache ---
    private readonly Dictionary<int, Geometry> _labelCache = new();
    private bool _labelCacheDirty = true;
    private double _labelCacheHexSize = -1;

    private static readonly Brush LabelFillBrush = MakeFrozenBrush(Color.FromArgb(210, 255, 255, 255));
    private static readonly Pen   LabelStrokePen  = MakeFrozenPen(Color.FromArgb(150, 0, 0, 0), 1.5);
    private static readonly Typeface LabelTypeface = new(
        new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

    // Place name rendering: revealed (player-visible) vs hidden (DM-only hint).
    private static readonly Brush PlaceNameRevealedFill   = MakeFrozenBrush(Color.FromArgb(220, 255, 255, 255));
    private static readonly Pen   PlaceNameRevealedStroke  = MakeFrozenPen(Color.FromArgb(160, 0, 0, 0), 1.5);
    private static readonly Brush PlaceNameHiddenFill     = MakeFrozenBrush(Color.FromArgb(130, 210, 210, 210));
    private static readonly Pen   PlaceNameHiddenStroke   = MakeFrozenPen(Color.FromArgb(100, 120, 120, 120), 1.0);

    private static Brush MakeFrozenBrush(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
    private static Pen   MakeFrozenPen(Color c, double t)
    {
        var brush = new SolidColorBrush(c); brush.Freeze();
        var pen   = new Pen(brush, t);      pen.Freeze();
        return pen;
    }

    public HexcrawlMapViewModel(HexTileStore tileStore, HexEdgeTypeStore edgeStore)
    {
        _tileStore = tileStore;
        _edgeStore = edgeStore;
        PaintHistory = new CommandHistory<IHexMapCommand>(50);
    }

    public HexcrawlMapViewModel(HexTileStore tileStore)
    {
        _tileStore = tileStore;
        _edgeStore = new HexEdgeTypeStore();
        PaintHistory = new CommandHistory<IHexMapCommand>(50);
    }

    public HexcrawlMapViewModel()
    {
        _tileStore = new HexTileStore();
        _edgeStore = new HexEdgeTypeStore();
        PaintHistory = new CommandHistory<IHexMapCommand>(50);
    }

    protected override void InitializeCommands()
    {
        Scale = 1.0;
        GridWidth = 30;
        GridHeight = 20;
        SelectCellCommand = new RelayCommand(SelectCell);
    }

    public CommandHistory<IHexMapCommand> PaintHistory { get; }
    public double HexSize { get; set; } = 80.0;
    public string ActiveTileId { get; set; }
    public string ActiveEdgeTypeId { get; set; }
    public bool IsEdgePaintMode => ActiveEdgeTypeId != null;
    public Size CanvasSize { get; private set; }

    public int? PartyQ => _partyQ;
    public int? PartyR => _partyR;

    // Bound to the play view token image preview.
    public BitmapSource PartyTokenImage { get => Get<BitmapSource>(); private set => Set(value); }

    // Right-click a hex in the editor to select it for labeling; parameter is ValueTuple<int,int> (q, r).
    public ICommand SelectCellCommand { get; private set; }

    // Label of the currently right-clicked hex. Setting it writes to _cellLabels immediately.
    public string SelectedCellLabel
    {
        get
        {
            if (_selectedQ == null || _selectedR == null) return string.Empty;
            _cellLabels.TryGetValue((_selectedQ.Value, _selectedR.Value), out var l);
            return l ?? string.Empty;
        }
        set
        {
            if (_selectedQ == null || _selectedR == null) return;
            var key = (_selectedQ.Value, _selectedR.Value);
            var label = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (label == null) _cellLabels.Remove(key);
            else _cellLabels[key] = label;
            NotifyPropertyChange(nameof(SelectedCellLabel));
            RenderRequested?.Invoke();
        }
    }

    // Exposed so HexMapCanvas can highlight the selected hex without a DP binding.
    public int? SelectedQ => _selectedQ;
    public int? SelectedR => _selectedR;

    // Display string for the editor panel, e.g. "0304". Empty dash when nothing is selected.
    public string SelectedHexInfo =>
        _selectedQ.HasValue && _selectedR.HasValue
            ? GetHexLabel(_selectedQ.Value, _selectedR.Value).ToString("D4")
            : "—";

    public double Scale   { get => Get<double>(); private set => Set(value); }
    public double OffsetX { get => Get<double>(); private set => Set(value); }
    public double OffsetY { get => Get<double>(); private set => Set(value); }

    public int GridWidth
    {
        get => Get<int>();
        set { Set(value); _labelCacheDirty = true; CenterGrid(); RenderRequested?.Invoke(); }
    }

    public int GridHeight
    {
        get => Get<int>();
        set { Set(value); _labelCacheDirty = true; CenterGrid(); RenderRequested?.Invoke(); }
    }

    public event Action RenderRequested;
    public event Action RectZoomModeChanged;
    public event Action CanRevertZoomChanged;

    public bool IsRectZoomMode
    {
        get => Get<bool>();
        set => Set(value, () => RectZoomModeChanged?.Invoke());
    }

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
        if (cmd is HexPaintCommand tc)
            foreach (var c in tc.Changes)
            {
                if (c.OldTileId == null) _cells.Remove((c.Q, c.R));
                else _cells[(c.Q, c.R)] = new HexCell(c.Q, c.R, c.OldTileId);
            }
        else if (cmd is HexEdgePaintCommand ec)
            foreach (var c in ec.Changes)
                ApplyEdgeValue(c.Q, c.R, c.EdgeDir, c.OldTypeId);
        RenderRequested?.Invoke();
    }

    public void Redo()
    {
        if (!PaintHistory.TryDequeueNextCommand(out var cmd)) return;
        if (cmd is HexPaintCommand tc)
            foreach (var c in tc.Changes)
            {
                if (c.NewTileId == null) _cells.Remove((c.Q, c.R));
                else _cells[(c.Q, c.R)] = new HexCell(c.Q, c.R, c.NewTileId);
            }
        else if (cmd is HexEdgePaintCommand ec)
            foreach (var c in ec.Changes)
                ApplyEdgeValue(c.Q, c.R, c.EdgeDir, c.NewTypeId);
        RenderRequested?.Invoke();
    }

    private void ApplyEdgeValue(int q, int r, int dir, string typeId)
    {
        var key = HexGeometry.CanonicalizeEdge(q, r, dir);
        if (typeId == null) _edges.Remove(key);
        else _edges[key] = typeId;
    }

    // --- Edge painting ---

    public void BeginEdgePaint()
    {
        _isEdgePainting = true;
        _edgeGestureOriginals.Clear();
        _edgePaintedThisGesture.Clear();
    }

    // Paints (or erases when ActiveEdgeTypeId==null) the edge between (q,r) and its dir-neighbour.
    public void PaintEdge(int q, int r, int dir)
    {
        if (!_isEdgePainting) return;
        var key = HexGeometry.CanonicalizeEdge(q, r, dir);

        if (_edgePaintedThisGesture.Contains(key)) return;
        if (!_edgeGestureOriginals.ContainsKey(key))
            _edgeGestureOriginals[key] = _edges.TryGetValue(key, out var ex) ? ex : null;

        if (ActiveEdgeTypeId == null) _edges.Remove(key);
        else _edges[key] = ActiveEdgeTypeId;

        _edgePaintedThisGesture.Add(key);
        RenderRequested?.Invoke();
    }

    public void EndEdgePaint()
    {
        if (!_isEdgePainting) return;
        _isEdgePainting = false;

        if (_edgePaintedThisGesture.Count == 0) return;

        var cmd = new HexEdgePaintCommand();
        foreach (var key in _edgePaintedThisGesture)
        {
            _edgeGestureOriginals.TryGetValue(key, out var oldId);
            _edges.TryGetValue(key, out var newId);
            cmd.Changes.Add(new HexEdgePaintCommand.HexEdgeChange(key.Item1, key.Item2, key.Item3, oldId, newId));
        }
        PaintHistory.Enqueue(cmd);
    }

    // Immediately removes a single edge and creates a one-shot undo entry.
    public void RemoveEdgeAt(int q, int r, int dir)
    {
        var key = HexGeometry.CanonicalizeEdge(q, r, dir);
        if (!_edges.TryGetValue(key, out var oldId)) return;

        _edges.Remove(key);

        var cmd = new HexEdgePaintCommand();
        cmd.Changes.Add(new HexEdgePaintCommand.HexEdgeChange(key.Item1, key.Item2, key.Item3, oldId, null));
        PaintHistory.Enqueue(cmd);
        RenderRequested?.Invoke();
    }

    // Returns all edge directions and their typeId for a given hex (up to 6).
    public System.Collections.Generic.List<(int dir, string typeId)> GetEdgesForHex(int q, int r)
    {
        var result = new System.Collections.Generic.List<(int, string)>();
        for (int dir = 0; dir < 6; dir++)
        {
            var key = HexGeometry.CanonicalizeEdge(q, r, dir);
            if (_edges.TryGetValue(key, out var typeId))
                result.Add((dir, typeId));
        }
        return result;
    }

    public HexEdgeType GetEdgeType(string id) => _edgeStore.GetById(id);

    public void InvalidateEdgePenCache() => _edgePenCache.Clear();

    public void RaiseRenderRequested() => RenderRequested?.Invoke();

    public void ZoomIn()  => ApplyZoom(1.25);
    public void ZoomOut() => ApplyZoom(1.0 / 1.25);

    // Zooms and centers so that the given world rectangle fills the canvas.
    public void ZoomToRect(double worldLeft, double worldTop, double worldRight, double worldBottom)
    {
        double worldW = worldRight  - worldLeft;
        double worldH = worldBottom - worldTop;
        if (worldW < 1 || worldH < 1) return;

        _prevScale   = Scale;
        _prevOffsetX = OffsetX;
        _prevOffsetY = OffsetY;

        Scale   = Math.Min(CanvasSize.Width / worldW, CanvasSize.Height / worldH);
        OffsetX = CanvasSize.Width  / 2.0 - (worldLeft + worldRight)  / 2.0 * Scale;
        OffsetY = CanvasSize.Height / 2.0 - (worldTop  + worldBottom) / 2.0 * Scale;
        CanRevertZoomChanged?.Invoke();
        RenderRequested?.Invoke();
    }

    public bool CanRevertZoom => _prevScale.HasValue;

    public void RevertZoom()
    {
        if (!_prevScale.HasValue) return;
        Scale   = _prevScale.Value;
        OffsetX = _prevOffsetX.Value;
        OffsetY = _prevOffsetY.Value;
        _prevScale = _prevOffsetX = _prevOffsetY = null;
        CanRevertZoomChanged?.Invoke();
        RenderRequested?.Invoke();
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
        double colStep = HexSize * Scale * 1.5       * steps; // one full column pitch
        double rowStep = HexSize * Scale * HexGeometry.Sqrt3 * steps; // one full row pitch
        switch (direction)
        {
            case ArrowDirection.Up:    OffsetY += rowStep; break;
            case ArrowDirection.Down:  OffsetY -= rowStep; break;
            case ArrowDirection.Left:  OffsetX += colStep; break;
            case ArrowDirection.Right: OffsetX -= colStep; break;
        }
        RenderRequested?.Invoke();
    }

    public void LoadFromData(HexcrawlGridData data)
    {
        _cells.Clear();
        _exploredHexes.Clear();
        _cellLabels.Clear();
        _revealedLabels.Clear();
        _edges.Clear();
        HexSize = data.HexSize;
        _needsInitialCenter = true;
        // Set without triggering CenterGrid until canvas size is known
        Set(data.GridWidth,  nameof(GridWidth));
        Set(data.GridHeight, nameof(GridHeight));
        foreach (var cell in data.Cells)
            _cells[(cell.Q, cell.R)] = cell;
        foreach (var coord in data.ExploredHexes)
            _exploredHexes.Add((coord.Q, coord.R));
        foreach (var entry in data.HexLabels ?? new List<HexLabelEntry>())
            if (!string.IsNullOrEmpty(entry.Label))
                _cellLabels[(entry.Q, entry.R)] = entry.Label;
        foreach (var coord in data.RevealedLabels ?? new List<HexCoord>())
            _revealedLabels.Add((coord.Q, coord.R));

        if (data.PartyTokenPos != null)
        {
            _partyQ = data.PartyTokenPos.Q;
            _partyR = data.PartyTokenPos.R;
        }
        else
        {
            _partyQ = null;
            _partyR = null;
        }
        LoadPartyTokenImage(data.PartyTokenImagePath);

        foreach (var edge in data.Edges ?? new System.Collections.Generic.List<HexEdge>())
        {
            var key = HexGeometry.CanonicalizeEdge(edge.Q, edge.R, edge.EdgeDir);
            _edges[key] = edge.TypeId;
        }

        PaintHistory.Clear();
        RenderRequested?.Invoke();
    }

    public HexcrawlGridData GetGridData()
    {
        return new HexcrawlGridData
        {
            HexSize            = HexSize,
            GridWidth          = GridWidth,
            GridHeight         = GridHeight,
            Cells              = _cells.Values.ToList(),
            ExploredHexes      = _exploredHexes.Select(e => new HexCoord { Q = e.Item1, R = e.Item2 }).ToList(),
            PartyTokenPos      = _partyQ.HasValue && _partyR.HasValue ? new HexCoord { Q = _partyQ.Value, R = _partyR.Value } : null,
            PartyTokenImagePath = _partyTokenImagePath,
            HexLabels      = _cellLabels
                .Select(kv => new HexLabelEntry { Q = kv.Key.Item1, R = kv.Key.Item2, Label = kv.Value })
                .ToList(),
            RevealedLabels = _revealedLabels
                .Select(e => new HexCoord { Q = e.Item1, R = e.Item2 })
                .ToList(),
            Edges = _edges
                .Select(kv => new HexEdge { Q = kv.Key.Item1, R = kv.Key.Item2, EdgeDir = kv.Key.Item3, TypeId = kv.Value })
                .ToList()
        };
    }

    // --- Fog of exploration ---

    public bool IsExplored(int q, int r) => _exploredHexes.Contains((q, r));

    // Marks the hex and all in-bounds neighbors as explored.
    public void Explore(int q, int r, bool includeNeighbors)
    {
        _exploredHexes.Add((q, r));
        if (includeNeighbors)
            foreach (var n in HexGeometry.GetNeighbors(q, r, GridWidth, GridHeight))
                _exploredHexes.Add(n);
        RenderRequested?.Invoke();
    }

    // Removes exploration from the hex and all in-bounds neighbors.
    public void Cover(int q, int r, bool includeNeighbors)
    {
        _exploredHexes.Remove((q, r));
        if (includeNeighbors)
            foreach (var n in HexGeometry.GetNeighbors(q, r, GridWidth, GridHeight))
                _exploredHexes.Remove(n);
        RenderRequested?.Invoke();
    }

    // Covers the entire map (full fog).
    public void ResetFog()
    {
        _exploredHexes.Clear();
        RenderRequested?.Invoke();
    }

    // --- Party token ---

    public void PlacePartyToken(int q, int r)
    {
        _partyQ = q;
        _partyR = r;
        RenderRequested?.Invoke();
    }

    public void ClearPartyToken()
    {
        _partyQ = null;
        _partyR = null;
        RenderRequested?.Invoke();
    }

    // Copies an external image into the tiles folder and loads it.
    public void ImportPartyTokenImage(string sourcePath)
    {
        var logicalName = _tileStore.ImportPartyTokenImage(sourcePath);
        if (logicalName != null)
            LoadPartyTokenImage(logicalName);
    }

    // Loads from a saved path or logical filename. Pass null to clear the image.
    public void LoadPartyTokenImage(string pathOrLogicalName)
    {
        _partyTokenImagePath = pathOrLogicalName;
        if (string.IsNullOrEmpty(pathOrLogicalName)) { PartyTokenImage = null; return; }

        var fullPath = _tileStore.ResolvePartyTokenPath(pathOrLogicalName);
        if (fullPath == null) { PartyTokenImage = null; return; }

        try
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(fullPath);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            img.Freeze();
            PartyTokenImage = img;
            RenderRequested?.Invoke();
        }
        catch
        {
            PartyTokenImage = null;
        }
    }

    // Renders the current hex map view to a System.Drawing.Bitmap for sending to players.
    // Falls back to 1280x720 if the canvas size is not yet known (hex tab never opened).
    // includeFog=true renders unexplored hexes with fully opaque fog (player view).
    public System.Drawing.Bitmap RenderToBitmap(bool includeFog = false)
    {
        int width  = CanvasSize.Width  > 0 ? (int)CanvasSize.Width  : 1280;
        int height = CanvasSize.Height > 0 ? (int)CanvasSize.Height : 720;

        var visual = BuildDrawingVisual(width, height, includeFog);
        var rtb    = RenderToTarget(visual, width, height);
        return ConvertToBitmap(rtb);
    }

    // Draws the full hex grid into a DrawingVisual using the current pan/zoom state.
    private DrawingVisual BuildDrawingVisual(int width, int height, bool includeFog = false)
    {
        double scale   = Scale;
        double ox      = OffsetX;
        double oy      = OffsetY;
        double hexSize = HexSize;

        // Convert screen bounds to world coordinates so only visible columns/rows are drawn.
        double worldLeft   = (0      - ox) / scale;
        double worldRight  = (width  - ox) / scale;
        double worldTop    = (0      - oy) / scale;
        double worldBottom = (height - oy) / scale;

        var (qMin, qMax) = GetVisibleColumnRange(worldLeft, worldRight, hexSize);

        var outlinePen = new Pen(new SolidColorBrush(Color.FromRgb(100, 100, 100)), 1.0 / scale);
        outlinePen.Freeze();

        var visual = new DrawingVisual();
        using (var ctx = visual.RenderOpen())
        {
            ctx.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            ctx.PushClip(new RectangleGeometry(new Rect(0, 0, width, height)));
            ctx.PushTransform(new TranslateTransform(ox, oy));
            ctx.PushTransform(new ScaleTransform(scale, scale));

            for (int q = qMin; q <= qMax; q++)
                DrawColumn(ctx, q, hexSize, worldTop, worldBottom, outlinePen);

            // Second pass: edges on top of all tile fills
            for (int q = qMin; q <= qMax; q++)
                DrawColumnEdges(ctx, q, hexSize, worldTop, worldBottom, respectFog: includeFog);

            if (includeFog)
            {
                // Fully opaque fog for the player: unexplored hexes are completely hidden.
                var fogBrush = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40));
                fogBrush.Freeze();
                double rScale = hexSize * HexGeometry.Sqrt3;
                for (int q = qMin; q <= qMax; q++)
                {
                    int rOff = q / 2;
                    double halfQ = q / 2.0;
                    int rMin = Math.Max((int)Math.Floor(worldTop    / rScale - halfQ) - 1, -rOff);
                    int rMax = Math.Min((int)Math.Ceiling(worldBottom / rScale - halfQ) + 1, GridHeight - 1 - rOff);
                    for (int r = rMin; r <= rMax; r++)
                    {
                        if (IsExplored(q, r)) continue;
                        var (wx, wy) = HexGeometry.AxialToPixel(q, r, hexSize);
                        ctx.DrawGeometry(fogBrush, null, HexGeometry.CreateStreamGeometry(wx, wy, hexSize));
                    }
                }
            }

            DrawPartyToken(ctx, hexSize, scale);
            DrawPing(ctx, scale);

            // Draw revealed place names on top of fog (player bitmap shows only revealed+explored).
            {
                double rScalePn = hexSize * HexGeometry.Sqrt3;
                for (int q = qMin; q <= qMax; q++)
                {
                    int rOff = q / 2;
                    double halfQ = q / 2.0;
                    int rMin = Math.Max((int)Math.Floor(worldTop    / rScalePn - halfQ) - 1, -rOff);
                    int rMax = Math.Min((int)Math.Ceiling(worldBottom / rScalePn - halfQ) + 1, GridHeight - 1 - rOff);
                    for (int r = rMin; r <= rMax; r++)
                    {
                        if (!IsLabelRevealed(q, r)) continue;
                        if (includeFog && !IsExplored(q, r)) continue;
                        DrawPlaceName(ctx, q, r, hexSize, revealed: true);
                    }
                }
            }

            ctx.Pop(); // ScaleTransform
            ctx.Pop(); // TranslateTransform
            ctx.Pop(); // clip
        }
        return visual;
    }

    // Draws the party token circle (with custom image or gold fallback) at its current position.
    private void DrawPartyToken(DrawingContext ctx, double hexSize, double scale)
    {
        if (_partyQ == null || _partyR == null) return;
        var (wx, wy) = HexGeometry.AxialToPixel(_partyQ.Value, _partyR.Value, hexSize);
        double r = hexSize * 0.4;
        var ellipse = new EllipseGeometry(new Point(wx, wy), r, r);
        var outlinePen = new Pen(Brushes.White, 2.5 / scale);
        outlinePen.Freeze();

        if (PartyTokenImage != null)
        {
            ctx.PushClip(ellipse);
            ctx.DrawImage(PartyTokenImage, new Rect(wx - r, wy - r, r * 2, r * 2));
            ctx.Pop();
            ctx.DrawGeometry(null, outlinePen, ellipse);
        }
        else
        {
            var fill = new SolidColorBrush(Color.FromRgb(255, 200, 0));
            fill.Freeze();
            ctx.DrawGeometry(fill, outlinePen, ellipse);
        }
    }

    // --- Ping ---

    // Fired when the ping is set or cleared so MainWindowViewModel can push a fresh bitmap to players.
    public event Action PingSendRequired;

    public double PingCenterX    => _pingCenterX;
    public double PingCenterY    => _pingCenterY;
    public double PingRadiusWorld => _pingRadiusWorld;
    public bool   IsPingVisible  => _pingVisible;

    // Activates a ping at the given world position and radius, auto-clears after 4 seconds.
    public void SetPing(double worldX, double worldY, double radius)
    {
        _pingCenterX    = worldX;
        _pingCenterY    = worldY;
        _pingRadiusWorld = Math.Max(radius, 1.0);
        _pingVisible    = true;

        _pingTimer?.Stop();
        _pingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _pingTimer.Tick += (_, _) => { _pingTimer.Stop(); ClearPing(); };
        _pingTimer.Start();

        RenderRequested?.Invoke();
        PingSendRequired?.Invoke();
    }

    public void ClearPing()
    {
        _pingVisible = false;
        RenderRequested?.Invoke();
        PingSendRequired?.Invoke();
    }

    // Draws the active ping circle. Called from both BuildDrawingVisual (player bitmap) and HexMapCanvas.
    public void DrawPing(DrawingContext ctx, double scale)
    {
        if (!_pingVisible) return;
        var ellipse = new EllipseGeometry(new Point(_pingCenterX, _pingCenterY), _pingRadiusWorld, _pingRadiusWorld);
        var fill = new SolidColorBrush(Color.FromArgb(50, 220, 30, 30));
        fill.Freeze();
        var stroke = new Pen(new SolidColorBrush(Color.FromRgb(210, 30, 30)), 2.5 / scale);
        stroke.Freeze();
        ctx.DrawGeometry(fill, stroke, ellipse);
    }

    // --- Place names ---

    // Reveals the place name of a hex (and optionally neighbors) to players.
    // Only adds hexes that actually have a label; no-ops otherwise.
    public void RevealLabel(int q, int r, bool includeNeighbors)
    {
        if (_cellLabels.ContainsKey((q, r)))
            _revealedLabels.Add((q, r));
        if (includeNeighbors)
            foreach (var n in HexGeometry.GetNeighbors(q, r, GridWidth, GridHeight))
                if (_cellLabels.ContainsKey(n))
                    _revealedLabels.Add(n);
        RenderRequested?.Invoke();
    }

    public bool IsLabelRevealed(int q, int r) => _revealedLabels.Contains((q, r));

    // Sets the selected hex for place-name editing (called via SelectCellCommand from right-click).
    private void SelectCell(object param)
    {
        if (param is not (int q, int r)) return;
        if (q < 0 || q >= GridWidth) return;
        int rOff = q / 2;
        if (r < -rOff || r >= GridHeight - rOff) return;
        _selectedQ = q;
        _selectedR = r;
        NotifyPropertyChange(nameof(SelectedCellLabel));
        NotifyPropertyChange(nameof(SelectedHexInfo));
        RenderRequested?.Invoke();
    }

    // Draws the place name (if any) at the lower-center of a hex. Text is not clipped to the hex.
    // revealed=true → normal white style (visible to players);
    // revealed=false → dimmed gray style (DM-only hint that the name is still hidden).
    public void DrawPlaceName(DrawingContext ctx, int q, int r, double hexSize, bool revealed)
    {
        if (!_cellLabels.TryGetValue((q, r), out var label) || string.IsNullOrEmpty(label)) return;
        var geom = GetOrCreatePlaceNameGeom(label, hexSize);
        if (geom == null) return;
        var (wx, wy) = HexGeometry.AxialToPixel(q, r, hexSize);
        double tx = wx - geom.Bounds.Width / 2.0;
        double ty = wy + hexSize * 0.50;
        ctx.PushTransform(new TranslateTransform(tx, ty));
        ctx.DrawGeometry(
            revealed ? PlaceNameRevealedFill : PlaceNameHiddenFill,
            revealed ? PlaceNameRevealedStroke : PlaceNameHiddenStroke,
            geom);
        ctx.Pop();
    }

    // Lazy per-text geometry cache; cleared whenever hexSize changes.
    private Geometry GetOrCreatePlaceNameGeom(string text, double hexSize)
    {
        if (Math.Abs(_placeNameCacheHexSize - hexSize) > 0.001)
        {
            _placeNameCache.Clear();
            _placeNameCacheHexSize = hexSize;
        }
        if (_placeNameCache.TryGetValue(text, out var cached)) return cached;
        var ft = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            hexSize * 0.28,
            LabelFillBrush,
            1.0);
        var geom = ft.BuildGeometry(new Point(0, 0));
        geom?.Freeze();
        _placeNameCache[text] = geom;
        return geom;
    }

    // --- Edge rendering ---

    // Draws all edge features touching hex (q,r). Called from both OnRender and BuildDrawingVisual.
    // Only draws from the canonical (owning) side of each shared border to avoid double-drawing.
    // When respectFog is true, an edge is skipped if both hexes sharing it are unexplored — otherwise
    // its color can bleed through the anti-aliasing seam between the two hexes' opaque fog polygons.
    public void DrawEdgesForHex(DrawingContext ctx, int q, int r, double hexSize, bool respectFog = false)
    {
        var edgesOnHex = GetEdgesForHex(q, r);
        if (edgesOnHex.Count == 0) return;

        var (wx, wy) = HexGeometry.AxialToPixel(q, r, hexSize);
        var corners  = HexGeometry.GetCorners(wx, wy, hexSize);

        foreach (var (dir, typeId) in edgesOnHex)
        {
            var canonical = HexGeometry.CanonicalizeEdge(q, r, dir);
            if (canonical.Q != q || canonical.R != r) continue;

            if (respectFog)
            {
                var (nq, nr) = HexGeometry.GetNeighborAt(q, r, dir);
                if (!IsExplored(q, r) && !IsExplored(nq, nr)) continue;
            }

            var edgeType = _edgeStore.GetById(typeId);
            if (edgeType == null) continue;

            var pen = GetEdgePen(edgeType, hexSize);
            var (ca, cb) = HexGeometry.GetEdgeCornerIndices(dir);

            if (!edgeType.IsCurved)
            {
                ctx.DrawLine(pen, corners[ca], corners[cb]);
                continue;
            }

            // Curved: draw two quadratic bezier halves, one from each end corner.
            var M = new Point((corners[ca].X + corners[cb].X) / 2.0,
                              (corners[ca].Y + corners[cb].Y) / 2.0);
            DrawCurvedHalf(ctx, pen, q, r, dir, ca, M, corners[ca], typeId, hexSize);
            DrawCurvedHalf(ctx, pen, q, r, dir, cb, M, corners[cb], typeId, hexSize);
        }
    }

    // Draws one half of a curved edge from the edge midpoint M toward corner at cornerIdx.
    // For each same-type adjacent edge at that corner where this edge is the canonical owner,
    // draws a quadratic bezier M → corner (control) → M_adj.
    // At a three-way junction (two adjacents), also fills the central gap with the edge color.
    // If no adjacent same-type edges exist, draws a straight half-line M → corner.
    // If adjacent edges exist but none are owned by this edge, draws nothing.
    private void DrawCurvedHalf(DrawingContext ctx, Pen pen,
        int q, int r, int dir, int cornerIdx,
        Point M, Point corner, string typeId, double hexSize)
    {
        var adjacents = FindSameTypeEdgesAtCorner(q, r, cornerIdx, dir, typeId);

        if (adjacents.Count == 0)
        {
            ctx.DrawLine(pen, M, corner);
            return;
        }

        var selfKey = HexGeometry.CanonicalizeEdge(q, r, dir);

        Point AdjMidpoint((int Q, int R, int Dir) e)
        {
            var (wx, wy) = HexGeometry.AxialToPixel(e.Q, e.R, hexSize);
            return HexGeometry.GetEdgeMidpoint(wx, wy, hexSize, e.Dir);
        }

        // Three-way junction: fill the central gap before drawing the arcs.
        if (adjacents.Count == 2
            && IsCanonicallySmaller(selfKey, adjacents[0])
            && IsCanonicallySmaller(selfKey, adjacents[1]))
        {
            DrawThreeWayFill(ctx, pen.Brush, M, AdjMidpoint(adjacents[0]), AdjMidpoint(adjacents[1]), corner);
        }

        foreach (var adj in adjacents)
        {
            if (!IsCanonicallySmaller(selfKey, adj)) continue;
            DrawQuadBezier(ctx, pen, M, corner, AdjMidpoint(adj));
        }
    }

    private static void DrawThreeWayFill(DrawingContext ctx, Brush brush,
        Point m1, Point m2, Point m3, Point corner)
    {
        var pf = new System.Windows.Media.PathFigure
        {
            StartPoint = m1,
            IsClosed   = true,
            IsFilled   = true
        };
        pf.Segments.Add(new System.Windows.Media.QuadraticBezierSegment(corner, m2, false));
        pf.Segments.Add(new System.Windows.Media.QuadraticBezierSegment(corner, m3, false));
        pf.Segments.Add(new System.Windows.Media.QuadraticBezierSegment(corner, m1, false));
        var pg = new System.Windows.Media.PathGeometry();
        pg.Figures.Add(pf);
        pg.Freeze();
        ctx.DrawGeometry(brush, null, pg);
    }

    // Returns all same-type edges (up to 2) adjacent at cornerIdx of hex (q,r),
    // excluding the current edge direction. Handles three-way junctions.
    private List<(int Q, int R, int Dir)> FindSameTypeEdgesAtCorner(
        int q, int r, int cornerIdx, int excludeDir, string typeId)
    {
        var result = new List<(int Q, int R, int Dir)>(2);

        var (d1, d2) = HexGeometry.GetCornerEdgeDirs(cornerIdx);
        int otherDir = (d1 == excludeDir) ? d2 : d1;

        var canonOther = HexGeometry.CanonicalizeEdge(q, r, otherDir);
        if (_edges.TryGetValue(canonOther, out var tid1) && tid1 == typeId)
            result.Add(canonOther);

        var (dq, dr, thirdDir) = HexGeometry.GetThirdEdgeAtCorner(cornerIdx);
        var thirdKey = (q + dq, r + dr, thirdDir);
        if (_edges.TryGetValue(thirdKey, out var tid2) && tid2 == typeId)
            result.Add(thirdKey);

        return result;
    }

    private static bool IsCanonicallySmaller(
        (int Q, int R, int Dir) a, (int Q, int R, int Dir) b)
    {
        if (a.Q != b.Q) return a.Q < b.Q;
        if (a.R != b.R) return a.R < b.R;
        return a.Dir < b.Dir;
    }

    private static void DrawQuadBezier(DrawingContext ctx, Pen pen,
        Point start, Point control, Point end)
    {
        var sg = new StreamGeometry();
        using (var sgc = sg.Open())
        {
            sgc.BeginFigure(start, false, false);
            sgc.QuadraticBezierTo(control, end, true, false);
        }
        sg.Freeze();
        ctx.DrawGeometry(null, pen, sg);
    }

    private Pen GetEdgePen(HexEdgeType edgeType, double hexSize)
    {
        if (Math.Abs(_edgePenCacheHexSize - hexSize) > 0.001)
        {
            _edgePenCache.Clear();
            _edgePenCacheHexSize = hexSize;
        }
        if (_edgePenCache.TryGetValue(edgeType.Id, out var cached)) return cached;

        var color = ParseHexColor(edgeType.Color);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        var pen = new Pen(brush, edgeType.StrokeWidth * hexSize)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap   = PenLineCap.Round,
            LineJoin      = PenLineJoin.Round
        };
        pen.Freeze();
        _edgePenCache[edgeType.Id] = pen;
        return pen;
    }

    private static Color ParseHexColor(string hex)
    {
        try
        {
            if (hex?.StartsWith("#") == true && hex.Length == 7)
            {
                byte r = Convert.ToByte(hex.Substring(1, 2), 16);
                byte g = Convert.ToByte(hex.Substring(3, 2), 16);
                byte b = Convert.ToByte(hex.Substring(5, 2), 16);
                return Color.FromRgb(r, g, b);
            }
        }
        catch { }
        return Colors.DodgerBlue;
    }

    // --- Hex labels (CCRR wargame convention: first two digits = column, last two = row) ---

    // Returns the 4-digit label for a hex: column*100 + visualRow, both 1-based.
    public static int GetHexLabel(int q, int r) => (q + 1) * 100 + (r + q / 2 + 1);

    // Draws the CCRR label at the top-center of a hex. Uses a per-grid frozen geometry cache.
    public void DrawHexLabel(DrawingContext ctx, int q, int r, double hexSize)
    {
        EnsureLabelCache(hexSize);
        int label = GetHexLabel(q, r);
        if (!_labelCache.TryGetValue(label, out var geom) || geom == null) return;

        var (wx, wy) = HexGeometry.AxialToPixel(q, r, hexSize);
        double tx = wx - geom.Bounds.Width  / 2.0;
        double ty = wy - hexSize * 0.72;
        ctx.PushTransform(new TranslateTransform(tx, ty));
        ctx.DrawGeometry(LabelFillBrush, LabelStrokePen, geom);
        ctx.Pop();
    }

    private void EnsureLabelCache(double hexSize)
    {
        if (!_labelCacheDirty && Math.Abs(_labelCacheHexSize - hexSize) < 0.001) return;
        BuildLabelCache(hexSize);
    }

    private void BuildLabelCache(double hexSize)
    {
        _labelCache.Clear();
        double fontSize = hexSize * 0.22;

        for (int q = 0; q < GridWidth; q++)
        {
            int rOff = q / 2;
            for (int r = -rOff; r < GridHeight - rOff; r++)
            {
                int label = GetHexLabel(q, r);
                if (_labelCache.ContainsKey(label)) continue;

                var ft = new FormattedText(
                    label.ToString("D4"),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    LabelTypeface,
                    fontSize,
                    LabelFillBrush,
                    1.0);

                var geom = ft.BuildGeometry(new Point(0, 0));
                geom?.Freeze();
                _labelCache[label] = geom;
            }
        }

        _labelCacheDirty    = false;
        _labelCacheHexSize  = hexSize;
    }

    // Returns the inclusive column (q) range that intersects the visible world area.
    private (int qMin, int qMax) GetVisibleColumnRange(double worldLeft, double worldRight, double hexSize)
    {
        // Flat-top hex: column pitch is hexSize * 1.5 along x.
        int qMin = Math.Max(0,             (int)Math.Floor(worldLeft   / (hexSize * 1.5)) - 2);
        int qMax = Math.Min(GridWidth - 1, (int)Math.Ceiling(worldRight / (hexSize * 1.5)) + 2);
        return (qMin, qMax);
    }

    // Draws all visible rows in one column.
    private void DrawColumn(DrawingContext ctx, int q, double hexSize,
                            double worldTop, double worldBottom, Pen outlinePen)
    {
        // Even-q offset layout: odd columns are shifted down by half a hex.
        // Row world-y for axial (q,r): wy = hexSize * Sqrt3 * (r + q/2)
        double rScale = hexSize * HexGeometry.Sqrt3;
        int rOff  = q / 2;
        double halfQ = q / 2.0;
        int rMin = Math.Max((int)Math.Floor(worldTop    / rScale - halfQ) - 1, -rOff);
        int rMax = Math.Min((int)Math.Ceiling(worldBottom / rScale - halfQ) + 1, GridHeight - 1 - rOff);

        for (int r = rMin; r <= rMax; r++)
            DrawHex(ctx, q, r, hexSize, outlinePen);
    }

    // Draws a single hex cell: fill (if painted) then outline. Edges are drawn in a separate pass.
    private void DrawHex(DrawingContext ctx, int q, int r, double hexSize, Pen outlinePen)
    {
        var (wx, wy) = HexGeometry.AxialToPixel(q, r, hexSize);
        var hexGeom  = HexGeometry.CreateStreamGeometry(wx, wy, hexSize);

        var cell = GetCell(q, r);
        if (cell != null)
            DrawTileFill(ctx, hexGeom, wx, wy, hexSize, GetTile(cell.TileId));

        ctx.DrawGeometry(null, outlinePen, hexGeom);
        DrawHexLabel(ctx, q, r, hexSize);
    }

    // Second-pass edge drawing for one column — called after all hex fills so edges are always on top.
    private void DrawColumnEdges(DrawingContext ctx, int q, double hexSize,
                                 double worldTop, double worldBottom, bool respectFog)
    {
        double rScale = hexSize * HexGeometry.Sqrt3;
        int rOff  = q / 2;
        double halfQ = q / 2.0;
        int rMin = Math.Max((int)Math.Floor(worldTop    / rScale - halfQ) - 1, -rOff);
        int rMax = Math.Min((int)Math.Ceiling(worldBottom / rScale - halfQ) + 1, GridHeight - 1 - rOff);
        for (int r = rMin; r <= rMax; r++)
            DrawEdgesForHex(ctx, q, r, hexSize, respectFog);
    }

    // Fills a hex with either a solid colour (preset tile) or a clipped bitmap (custom tile).
    private static void DrawTileFill(DrawingContext ctx, Geometry hexGeom,
                                     double wx, double wy, double hexSize, HexTile tile)
    {
        if (tile == null) return;

        if (tile.IsPreset)
        {
            ctx.DrawGeometry(new SolidColorBrush(tile.PresetColor), null, hexGeom);
        }
        else if (tile.Bitmap != null)
        {
            // Clip bitmap to hex shape; bitmap rect is centred on the hex centre.
            ctx.PushClip(hexGeom);
            ctx.DrawImage(tile.Bitmap, new Rect(wx - hexSize, wy - hexSize, hexSize * 2, hexSize * 2));
            ctx.Pop();
        }
    }

    // Rasterises the DrawingVisual to a WPF RenderTargetBitmap.
    private static RenderTargetBitmap RenderToTarget(DrawingVisual visual, int width, int height)
    {
        var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze();
        return rtb;
    }

    // Converts a WPF BitmapSource to a System.Drawing.Bitmap via PNG encoding.
    private static System.Drawing.Bitmap ConvertToBitmap(BitmapSource source)
    {
        using var ms = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        encoder.Save(ms);
        ms.Position = 0;
        return new System.Drawing.Bitmap(ms);
    }
}
