using DigitalBattleMap.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DigitalBattleMap.UIElements;

public class HexMapCanvas : FrameworkElement
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(HexcrawlMapViewModel), typeof(HexMapCanvas),
            new PropertyMetadata(null, OnViewModelChanged));

    public static readonly DependencyProperty ShowFogProperty =
        DependencyProperty.Register(nameof(ShowFog), typeof(bool), typeof(HexMapCanvas),
            new PropertyMetadata(false, (d, _) => ((HexMapCanvas)d).InvalidateVisual()));

    // When true: unexplored hexes are drawn with a semi-transparent dark gray overlay (DM view).
    public bool ShowFog
    {
        get => (bool)GetValue(ShowFogProperty);
        set => SetValue(ShowFogProperty, value);
    }

    public HexcrawlMapViewModel ViewModel
    {
        get => (HexcrawlMapViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (HexMapCanvas)d;
        if (e.OldValue is HexcrawlMapViewModel old)
            old.RenderRequested -= canvas.RequestRender;
        if (e.NewValue is HexcrawlMapViewModel vm)
        {
            vm.RenderRequested += canvas.RequestRender;
            vm.SetCanvasSize(canvas.ActualWidth, canvas.ActualHeight);
        }
        canvas.InvalidateVisual();
    }

    private void RequestRender() => InvalidateVisual();

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        ViewModel?.SetCanvasSize(ActualWidth, ActualHeight);
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var vm = ViewModel;
        double w = ActualWidth;
        double h = ActualHeight;

        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w, h));

        if (vm == null) return;

        dc.PushClip(new RectangleGeometry(new Rect(0, 0, w, h)));

        double scale   = vm.Scale;
        double ox      = vm.OffsetX;
        double oy      = vm.OffsetY;
        double hexSize = vm.HexSize;

        double worldLeft   = (0 - ox) / scale;
        double worldRight  = (w - ox) / scale;
        double worldTop    = (0 - oy) / scale;
        double worldBottom = (h - oy) / scale;

        // Clamp columns to grid and viewport
        int qMin = Math.Max(0,                (int)Math.Floor(worldLeft   / (hexSize * 1.5)) - 2);
        int qMax = Math.Min(vm.GridWidth - 1, (int)Math.Ceiling(worldRight / (hexSize * 1.5)) + 2);

        double rScale = hexSize * HexGeometry.Sqrt3; // wy = rScale*r + rScale/2*q

        var outlinePen = new Pen(new SolidColorBrush(Color.FromRgb(100, 100, 100)), 1.0 / scale);
        outlinePen.Freeze();

        dc.PushTransform(new TranslateTransform(ox, oy));
        dc.PushTransform(new ScaleTransform(scale, scale));

        for (int q = qMin; q <= qMax; q++)
        {
            // Even-q offset: each column shifts its row range up by floor(q/2).
            // Visible r for column q: wy = hexSize*(√3/2*q + √3*r)
            //   → r = worldY/(hexSize*√3) - q/2
            int rOff = q / 2;
            double halfQ = (double)q / 2.0;
            int rMin = Math.Max((int)Math.Floor(worldTop    / rScale - halfQ) - 1, -rOff);
            int rMax = Math.Min((int)Math.Ceiling(worldBottom / rScale - halfQ) + 1, vm.GridHeight - 1 - rOff);

            for (int r = rMin; r <= rMax; r++)
            {
                var (wx, wy) = HexGeometry.AxialToPixel(q, r, hexSize);
                if (wx + hexSize < worldLeft  || wx - hexSize > worldRight)  continue;
                if (wy + hexSize < worldTop   || wy - hexSize > worldBottom) continue;

                var hexGeom = HexGeometry.CreateStreamGeometry(wx, wy, hexSize);
                var cell    = vm.GetCell(q, r);

                if (cell != null)
                {
                    var tile = vm.GetTile(cell.TileId);
                    if (tile != null)
                    {
                        if (tile.IsPreset)
                        {
                            dc.DrawGeometry(new SolidColorBrush(tile.PresetColor), null, hexGeom);
                        }
                        else if (tile.Bitmap != null)
                        {
                            var bounds = new Rect(wx - hexSize, wy - hexSize, hexSize * 2, hexSize * 2);
                            dc.PushClip(hexGeom);
                            dc.DrawImage(tile.Bitmap, bounds);
                            dc.Pop();
                        }
                    }
                }

                dc.DrawGeometry(null, outlinePen, hexGeom);
                if (ShowFog) vm.DrawHexLabel(dc, q, r, hexSize);
            }
        }

        // Second pass: edges drawn on top of all tile fills
        for (int q = qMin; q <= qMax; q++)
        {
            int rOff = q / 2;
            double halfQ2 = (double)q / 2.0;
            int rMinE = Math.Max((int)Math.Floor(worldTop    / rScale - halfQ2) - 1, -rOff);
            int rMaxE = Math.Min((int)Math.Ceiling(worldBottom / rScale - halfQ2) + 1, vm.GridHeight - 1 - rOff);
            for (int r = rMinE; r <= rMaxE; r++)
                vm.DrawEdgesForHex(dc, q, r, hexSize);
        }

        if (ShowFog)
        {
            // Semi-transparent dark gray so the DM can still see what tile type is underneath.
            var fogBrush = new SolidColorBrush(Color.FromArgb(180, 40, 40, 40));
            fogBrush.Freeze();

            for (int q = qMin; q <= qMax; q++)
            {
                int rOff = q / 2;
                double halfQ = (double)q / 2.0;
                int rMin2 = Math.Max((int)Math.Floor(worldTop    / rScale - halfQ) - 1, -rOff);
                int rMax2 = Math.Min((int)Math.Ceiling(worldBottom / rScale - halfQ) + 1, vm.GridHeight - 1 - rOff);

                for (int r = rMin2; r <= rMax2; r++)
                {
                    if (vm.IsExplored(q, r)) continue;
                    var (wx, wy) = HexGeometry.AxialToPixel(q, r, hexSize);
                    dc.DrawGeometry(fogBrush, null, HexGeometry.CreateStreamGeometry(wx, wy, hexSize));
                }
            }
        }

        // Draw party token on top of fog (play mode only)
        if (ShowFog && vm.PartyQ.HasValue && vm.PartyR.HasValue)
        {
            var (tx, ty) = HexGeometry.AxialToPixel(vm.PartyQ.Value, vm.PartyR.Value, hexSize);
            double tokenR = hexSize * 0.4;
            var ellipse = new EllipseGeometry(new Point(tx, ty), tokenR, tokenR);
            var tokenOutline = new Pen(Brushes.White, 2.5 / scale);
            tokenOutline.Freeze();

            if (vm.PartyTokenImage != null)
            {
                dc.PushClip(ellipse);
                dc.DrawImage(vm.PartyTokenImage, new Rect(tx - tokenR, ty - tokenR, tokenR * 2, tokenR * 2));
                dc.Pop();
                dc.DrawGeometry(null, tokenOutline, ellipse);
            }
            else
            {
                var fill = new SolidColorBrush(Color.FromRgb(255, 200, 0));
                fill.Freeze();
                dc.DrawGeometry(fill, tokenOutline, ellipse);
            }
        }

        // Ping drag preview — DM-only dashed circle while the button is held.
        if (_pingDragging && _pingDragRadiusWorld > 0)
        {
            var prevEllipse = new EllipseGeometry(
                new Point(_pingCenterWorld.X, _pingCenterWorld.Y),
                _pingDragRadiusWorld, _pingDragRadiusWorld);
            var prevFill = new SolidColorBrush(Color.FromArgb(30, 255, 50, 50));
            prevFill.Freeze();
            var prevBrush = new SolidColorBrush(Color.FromArgb(200, 255, 50, 50));
            prevBrush.Freeze();
            var prevPen = new Pen(prevBrush, 2.0 / scale) { DashStyle = DashStyles.Dash };
            prevPen.Freeze();
            dc.DrawGeometry(prevFill, prevPen, prevEllipse);
        }

        // Finalized ping circle — visible on both DM canvas and player bitmap.
        if (ShowFog) vm.DrawPing(dc, scale);

        // Selected hex highlight in editor mode.
        if (!ShowFog && vm.SelectedQ.HasValue && vm.SelectedR.HasValue)
        {
            var (sx, sy) = HexGeometry.AxialToPixel(vm.SelectedQ.Value, vm.SelectedR.Value, hexSize);
            var selGeom = HexGeometry.CreateStreamGeometry(sx, sy, hexSize);
            var selFill = new SolidColorBrush(Color.FromArgb(60, 255, 220, 0));
            selFill.Freeze();
            var selPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 220, 0)), 2.5 / scale);
            selPen.Freeze();
            dc.DrawGeometry(selFill, selPen, selGeom);
        }

        // Place names: editor shows all as revealed; play mode shows all with reveal-status styling
        // (white = player can see it, gray = DM-only hint that the name is still hidden).
        {
            for (int q = qMin; q <= qMax; q++)
            {
                int rOff = q / 2;
                double halfQ = (double)q / 2.0;
                int rMinL = Math.Max((int)Math.Floor(worldTop    / rScale - halfQ) - 1, -rOff);
                int rMaxL = Math.Min((int)Math.Ceiling(worldBottom / rScale - halfQ) + 1, vm.GridHeight - 1 - rOff);
                for (int r = rMinL; r <= rMaxL; r++)
                {
                    bool revealed = !ShowFog || vm.IsLabelRevealed(q, r);
                    vm.DrawPlaceName(dc, q, r, hexSize, revealed);
                }
            }
        }

        dc.Pop(); // scale
        dc.Pop(); // translate

        // Rectangle zoom selection — drawn in screen space, clipped to canvas.
        if (_rectDragging)
        {
            double rx = Math.Min(_rectStartScreen.X, _rectCurrentScreen.X);
            double ry = Math.Min(_rectStartScreen.Y, _rectCurrentScreen.Y);
            double rw = Math.Abs(_rectCurrentScreen.X - _rectStartScreen.X);
            double rh = Math.Abs(_rectCurrentScreen.Y - _rectStartScreen.Y);
            var selFill = new SolidColorBrush(Color.FromArgb(30, 100, 180, 255));
            selFill.Freeze();
            var selBrush = new SolidColorBrush(Color.FromArgb(200, 80, 150, 255));
            selBrush.Freeze();
            var selPen = new Pen(selBrush, 1.5) { DashStyle = DashStyles.Dash };
            selPen.Freeze();
            dc.DrawRectangle(selFill, selPen, new Rect(rx, ry, rw, rh));
        }

        dc.Pop(); // clip
    }

    public static readonly DependencyProperty FogExploreCommandProperty =
        DependencyProperty.Register(nameof(FogExploreCommand), typeof(ICommand), typeof(HexMapCanvas));
    public static readonly DependencyProperty FogCoverCommandProperty =
        DependencyProperty.Register(nameof(FogCoverCommand), typeof(ICommand), typeof(HexMapCanvas));
    public static readonly DependencyProperty PlaceTokenCommandProperty =
        DependencyProperty.Register(nameof(PlaceTokenCommand), typeof(ICommand), typeof(HexMapCanvas));
    public static readonly DependencyProperty IsTokenModeProperty =
        DependencyProperty.Register(nameof(IsTokenMode), typeof(bool), typeof(HexMapCanvas),
            new PropertyMetadata(false));
    public static readonly DependencyProperty SelectCellCommandProperty =
        DependencyProperty.Register(nameof(SelectCellCommand), typeof(ICommand), typeof(HexMapCanvas));
    public static readonly DependencyProperty IsPingModeProperty =
        DependencyProperty.Register(nameof(IsPingMode), typeof(bool), typeof(HexMapCanvas),
            new PropertyMetadata(false));

    // Bound to HexPlayViewModel.ExploreCommand / CoverCommand when ShowFog is true.
    // Parameter passed is a ValueTuple<int,int> (q, r).
    public ICommand FogExploreCommand
    {
        get => (ICommand)GetValue(FogExploreCommandProperty);
        set => SetValue(FogExploreCommandProperty, value);
    }
    public ICommand FogCoverCommand
    {
        get => (ICommand)GetValue(FogCoverCommandProperty);
        set => SetValue(FogCoverCommandProperty, value);
    }
    // Bound to HexPlayViewModel.PlaceTokenCommand; parameter is ValueTuple<int,int> (q, r).
    public ICommand PlaceTokenCommand
    {
        get => (ICommand)GetValue(PlaceTokenCommandProperty);
        set => SetValue(PlaceTokenCommandProperty, value);
    }
    // When true, left-click places the party token instead of exploring.
    public bool IsTokenMode
    {
        get => (bool)GetValue(IsTokenModeProperty);
        set => SetValue(IsTokenModeProperty, value);
    }
    // Right-click in editor mode calls this to select a hex for place-name editing.
    public ICommand SelectCellCommand
    {
        get => (ICommand)GetValue(SelectCellCommandProperty);
        set => SetValue(SelectCellCommandProperty, value);
    }
    // When true, left-click+drag draws a ping circle instead of exploring / placing token.
    public bool IsPingMode
    {
        get => (bool)GetValue(IsPingModeProperty);
        set => SetValue(IsPingModeProperty, value);
    }

    // Ping drag state (canvas-local; not part of the view model).
    private bool   _pingDragging;
    private Point  _pingCenterWorld;
    private double _pingDragRadiusWorld;

    // Rectangle zoom drag state (screen coordinates).
    private bool  _rectDragging;
    private Point _rectStartScreen;
    private Point _rectCurrentScreen;

    // Edge paint gesture state (canvas-local).
    private bool _edgePainting;

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (ViewModel == null) return;
        if (ViewModel.IsRectZoomMode)
        {
            _rectStartScreen = _rectCurrentScreen = e.GetPosition(this);
            _rectDragging = true;
            CaptureMouse();
            e.Handled = true;
            return;
        }
        if (ShowFog)
        {
            if (IsPingMode)
                BeginPingDrag(e.GetPosition(this));
            else if (IsTokenMode)
                TokenClickAt(e.GetPosition(this));
            else
                FogClickAt(e.GetPosition(this), explore: true);
            e.Handled = true;
            return;
        }
        CaptureMouse();
        if (ViewModel.IsEdgePaintMode)
        {
            _edgePainting = true;
            ViewModel.BeginEdgePaint();
            PaintEdgeAt(e.GetPosition(this));
        }
        else
        {
            ViewModel.BeginPaint();
            PaintAt(e.GetPosition(this));
        }
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        if (ViewModel == null) return;
        if (ShowFog)
            FogClickAt(e.GetPosition(this), explore: false);
        else if (ViewModel.IsEdgePaintMode)
            RemoveEdgeAt(e.GetPosition(this));
        else
            SelectCellAt(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_pingDragging && ViewModel != null)
        {
            UpdatePingDrag(e.GetPosition(this));
            e.Handled = true;
            return;
        }
        if (_rectDragging)
        {
            _rectCurrentScreen = e.GetPosition(this);
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        if (e.LeftButton == MouseButtonState.Pressed && ViewModel != null && !ShowFog)
        {
            if (_edgePainting)
                PaintEdgeAt(e.GetPosition(this));
            else
                PaintAt(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_rectDragging)
        {
            _rectDragging = false;
            ReleaseMouseCapture();
            FinalizeRectZoom();
            e.Handled = true;
            return;
        }
        if (_pingDragging)
        {
            FinalizePingDrag();
            e.Handled = true;
            return;
        }
        if (ShowFog) return;
        ReleaseMouseCapture();
        if (_edgePainting)
        {
            _edgePainting = false;
            ViewModel?.EndEdgePaint();
        }
        else
        {
            ViewModel?.EndPaint();
        }
        e.Handled = true;
    }

    private void PaintAt(Point screenPos)
    {
        double worldX = (screenPos.X - ViewModel.OffsetX) / ViewModel.Scale;
        double worldY = (screenPos.Y - ViewModel.OffsetY) / ViewModel.Scale;
        var (q, r) = HexGeometry.PixelToAxial(worldX, worldY, ViewModel.HexSize);
        ViewModel.PaintCell(q, r);
    }

    private void PaintEdgeAt(Point screenPos)
    {
        double worldX = (screenPos.X - ViewModel.OffsetX) / ViewModel.Scale;
        double worldY = (screenPos.Y - ViewModel.OffsetY) / ViewModel.Scale;
        var (q, r) = HexGeometry.PixelToAxial(worldX, worldY, ViewModel.HexSize);
        int dir = NearestEdgeDir(worldX, worldY, q, r, ViewModel.HexSize);
        if (dir >= 0) ViewModel.PaintEdge(q, r, dir);
    }

    private void RemoveEdgeAt(Point screenPos)
    {
        double worldX = (screenPos.X - ViewModel.OffsetX) / ViewModel.Scale;
        double worldY = (screenPos.Y - ViewModel.OffsetY) / ViewModel.Scale;
        var (q, r) = HexGeometry.PixelToAxial(worldX, worldY, ViewModel.HexSize);
        int dir = NearestEdgeDir(worldX, worldY, q, r, ViewModel.HexSize);
        if (dir >= 0) ViewModel.RemoveEdgeAt(q, r, dir);
    }

    // Returns the direction index (0-5) of the nearest edge midpoint within 45% of hexSize,
    // or -1 if the cursor is too far from any edge midpoint.
    private static int NearestEdgeDir(double worldX, double worldY, int q, int r, double hexSize)
    {
        var (wx, wy) = HexGeometry.AxialToPixel(q, r, hexSize);
        double threshold = 0.45 * hexSize;
        int bestDir = -1;
        double bestDist = threshold;
        for (int dir = 0; dir < 6; dir++)
        {
            var mid = HexGeometry.GetEdgeMidpoint(wx, wy, hexSize, dir);
            double dx = worldX - mid.X;
            double dy = worldY - mid.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < bestDist) { bestDist = dist; bestDir = dir; }
        }
        return bestDir;
    }

    private void FogClickAt(Point screenPos, bool explore)
    {
        double worldX = (screenPos.X - ViewModel.OffsetX) / ViewModel.Scale;
        double worldY = (screenPos.Y - ViewModel.OffsetY) / ViewModel.Scale;
        var (q, r) = HexGeometry.PixelToAxial(worldX, worldY, ViewModel.HexSize);
        var cmd = explore ? FogExploreCommand : FogCoverCommand;
        cmd?.Execute((q, r));
    }

    private void TokenClickAt(Point screenPos)
    {
        double worldX = (screenPos.X - ViewModel.OffsetX) / ViewModel.Scale;
        double worldY = (screenPos.Y - ViewModel.OffsetY) / ViewModel.Scale;
        var (q, r) = HexGeometry.PixelToAxial(worldX, worldY, ViewModel.HexSize);
        PlaceTokenCommand?.Execute((q, r));
    }

    private void SelectCellAt(Point screenPos)
    {
        double worldX = (screenPos.X - ViewModel.OffsetX) / ViewModel.Scale;
        double worldY = (screenPos.Y - ViewModel.OffsetY) / ViewModel.Scale;
        var (q, r) = HexGeometry.PixelToAxial(worldX, worldY, ViewModel.HexSize);
        SelectCellCommand?.Execute((q, r));
    }

    private void FinalizeRectZoom()
    {
        double screenLeft   = Math.Min(_rectStartScreen.X, _rectCurrentScreen.X);
        double screenTop    = Math.Min(_rectStartScreen.Y, _rectCurrentScreen.Y);
        double screenRight  = Math.Max(_rectStartScreen.X, _rectCurrentScreen.X);
        double screenBottom = Math.Max(_rectStartScreen.Y, _rectCurrentScreen.Y);

        if (screenRight - screenLeft < 10 || screenBottom - screenTop < 10)
        {
            InvalidateVisual();
            return;
        }

        double worldLeft   = (screenLeft   - ViewModel.OffsetX) / ViewModel.Scale;
        double worldTop    = (screenTop    - ViewModel.OffsetY) / ViewModel.Scale;
        double worldRight  = (screenRight  - ViewModel.OffsetX) / ViewModel.Scale;
        double worldBottom = (screenBottom - ViewModel.OffsetY) / ViewModel.Scale;
        ViewModel.ZoomToRect(worldLeft, worldTop, worldRight, worldBottom);
        ViewModel.IsRectZoomMode = false;
    }

    private void BeginPingDrag(Point screenPos)
    {
        double worldX = (screenPos.X - ViewModel.OffsetX) / ViewModel.Scale;
        double worldY = (screenPos.Y - ViewModel.OffsetY) / ViewModel.Scale;
        _pingCenterWorld     = new Point(worldX, worldY);
        _pingDragRadiusWorld = 0;
        _pingDragging        = true;
        CaptureMouse();
        InvalidateVisual();
    }

    private void UpdatePingDrag(Point screenPos)
    {
        double worldX = (screenPos.X - ViewModel.OffsetX) / ViewModel.Scale;
        double worldY = (screenPos.Y - ViewModel.OffsetY) / ViewModel.Scale;
        double dx = worldX - _pingCenterWorld.X;
        double dy = worldY - _pingCenterWorld.Y;
        _pingDragRadiusWorld = Math.Sqrt(dx * dx + dy * dy);
        InvalidateVisual();
    }

    private void FinalizePingDrag()
    {
        _pingDragging = false;
        ReleaseMouseCapture();
        if (_pingDragRadiusWorld > 0)
            ViewModel?.SetPing(_pingCenterWorld.X, _pingCenterWorld.Y, _pingDragRadiusWorld);
        InvalidateVisual();
    }
}
