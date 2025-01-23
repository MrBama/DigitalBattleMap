using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Imaging;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace DigitalBattleMap.ViewModels;
public class MapOverviewViewModel : ViewModelBase
{
    const double _gridLineWidth = 2.0;
    const double _playerAreaIndicatorLineWidth = 15.0;

    protected IMapSize _mapSize;
    private Bitmap _overviewBitmap;
    private Rectangle _area = new Rectangle();

    public MapOverviewViewModel()
    {
        OverviewBitmap = BitmapTools.CreateEmptyBitmap();
    }

    public MapOverviewViewModel(IMapSize mapSize)
    {
        _mapSize = mapSize;
        OverviewBitmap = BitmapTools.CreateEmptyBitmap();
    }

    protected override void InitializeCommands()
    {
    }

    public BitmapSource OverviewBitmapSource { get => Get<BitmapSource>(); private set => Set(value); }

    private Bitmap OverviewBitmap
    {
        get => _overviewBitmap;
        set
        {
            if (value != _overviewBitmap)
            {
                _overviewBitmap = value;
                OverviewBitmapSource = value.ToBitmapImage();
            }
        }
    }

    public void CreateOverview(List<OverviewBitmap> overviewBitmaps, double zoomFactor, bool containsBackgroundOverview, bool isGridShown)
    {
        var playerViewOverview = new OverviewBitmap { Bitmap = BitmapTools.CreateEmptyBitmap(), OffsetFromOrigin = new Point<int>() };
        playerViewOverview.Resize(zoomFactor);
        overviewBitmaps.Add(playerViewOverview);

        // Origin equals top left of player view
        var minX = Mathematics.Min(overviewBitmaps.Select(l => l.OffsetFromOrigin.X));
        var minY = Mathematics.Min(overviewBitmaps.Select(l => l.OffsetFromOrigin.Y));
        var maxX = Mathematics.Max(overviewBitmaps.Select(l => l.OffsetFromOrigin.X + l.Bitmap.Width));
        var maxY = Mathematics.Max(overviewBitmaps.Select(l => l.OffsetFromOrigin.Y + l.Bitmap.Height));
        var overviewSize = new Size<int>(Math.Abs(maxX - minX), Math.Abs(maxY - minY));
        var bitmapToOrigin = new Point<int>(-minX, -minY);
        var lineFactor = CalculateLineFactor(overviewSize);

        if(isGridShown)
        {
            var gridOverviewBitmap = CreateGridOverviewBitmap(overviewSize, bitmapToOrigin, lineFactor, zoomFactor);
            var gridOverviewIndex = containsBackgroundOverview ? 1 : 0;
            overviewBitmaps.Insert(gridOverviewIndex, gridOverviewBitmap);
        }

        BitmapTools.DrawPlayerViewIndicator(playerViewOverview.Bitmap, (int)Math.Round(lineFactor * _playerAreaIndicatorLineWidth));

        OverviewBitmap = BitmapTools.CreateMapOverview(overviewBitmaps, overviewSize, bitmapToOrigin);
        CalculateBoundingBox(overviewSize);
    }

    public void Zoom()
    {
        var zoomFactor = 1.5;
        var newWidth = _area.Width / zoomFactor;
        var newHeight = _area.Height / zoomFactor;
        _area.X += (int)Math.Round((_area.Width - newWidth) / 2);
        _area.Y += (int)Math.Round((_area.Height - newHeight) / 2);
        _area.Width = (int)Math.Round(newWidth);
        _area.Height = (int)Math.Round(newHeight);

        OverviewBitmap = BitmapTools.CropBitmap(OverviewBitmap, _area);

        // After cropping the bitmap is equal to _area and thus the offset can be set to 0,0
        _area.X = 0;
        _area.Y = 0;
    }

    private double CalculateLineFactor(Size<int> overviewSize)
    {
        var distanceOverview = Math.Sqrt(Math.Pow(overviewSize.Width, 2) + Math.Pow(overviewSize.Height, 2));
        var distancePlayerView = Math.Sqrt(Math.Pow(Constants.MapSize.Width, 2) + Math.Pow(Constants.MapSize.Height, 2));
        return distanceOverview / distancePlayerView;
    }

    private OverviewBitmap CreateGridOverviewBitmap(Size<int> overviewSize, Point<int> bitmapToOrigin, double lineFactor, double zoomFactor)
    {
        var gridOrigin = new Point<int>(
            (int)Math.Round(Constants.MapSize.Width / 2 * zoomFactor),
            (int)Math.Round(Constants.MapSize.Height / 2 * zoomFactor));
        gridOrigin.X += bitmapToOrigin.X;
        gridOrigin.Y += bitmapToOrigin.Y;

        var gridBitmap = new Bitmap(overviewSize.Width, overviewSize.Height);
        var penSize = (int)Math.Round(_gridLineWidth * lineFactor);
        BitmapTools.DrawGrid(gridBitmap, (int)Math.Round(_mapSize.GridSize * zoomFactor), gridOrigin, penSize);

        return new OverviewBitmap { Bitmap = gridBitmap, OffsetFromOrigin = new Point<int>(-bitmapToOrigin.X, -bitmapToOrigin.Y) };
    }

    private void CalculateBoundingBox(Size<int> overviewSize)
    {
        if ((overviewSize.Width / 16.0 * 9.0) > overviewSize.Height)
        {
            // This means that width is the limiting factor, so height needs to be adjusted

            //var b = new Bitmap(overviewSize.Width, (int)Math.Round((double)overviewSize.Width / 16 * 9));

            //using var graphics = Graphics.FromImage(b);
            //graphics.DrawImage(OverviewBitmap, (b.Width / 2) - (OverviewBitmap.Width / 2), (b.Height / 2) - (OverviewBitmap.Height / 2));
            //graphics.DrawRectangle(new System.Drawing.Pen(System.Drawing.Color.DarkOrange, 20), 0, 0, b.Width, b.Height);

            //ImageTester.ShowImage(b, "");

            _area.Width = overviewSize.Width;
            _area.Height = (int)Math.Round((double)overviewSize.Width / 16 * 9);

        }
        else
        {
            // This means that height is the limiting factor, so width needs to be adjusted

            //var b = new Bitmap((int)Math.Round((double)overviewSize.Height / 9 * 16), overviewSize.Height);
            //using var graphics = Graphics.FromImage(b);
            //graphics.DrawImage(OverviewBitmap, (b.Width / 2) - (OverviewBitmap.Width / 2), (b.Height / 2) - (OverviewBitmap.Height / 2));
            //graphics.DrawRectangle(new System.Drawing.Pen(System.Drawing.Color.DarkOrange, 20), 0, 0, b.Width, b.Height);

            _area.Width = (int)Math.Round((double)overviewSize.Height / 9 * 16);
            _area.Height = overviewSize.Height;
        }

        _area.X = (int)Math.Round((_area.Width - overviewSize.Width) / 2.0 * -1.0);
        _area.Y = (int)Math.Round((_area.Height - overviewSize.Height) / 2.0 * -1.0);
    }
}
