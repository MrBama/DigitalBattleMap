using System;
using System.Drawing;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace DigitalBattleMap
{
    public class BackgroundController
    {
        private Bitmap _backgroundBitmap;
        private Bitmap _fullBackgroundBitmap;
        private Rectangle _area;
        private IWindowService _windowService;
        private Size<int> _bitmapSize;
        private Size<double> _canvasSize;
        private Point<double> _mouseDownPosition;
        private bool _mouseDown;

        public BackgroundController(IWindowService windowService)
        {
            _windowService = windowService;
            _backgroundBitmap = BitmapTools.CreateEmptyBitmap();
            _bitmapSize = BitmapTools.GetBitmapSize();
        }

        public event EventHandler BackgroundUpdated;

        public Bitmap BackgroundBitmap { get => _backgroundBitmap; }

        public void OpenBackground()
        {
            if(_windowService.ShowFileDialog(out string path))
            { 
                _fullBackgroundBitmap = new Bitmap(path);
                _area = new Rectangle(
                    (_fullBackgroundBitmap.Width / 2) - (_bitmapSize.Width / 2), 
                    (_fullBackgroundBitmap.Height / 2) - (_bitmapSize.Height / 2), 
                    _bitmapSize.Width, 
                    _bitmapSize.Height);
                _backgroundBitmap = BitmapTools.CropBitmap(_fullBackgroundBitmap, _area);
                NotifyBackgroundUpdated();
            }
        }

        public void ClearBackground()
        {
            _fullBackgroundBitmap = null;
            _backgroundBitmap = BitmapTools.CreateEmptyBitmap();
            NotifyBackgroundUpdated();
        }

        public void MouseDown(Point<double> point)
        {
            _mouseDownPosition = point;
            _mouseDown = true;
        }

        public void MouseUp(Point<double> point)
        {
            if (_fullBackgroundBitmap != null && _mouseDown)
            {
                var distanceX = _mouseDownPosition.X - point.X;
                distanceX = distanceX.Map(0, _canvasSize.Width, 0, _area.Width);
                _area.X += (int)distanceX;

                var distanceY = _mouseDownPosition.Y - point.Y;
                distanceY = distanceY.Map(0, _canvasSize.Width, 0, _area.Width);
                _area.Y += (int)distanceY;

                CreateBackground();
            }
            _mouseDown = false;
        }

        public void SetCanvasSize(Size<double> canvasSize)
        {
            _canvasSize = canvasSize;
        }

        public void ZoomIn(double zoomPercentage)
        {
            var zoomFactor = (zoomPercentage / 100) + 1;
            _area.X += (int)(_area.Width - (_area.Width / zoomFactor)) / 2;
            _area.Y += (int)(_area.Height - (_area.Height / zoomFactor)) / 2;
            _area.Width = (int)(_area.Width / zoomFactor);
            _area.Height = (int)(_area.Height / zoomFactor);

            CreateBackground();
        }

        public void ZoomOut(double zoomPercentage)
        {
            var zoomFactor = (zoomPercentage / 100) + 1;
            _area.X += (int)(_area.Width - (_area.Width * zoomFactor)) / 2;
            _area.Y += (int)(_area.Height - (_area.Height * zoomFactor)) / 2;
            _area.Width = (int)(_area.Width * zoomFactor);
            _area.Height = (int)(_area.Height * zoomFactor);

            CreateBackground();
        }

        private void NotifyBackgroundUpdated()
        {
            BackgroundUpdated?.Invoke(this, new EventArgs());
        }

        private void CreateBackground()
        {
            var croppedBitmap = BitmapTools.CropBitmap(_fullBackgroundBitmap, _area);
            _backgroundBitmap = BitmapTools.ResizeBitmap(croppedBitmap);
            NotifyBackgroundUpdated();
        }
    }
}
