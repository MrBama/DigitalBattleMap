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

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (ViewModel == null) return;
        if (ShowFog)
        {
            if (IsTokenMode)
                TokenClickAt(e.GetPosition(this));
            else
                FogClickAt(e.GetPosition(this), explore: true);
            e.Handled = true;
            return;
        }
        CaptureMouse();
        ViewModel.BeginPaint();
        PaintAt(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        if (ViewModel == null) return;
        if (ShowFog)
            FogClickAt(e.GetPosition(this), explore: false);
        else
            SelectCellAt(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && ViewModel != null && !ShowFog)
        {
            PaintAt(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (ShowFog) return;
        ReleaseMouseCapture();
        ViewModel?.EndPaint();
        e.Handled = true;
    }

    private void PaintAt(Point screenPos)
    {
        double worldX = (screenPos.X - ViewModel.OffsetX) / ViewModel.Scale;
        double worldY = (screenPos.Y - ViewModel.OffsetY) / ViewModel.Scale;
        var (q, r) = HexGeometry.PixelToAxial(worldX, worldY, ViewModel.HexSize);
        ViewModel.PaintCell(q, r);
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
}
