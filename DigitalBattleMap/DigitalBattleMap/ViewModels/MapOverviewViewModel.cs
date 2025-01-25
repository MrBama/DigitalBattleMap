using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels;

public class MapOverviewViewModel : ViewModelBase
{
    const double _gridLineWidth = 2.0;
    const double _playerAreaIndicatorLineWidth = 15.0;
    const double _zoomFactor = 1.5;

    protected IMapSize _mapSize;
    private Bitmap _overviewBitmap;
    private Bitmap _fullOverviewBitmap;
    private Rectangle _area = new();
    private Rectangle _boudingBox = new();
    private Point<double> _mouseDownPosition = new();
    private bool _mouseDown;
    private Point<int> _bitmapToOrigin;
    private Size<int> _playerViewSize;

    public MapOverviewViewModel()
    {
        Initialize();
    }

    public MapOverviewViewModel(IMapSize mapSize)
    {
        _mapSize = mapSize;
        Initialize();
    }

    private void Initialize()
    {
        OverviewBitmap = BitmapTools.CreateEmptyBitmap();
        MouseCanvas = new MouseCanvasViewModel();
        MouseCanvas.OnLeftButtonDown += MouseDown;
        MouseCanvas.OnLeftButtonUp += MouseUp;
        MouseCanvas.OnMouseWheel += MouseScroll;
        MouseCanvas.OnFixRatioRectangleAreaSelected += FixRatioRectangleAreaSelected;
    }

    protected override void InitializeCommands()
    {
        ResetCommand = new RelayCommand(p => ResetView());
    }

    public event EventHandler<GridSizeZoomAndEnhanceEventArgs> OnGridSizeZoomAndEnhance;

    public BitmapSource OverviewBitmapSource { get => Get<BitmapSource>(); private set => Set(value); }
    public MouseCanvasViewModel MouseCanvas { get => Get<MouseCanvasViewModel>(); set => Set(value); }
    public ICommand ResetCommand { get; set; }

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
        _playerViewSize = new Size<int>(playerViewOverview.Bitmap.Width, playerViewOverview.Bitmap.Height);

        // Origin equals top left of player view
        var minX = Mathematics.Min(overviewBitmaps.Select(l => l.OffsetFromOrigin.X));
        var minY = Mathematics.Min(overviewBitmaps.Select(l => l.OffsetFromOrigin.Y));
        var maxX = Mathematics.Max(overviewBitmaps.Select(l => l.OffsetFromOrigin.X + l.Bitmap.Width));
        var maxY = Mathematics.Max(overviewBitmaps.Select(l => l.OffsetFromOrigin.Y + l.Bitmap.Height));
        var overviewSize = new Size<int>(Math.Abs(maxX - minX), Math.Abs(maxY - minY));
        _bitmapToOrigin = new Point<int>(-minX, -minY);
        var lineFactor = CalculateLineFactor(overviewSize);

        if(isGridShown)
        {
            var gridOverviewBitmap = CreateGridOverviewBitmap(overviewSize, _bitmapToOrigin, lineFactor, zoomFactor);
            var gridOverviewIndex = containsBackgroundOverview ? 1 : 0;
            overviewBitmaps.Insert(gridOverviewIndex, gridOverviewBitmap);
        }

        BitmapTools.DrawPlayerViewIndicator(playerViewOverview.Bitmap, (int)Math.Round(lineFactor * _playerAreaIndicatorLineWidth));

        _fullOverviewBitmap = BitmapTools.CreateMapOverview(overviewBitmaps, overviewSize, _bitmapToOrigin);
        OverviewBitmap = _fullOverviewBitmap;
        CalculateBoundingBox(overviewSize);
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
        if ((overviewSize.Width / (double)Constants.AspectRatio.Width * Constants.AspectRatio.Height) > overviewSize.Height)
        {
            // This means that width is the limiting factor, so height needs to be adjusted
            _boudingBox.Width = overviewSize.Width;
            _boudingBox.Height = (int)Math.Round((double)overviewSize.Width / Constants.AspectRatio.Width * Constants.AspectRatio.Height);
        }
        else
        {
            // This means that height is the limiting factor, so width needs to be adjusted
            _boudingBox.Width = (int)Math.Round((double)overviewSize.Height / Constants.AspectRatio.Height * Constants.AspectRatio.Width);
            _boudingBox.Height = overviewSize.Height;
        }

        _boudingBox.X = (int)Math.Round((_boudingBox.Width - overviewSize.Width) / 2.0 * -1.0);
        _boudingBox.Y = (int)Math.Round((_boudingBox.Height - overviewSize.Height) / 2.0 * -1.0);

        _area = _boudingBox;
    }

    private void MouseDown(object? sender, MouseButtonDataEventArgs e)
    {
        _mouseDownPosition = e.Position;
        _mouseDown = true;
    }

    private void MouseUp(object? sender, MouseButtonDataEventArgs e)
    {
        if (_fullOverviewBitmap != null && _mouseDown)
        {
            var distanceX = _mouseDownPosition.X - e.Position.X;
            distanceX = distanceX.Map(0, _mapSize.CanvasWidth, 0, _area.Width);
            _area.X += (int)distanceX;

            var distanceY = _mouseDownPosition.Y - e.Position.Y;
            distanceY = distanceY.Map(0, _mapSize.CanvasHeight, 0, _area.Height);
            _area.Y += (int)distanceY;

            ClampArea();

            OverviewBitmap = BitmapTools.CropBitmap(_fullOverviewBitmap, _area);
        }
    }

    private void MouseScroll(object? sender, MouseWheelDataEventArgs e)
    {
        // Do nothing when zooming out and the image is already max zoomed out 
        if(e.Delta < 0 && _area.Width == _boudingBox.Width && _area.Height == _boudingBox.Height)
        {
            return;
        }

        // Move mouse point towards center of screen
        var distanceX = e.Position.X.Map(0, _mapSize.CanvasWidth, 0, _area.Width);
        distanceX -= _area.Width / 2.0;
        _area.X = (int)(_area.X + distanceX);

        var distanceY = e.Position.Y.Map(0, _mapSize.CanvasHeight, 0, _area.Height);
        distanceY -= _area.Height / 2.0;
        _area.Y = (int)(_area.Y + distanceY);

        // Calculate new size and zoom factor
        var zoomFactor = e.Delta > 0 ? _zoomFactor : 1 / _zoomFactor;
        var newWidth = _area.Width / zoomFactor;
        var newHeight = _area.Height / zoomFactor;

        // Clamp size to maximum size of zoomed image
        if (newWidth > _boudingBox.Width || newHeight > _boudingBox.Height)
        {
            newWidth = _boudingBox.Width;
            newHeight = _boudingBox.Height;
            zoomFactor = _boudingBox.Width / _area.Width;
        }
        
        // Zoom in
        _area.X += (int)Math.Round((_area.Width - newWidth) / 2);
        _area.Y += (int)Math.Round((_area.Height - newHeight) / 2);
        _area.Width = (int)Math.Round(newWidth);
        _area.Height = (int)Math.Round(newHeight);

        // Move area back towards mouse point
        _area.X = (int)(_area.X - distanceX / zoomFactor);
        _area.Y = (int)(_area.Y - distanceY / zoomFactor);

        ClampArea();

        OverviewBitmap = BitmapTools.CropBitmap(_fullOverviewBitmap, _area);
    }

    // This function makes sure that atleast part of the image is visible
    private void ClampArea()
    {
        const double percentage = 50;

        var bitmapWidth = (double)_area.Width / _boudingBox.Width  * _fullOverviewBitmap.Width;
        var bitmapHeight = (double)_area.Height / _boudingBox.Height  * _fullOverviewBitmap.Height;
        var marginX = (int)Math.Round(bitmapWidth / 100 * percentage);
        var marginY = (int)Math.Round(bitmapHeight / 100 * percentage);

        _area.X = Math.Clamp(_area.X, -_area.Width + marginX, 0 + _fullOverviewBitmap.Width - marginX);
        _area.Y = Math.Clamp(_area.Y, -_area.Height + marginY, 0 + _fullOverviewBitmap.Height - marginY);
    }

    private void ResetView()
    {
        OverviewBitmap = _fullOverviewBitmap;
        CalculateBoundingBox(new Size<int>(_fullOverviewBitmap.Width, _fullOverviewBitmap.Height));
    }

    private void FixRatioRectangleAreaSelected(object? sender, RectangleF rectangle)
    {
        // 1. Convert from canvas to working area
        // 2. Shift origin from top left of area to top left of image
        // 3. Shift origin from top left of image to top left of player view
        var x = rectangle.X.Map(0, (float)_mapSize.CanvasWidth, 0, _area.Width);
        x += _area.X;
        x -= _bitmapToOrigin.X;

        var y = rectangle.Y.Map(0, (float)_mapSize.CanvasHeight, 0, _area.Height);
        y += _area.Y;
        y -= _bitmapToOrigin.Y;

        // Convert size from canvas to working area
        rectangle.Width = rectangle.Width.Map(0, (float)_mapSize.CanvasWidth, 0, _area.Width);
        rectangle.Height = rectangle.Height.Map(0, (float)_mapSize.CanvasHeight, 0, _area.Height);

        // ZoomAndEnhance function expects canvas coordinates.
        // Origin is top left of player view which means that everything is now relative to player view
        rectangle.X = x.Map(0, _playerViewSize.Width, 0, (float)_mapSize.CanvasWidth);
        rectangle.Y = y.Map(0, _playerViewSize.Height, 0, (float)_mapSize.CanvasHeight);
        rectangle.Width = rectangle.Width.Map(0, _playerViewSize.Width, 0, (float)_mapSize.CanvasWidth);
        rectangle.Height = rectangle.Height.Map(0, _playerViewSize.Height, 0, (float)_mapSize.CanvasHeight);

        OnGridSizeZoomAndEnhance?.Invoke(this, new GridSizeZoomAndEnhanceEventArgs() { rectangle = rectangle });
    }
}
