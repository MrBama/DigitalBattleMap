using DigitalBattleMap.UIElements;
using DigitalBattleMap.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels;

public class HexCutoutWindowViewModel : ViewModelBase
{
    private const double DisplayMaxSize = 480.0;
    private BitmapImage _sourceImage;

    public HexCutoutWindowViewModel(string imagePath)
    {
        _sourceImage = new BitmapImage(new Uri(imagePath));
        SourceImage = _sourceImage;
        TileName = Path.GetFileNameWithoutExtension(imagePath);

        double aspect = _sourceImage.PixelWidth / (double)_sourceImage.PixelHeight;
        if (aspect >= 1)
        {
            DisplayWidth  = DisplayMaxSize;
            DisplayHeight = DisplayMaxSize / aspect;
        }
        else
        {
            DisplayHeight = DisplayMaxSize;
            DisplayWidth  = DisplayMaxSize * aspect;
        }
        DisplayOffsetX = (DisplayMaxSize - DisplayWidth)  / 2;
        DisplayOffsetY = (DisplayMaxSize - DisplayHeight) / 2;

        HexCenterX = _sourceImage.PixelWidth  / 2.0;
        HexCenterY = _sourceImage.PixelHeight / 2.0;
        HexRadius  = Math.Min(_sourceImage.PixelWidth, _sourceImage.PixelHeight) * 0.4;
        MaxRadius  = Math.Min(_sourceImage.PixelWidth, _sourceImage.PixelHeight) / 2.0;

        UpdateOverlayPoints();
    }

    public HexCutoutWindowViewModel() { }

    protected override void InitializeCommands()
    {
        ConfirmCommand = new RelayCommand(_ => Confirm(), _ => !string.IsNullOrWhiteSpace(TileName));
        CancelCommand  = new RelayCommand(_ => { });
    }

    public BitmapImage SourceImage    { get => Get<BitmapImage>(); private set => Set(value); }
    public string TileName            { get => Get<string>();      set => Set(value); }
    public string OverlayPoints       { get => Get<string>();      private set => Set(value); }
    public double DisplayWidth        { get => Get<double>();      private set => Set(value); }
    public double DisplayHeight       { get => Get<double>();      private set => Set(value); }
    public double DisplayOffsetX      { get => Get<double>();      private set => Set(value); }
    public double DisplayOffsetY      { get => Get<double>();      private set => Set(value); }
    public double MaxRadius           { get => Get<double>();      private set => Set(value); }

    public double HexCenterX
    {
        get => Get<double>();
        set { Set(value); UpdateOverlayPoints(); }
    }
    public double HexCenterY
    {
        get => Get<double>();
        set { Set(value); UpdateOverlayPoints(); }
    }
    public double HexRadius
    {
        get => Get<double>();
        set { Set(value); UpdateOverlayPoints(); }
    }

    public BitmapSource Result { get; private set; }

    public ICommand ConfirmCommand { get; private set; }
    public ICommand CancelCommand  { get; private set; }

    public void SetHexCenterFromDisplayCoords(double dx, double dy)
    {
        if (_sourceImage == null || DisplayWidth <= 0 || DisplayHeight <= 0) return;
        HexCenterX = (dx - DisplayOffsetX) / DisplayWidth  * _sourceImage.PixelWidth;
        HexCenterY = (dy - DisplayOffsetY) / DisplayHeight * _sourceImage.PixelHeight;
    }

    private void UpdateOverlayPoints()
    {
        if (_sourceImage == null || DisplayWidth <= 0 || DisplayHeight <= 0) return;
        double scaleX = DisplayWidth  / _sourceImage.PixelWidth;
        double scaleY = DisplayHeight / _sourceImage.PixelHeight;
        double dcx = HexCenterX * scaleX + DisplayOffsetX;
        double dcy = HexCenterY * scaleY + DisplayOffsetY;
        double dr  = HexRadius  * Math.Min(scaleX, scaleY);
        var corners = HexGeometry.GetCorners(dcx, dcy, dr);
        OverlayPoints = string.Join(" ", corners.Select(p => $"{p.X:F1},{p.Y:F1}"));
    }

    private void Confirm()
    {
        int outputSize = Math.Max(1, (int)(HexRadius * 2));
        var hexGeom = HexGeometry.CreateStreamGeometry(outputSize / 2.0, outputSize / 2.0, HexRadius);

        var visual = new DrawingVisual();
        using (var ctx = visual.RenderOpen())
        {
            ctx.PushClip(hexGeom);
            var destRect = new Rect(
                outputSize / 2.0 - HexCenterX,
                outputSize / 2.0 - HexCenterY,
                _sourceImage.PixelWidth,
                _sourceImage.PixelHeight);
            ctx.DrawImage(_sourceImage, destRect);
            ctx.Pop();
        }

        var rtb = new RenderTargetBitmap(outputSize, outputSize, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze();
        Result = rtb;
    }
}
