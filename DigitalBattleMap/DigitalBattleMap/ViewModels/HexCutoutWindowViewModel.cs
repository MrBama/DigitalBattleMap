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
    private const double DisplayMaxSize  = 480.0;
    private const int    MaxOutputPixels = 512;

    private BitmapImage _sourceImage;
    private double _dcx; // hex center in canvas/display space
    private double _dcy;

    public HexCutoutWindowViewModel(string imagePath)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.UriSource    = new Uri(imagePath, UriKind.Absolute);
        bmp.CacheOption  = BitmapCacheOption.OnLoad;
        bmp.EndInit();
        bmp.Freeze();
        _sourceImage = bmp;
        SourceImage  = _sourceImage;
        TileName     = Path.GetFileNameWithoutExtension(imagePath);

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
        DisplayOffsetX = (DisplayMaxSize - DisplayWidth)  / 2.0;
        DisplayOffsetY = (DisplayMaxSize - DisplayHeight) / 2.0;

        // Set center before radius so the HexRadius setter's UpdateOverlayPoints call is correct
        _dcx = DisplayOffsetX + DisplayWidth  / 2.0;
        _dcy = DisplayOffsetY + DisplayHeight / 2.0;

        MaxRadius = Math.Min(DisplayWidth, DisplayHeight) / 2.0;
        HexRadius = MaxRadius * 0.8;
    }

    public HexCutoutWindowViewModel() { }

    protected override void InitializeCommands()
    {
        ConfirmCommand = new RelayCommand(_ => Confirm(), _ => !string.IsNullOrWhiteSpace(TileName));
    }

    public BitmapImage SourceImage { get => Get<BitmapImage>(); private set => Set(value); }
    public string      TileName    { get => Get<string>();      set => Set(value); }
    public string      OverlayPoints  { get => Get<string>();   private set => Set(value); }
    public double      DisplayWidth   { get => Get<double>();   private set => Set(value); }
    public double      DisplayHeight  { get => Get<double>();   private set => Set(value); }
    public double      DisplayOffsetX { get => Get<double>();   private set => Set(value); }
    public double      DisplayOffsetY { get => Get<double>();   private set => Set(value); }
    public double      MaxRadius      { get => Get<double>();   private set => Set(value); }

    public double HexRadius
    {
        get => Get<double>();
        set { Set(value); UpdateOverlayPoints(); }
    }

    public BitmapSource Result { get; private set; }

    public ICommand ConfirmCommand { get; private set; }

    public (double cx, double cy) GetDisplayCenter() => (_dcx, _dcy);

    // Called from code-behind while the user drags on the canvas
    public void SetHexCenterFromDisplayCoords(double dx, double dy)
    {
        if (_sourceImage == null) return;
        _dcx = dx;
        _dcy = dy;
        UpdateOverlayPoints();
    }

    private void UpdateOverlayPoints()
    {
        if (_sourceImage == null) return;
        var corners = HexGeometry.GetCorners(_dcx, _dcy, HexRadius);
        OverlayPoints = string.Join(" ", corners.Select(p => $"{p.X:F1},{p.Y:F1}"));
    }

    private void Confirm()
    {
        if (_sourceImage == null) return;

        double scaleX       = DisplayWidth  / _sourceImage.PixelWidth;
        double scaleY       = DisplayHeight / _sourceImage.PixelHeight;
        double displayScale = Math.Min(scaleX, scaleY);

        // Convert display-space values back to source pixels
        double srcRadius = HexRadius / displayScale;
        double srcCX     = (_dcx - DisplayOffsetX) / scaleX;
        double srcCY     = (_dcy - DisplayOffsetY) / scaleY;

        // Cap output size to avoid huge bitmaps
        int    outputSize    = Math.Min((int)(srcRadius * 2), MaxOutputPixels);
        if (outputSize < 2) return;
        double renderRadius  = outputSize / 2.0;
        double renderScale   = renderRadius / srcRadius;

        var hexGeom = HexGeometry.CreateStreamGeometry(renderRadius, renderRadius, renderRadius);

        var visual = new DrawingVisual();
        using (var ctx = visual.RenderOpen())
        {
            ctx.PushClip(hexGeom);
            ctx.DrawImage(_sourceImage, new Rect(
                renderRadius - srcCX  * renderScale,
                renderRadius - srcCY  * renderScale,
                _sourceImage.PixelWidth  * renderScale,
                _sourceImage.PixelHeight * renderScale));
            ctx.Pop();
        }

        var rtb = new RenderTargetBitmap(outputSize, outputSize, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze();
        Result = rtb;
    }
}
